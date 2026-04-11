using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compost.Core.Models;
using Compost.Transcription.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using YesSql;

namespace Compost.Transcription.Services;

/// <summary>
/// Background service that handles periodic persistence of active transcription meetings.
/// Runs as a hosted service with proper lifecycle management and exception handling.
/// </summary>
public class TranscriptionBackgroundService : BackgroundService
{
    private readonly ILogger<TranscriptionBackgroundService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ConcurrentDictionary<string, Meeting> _activeMeetings;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _persistenceTokens;

    public TranscriptionBackgroundService(
        ILogger<TranscriptionBackgroundService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _activeMeetings = new ConcurrentDictionary<string, Meeting>();
        _persistenceTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
    }

    /// <summary>
    /// Registers an active meeting for periodic persistence.
    /// </summary>
    public void RegisterMeeting(Meeting meeting)
    {
        if (meeting?.Id == null) return;

        _activeMeetings[meeting.Id] = meeting;
        
        // Create a new CTS for this meeting
        var cts = new CancellationTokenSource();
        _persistenceTokens[meeting.Id] = cts;
        
        _logger.LogInformation("Registered meeting {MeetingId} for periodic persistence", meeting.Id);
        
        // Start the persistence loop for this meeting
        _ = Task.Run(async () => await RunPersistenceLoopAsync(meeting.Id, cts.Token), cts.Token);
    }

    /// <summary>
    /// Unregisters a meeting and cancels its persistence loop.
    /// </summary>
    public bool UnregisterMeeting(string meetingId)
    {
        if (string.IsNullOrEmpty(meetingId)) return false;

        var removed = false;
        if (_persistenceTokens.TryRemove(meetingId, out var cts))
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
                _logger.LogInformation("Cancelled persistence for meeting {MeetingId}", meetingId);
                removed = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling persistence for meeting {MeetingId}", meetingId);
            }
        }

        _activeMeetings.TryRemove(meetingId, out _);
        return removed;
    }

    /// <summary>
    /// Gets a meeting from active memory if it exists.
    /// </summary>
    public bool TryGetMeeting(string meetingId, out Meeting meeting)
    {
        return _activeMeetings.TryGetValue(meetingId, out meeting);
    }

    /// <summary>
    /// Adds a transcript segment to an active meeting.
    /// </summary>
    public bool TryAddSegment(string meetingId, TranscriptSegment segment)
    {
        if (!_activeMeetings.TryGetValue(meetingId, out var meeting))
        {
            return false;
        }

        lock (meeting.Transcript)
        {
            meeting.Transcript.Add(segment);
        }
        
        _logger.LogInformation("[SEGMENT] Added transcript segment to meeting {MeetingId}: '{Text}' (Total: {Count})", 
            meetingId, segment.Text, meeting.Transcript.Count);
        return true;
    }

    /// <summary>
    /// Gets all active meetings.
    /// </summary>
    public List<Meeting> GetActiveMeetings()
    {
        return _activeMeetings.Values.ToList();
    }

    /// <summary>
    /// Main background loop - not used directly, as we spawn individual loops per meeting.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TranscriptionBackgroundService started");
        
        // Keep the service running until the application stops
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Expected when service is stopping
        }
        
        _logger.LogInformation("TranscriptionBackgroundService stopping - cleaning up {MeetingCount} active meetings", 
            _activeMeetings.Count);
        
        // Cancel all persistence loops on shutdown
        foreach (var meetingId in _persistenceTokens.Keys.ToList())
        {
            UnregisterMeeting(meetingId);
        }
    }

    /// <summary>
    /// Runs the periodic persistence loop for a specific meeting.
    /// </summary>
    private async Task RunPersistenceLoopAsync(string meetingId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Started persistence loop for meeting {MeetingId}", meetingId);
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check if meeting is still active and recording
                    if (!_activeMeetings.TryGetValue(meetingId, out var meeting) || 
                        meeting.Status != MeetingStatus.Recording)
                    {
                        _logger.LogInformation("Meeting {MeetingId} is no longer recording, ending persistence loop", meetingId);
                        break;
                    }

                    // Wait before next persistence
                    await Task.Delay(10000, cancellationToken);
                    
                    // Double-check status after delay
                    if (!_activeMeetings.TryGetValue(meetingId, out var checkMeeting) || 
                        checkMeeting.Status != MeetingStatus.Recording)
                    {
                        break;
                    }

                    // Perform persistence
                    await PersistMeetingAsync(meetingId);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Persistence loop cancelled for meeting {MeetingId}", meetingId);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in persistence loop for meeting {MeetingId}", meetingId);
                    // Continue the loop despite errors
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Persistence loop cancelled for meeting {MeetingId}", meetingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in persistence loop for meeting {MeetingId}", meetingId);
        }
        finally
        {
            _logger.LogInformation("Ended persistence loop for meeting {MeetingId}", meetingId);
        }
    }

    /// <summary>
    /// Persists a meeting's transcript to the database.
    /// </summary>
    private async Task PersistMeetingAsync(string meetingId)
    {
        if (!_activeMeetings.TryGetValue(meetingId, out var meeting))
        {
            _logger.LogWarning("Cannot persist meeting {MeetingId} - not found in active memory", meetingId);
            return;
        }

        try
        {
            // Create a new scope to avoid disposed session issues
            using var scope = _serviceScopeFactory.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<ISession>();
            var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
            
            // Use direct ID lookup
            var contentItem = await contentManager.GetAsync(meetingId, VersionOptions.Latest);
            
            // Fallback to title query only if ID lookup failed
            if (contentItem == null)
            {
                contentItem = await session.Query<ContentItem, ContentItemIndex>()
                    .Where(ci => ci.ContentType == nameof(Meeting) && ci.DisplayText == meeting.Title)
                    .FirstOrDefaultAsync();
            }
            
            if (contentItem != null)
            {
                var meetingPart = contentItem.As<MeetingPart>();
                if (meetingPart != null)
                {
                    // Sync all properties
                    meetingPart.MeetingId = meeting.Id;
                    meetingPart.Title = meeting.Title;
                    meetingPart.WorkContextId = meeting.WorkContextId;
                    meetingPart.Status = meeting.Status.ToString();
                    meetingPart.StartedAt = meeting.StartedAt;
                    meetingPart.EndedAt = meeting.EndedAt;
                    meetingPart.DurationSeconds = meeting.DurationSeconds;
                    
                    lock (meeting.Transcript)
                    {
                        if (meeting.Transcript.Count > 0)
                        {
                            meetingPart.Transcript = new List<TranscriptSegment>(meeting.Transcript);
                            meetingPart.TranscriptText = string.Join("\n", meeting.Transcript.Select(t => t.Text));
                        }
                    }
                    
                    meetingPart.ActionItems = meeting.ActionItems;
                    meetingPart.ExtractedNodes = meeting.ExtractedNodes;
                    meetingPart.TranscriptionCompletedAt = meeting.TranscriptionCompletedAt;
                    meetingPart.IsProcessed = meeting.IsProcessed;

                    contentItem.Apply(meetingPart);
                    await contentManager.UpdateAsync(contentItem);
                    await contentManager.PublishAsync(contentItem);
                    await session.SaveChangesAsync();
                    
                    _logger.LogInformation("Persisted transcript for meeting {MeetingId} with {Count} segments", 
                        meetingId, meeting.Transcript.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist meeting transcript for {MeetingId}", meetingId);
        }
    }
}
