using System;
using System.Threading.Tasks;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace Compost.Transcription.Hubs;

public class TranscriptionHub : Hub
{
    private readonly ITranscriptionService _transcriptionService;

    public TranscriptionHub(ITranscriptionService transcriptionService)
    {
        _transcriptionService = transcriptionService;
    }

    public async Task JoinMeeting(string meetingId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"meeting_{meetingId}");
    }

    public async Task LeaveMeeting(string meetingId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"meeting_{meetingId}");
    }

    public async Task SendTranscriptSegment(string meetingId, TranscriptSegment segment)
    {
        // Store the segment in the transcription service
        if (_transcriptionService != null && !segment.IsInterim)
        {
            await _transcriptionService.AddTranscriptSegmentAsync(meetingId, segment);
        }
        
        // Broadcast to all clients (for live view)
        await Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveTranscriptSegment", segment);
    }

    public async Task SendRecordingStatus(string meetingId, string status, string message = null)
    {
        await Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveRecordingStatus", status, message);
    }

    public async Task SendTimerUpdate(string meetingId, string elapsed)
    {
        await Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveTimerUpdate", elapsed);
    }

    public async Task SendAudioChunk(string meetingId, string base64Audio)
    {
        if (_transcriptionService != null)
        {
            var audioData = Convert.FromBase64String(base64Audio);
            await _transcriptionService.ProcessAudioSegmentAsync(meetingId, audioData);
        }
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        // Clean up any meeting subscriptions when client disconnects
        await base.OnDisconnectedAsync(exception);
    }
}
