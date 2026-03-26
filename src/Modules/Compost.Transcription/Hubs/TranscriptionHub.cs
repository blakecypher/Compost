using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Compost.Core.Models;
using Compost.Core.Interfaces;

namespace Compost.Transcription.Hubs;

public class TranscriptionHub : Hub
{
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
        await Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveTranscriptSegment", segment);
    }

    public async Task SendRecordingStatus(string meetingId, string status, string? message = null)
    {
        await Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveRecordingStatus", status, message);
    }

    public async Task SendTimerUpdate(string meetingId, string elapsed)
    {
        await Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveTimerUpdate", elapsed);
    }

    public async Task SendAudioChunk(string meetingId, string base64Audio)
    {
        var transcriptionService = Context.GetHttpContext()?.RequestServices.GetService(typeof(ITranscriptionService)) as ITranscriptionService;
        if (transcriptionService != null)
        {
            var audioData = Convert.FromBase64String(base64Audio);
            await transcriptionService.ProcessAudioSegmentAsync(meetingId, audioData);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Clean up any meeting subscriptions when client disconnects
        await base.OnDisconnectedAsync(exception);
    }
}
