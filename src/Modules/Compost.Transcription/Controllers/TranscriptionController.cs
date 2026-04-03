using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Compost.Transcription.Controllers;

public class TranscriptionController(
    ITranscriptionService transcriptionService, 
    IProjectManager projectManager, 
    ILogger<TranscriptionController> logger,
    IDecompositionEngine decompositionEngine)
    : Controller
{
    private readonly ILogger<TranscriptionController> _logger = logger;
    public async Task<IActionResult> Index()
    {
        // Load ALL meetings from database (regardless of WorkContextId)
        var allMeetings = await transcriptionService.GetAllMeetingsAsync();

        // Merge in any active in-memory meetings not yet persisted to the database
        var activeMeetings = transcriptionService.GetActiveMeetings();
        var dbMeetingIds = new HashSet<string>(allMeetings.Select(m => m.Id));
        foreach (var active in activeMeetings)
        {
            if (!dbMeetingIds.Contains(active.Id))
            {
                allMeetings.Add(active);
            }
        }

        allMeetings = allMeetings.OrderByDescending(m => m.StartedAt).ToList();
        
        return View(allMeetings);
    }

    public async Task<IActionResult> Record(string projectId = null)
    {
        var contexts = await projectManager.GetAllProjectsAsync();
        ViewBag.WorkContexts = contexts;
        ViewBag.SelectedContextId = projectId;
        
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> StartRecording([FromBody] SaveMeetingModel model)
    {
        if (model == null || string.IsNullOrEmpty(model.Title))
        {
            return BadRequest("Invalid meeting data.");
        }

        var meeting = await transcriptionService.StartRecordingAsync(model.ContextId ?? "default", model.Title);
        return Ok(new { id = meeting.Id, title = meeting.Title, startedAt = meeting.StartedAt });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadAudio(IFormFile audio, string meetingId)
    {
        try
        {
            _logger.LogInformation("UploadAudio called with meetingId: {MeetingId}, audio file: {AudioFileName}", 
                meetingId, audio?.FileName);
            
            if (audio == null || string.IsNullOrEmpty(meetingId))
            {
                _logger.LogError("UploadAudio failed: audio file or meetingId is null/empty");
                return BadRequest("Audio file and meeting ID are required.");
            }

            // Save audio file temporarily
            var audioPath = Path.Combine(Path.GetTempPath(), $"meeting_{meetingId}_{Guid.NewGuid():N}.webm");
            _logger.LogInformation("Saving audio to: {AudioPath}", audioPath);
            
            using (var stream = new FileStream(audioPath, FileMode.Create))
            {
                await audio.CopyToAsync(stream);
            }

            _logger.LogInformation("Audio file saved, calling ProcessAudioAsync for meeting {MeetingId}", meetingId);
            
            // Process the audio file (this would integrate with Azure Speech Services)
            await transcriptionService.ProcessAudioAsync(meetingId, audioPath);

            _logger.LogInformation("ProcessAudioAsync completed for meeting {MeetingId}", meetingId);
            return Ok(new { message = "Audio uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UploadAudio for meeting {MeetingId}", meetingId);
            return StatusCode(500, $"Error processing audio: {ex.Message}");
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> StopRecording([FromBody] StopRecordingModel model)
    {
        await transcriptionService.StopRecordingAsync(model.MeetingId);
        return Ok(new { message = "Recording stopped" });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveMeetingModel model)
    {
        if (model == null || string.IsNullOrEmpty(model.Title))
        {
            return BadRequest("Invalid meeting data.");
        }

        var meeting = await transcriptionService.StartRecordingAsync(model.ContextId ?? "default", model.Title);
        
        // Add mock transcript segments
        foreach (var text in model.Transcript)
        {
            var item = new TranscriptSegment
            {
                Text = text,
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.Zero
            };
            meeting.Transcript.Add(item);
        }

        await transcriptionService.StopRecordingAsync(meeting.Id);
        await transcriptionService.ProcessRecordingAsync(meeting.Id);

        return Ok(new { id = meeting.Id });
    }

    public async Task<IActionResult> Detail(string id)
    {
        var meeting = await transcriptionService.GetMeetingByIdAsync(id);
        if (meeting == null)
        {
            return NotFound();
        }

        return View(meeting);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ExtractInsights(string id)
    {
        try
        {
            _logger.LogInformation("ExtractInsights called for meeting {MeetingId}", id);
            
            var meeting = await transcriptionService.GetMeetingByIdAsync(id);
            if (meeting == null)
            {
                return NotFound();
            }

            // Extract action items and mind map nodes
            var actionItems = await transcriptionService.ExtractActionItemsAsync(id);
            var mindMapNodes = await transcriptionService.ExtractMindMapNodesAsync(id);

            // Update the meeting with extracted insights
            meeting.ActionItems = actionItems;
            meeting.ExtractedNodes = mindMapNodes;
            meeting.IsProcessed = true;
            
            // Persist changes to database
            await transcriptionService.UpdateMeetingAsync(meeting);

            _logger.LogInformation("Extracted {ActionItemCount} action items and {MindMapNodeCount} mind map nodes for meeting {MeetingId}", 
                actionItems.Count, mindMapNodes.Count, id);

            return Ok(new { 
                actionItemCount = actionItems.Count, 
                mindMapNodeCount = mindMapNodes.Count,
                actionItems,
                mindMapNodes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting insights for meeting {MeetingId}", id);
            return StatusCode(500, "Error extracting insights");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            _logger.LogInformation("Delete called for meeting {MeetingId}", id);
            
            var deleted = await transcriptionService.DeleteMeetingAsync(id);
            
            if (deleted)
            {
                _logger.LogInformation("Successfully deleted meeting {MeetingId}", id);
            }
            else
            {
                _logger.LogWarning("Failed to delete meeting {MeetingId} or it was already gone", id);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting meeting {MeetingId}", id);
            return StatusCode(500, "Error deleting meeting");
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> PromoteToKanban([FromBody] PromoteToKanbanRequest request)
    {
        try
        {
            _logger.LogInformation("PromoteToKanban called for meeting {MeetingId}, action item {ActionItemId}", 
                request.MeetingId, request.ActionItemId);
            
            var card = await decompositionEngine.PromoteActionItemToKanbanAsync(
                request.MeetingId, 
                request.ActionItemId, 
                request.ProjectId);
            
            return Ok(new { cardId = card.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting action item to Kanban");
            return StatusCode(500, ex.Message);
        }
    }
}

public class PromoteToKanbanRequest
{
    public string MeetingId { get; set; } = string.Empty;
    public string ActionItemId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
}

public class SaveMeetingModel
{
    public string Title { get; set; } = string.Empty;
    public string ContextId { get; set; }
    public List<string> Transcript { get; set; } = [];
}

public class StopRecordingModel
{
    public string MeetingId { get; set; } = string.Empty;
}
