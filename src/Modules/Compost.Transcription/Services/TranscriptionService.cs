using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.AspNetCore.SignalR;
using Compost.Transcription.Hubs;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using Compost.Transcription.Models;
using YesSql;
using System.Diagnostics;
using System.Text;
using Compost.Core.Services;
using Microsoft.Extensions.Configuration;
using Config = Microsoft.Extensions.Configuration.IConfiguration;
using Microsoft.Extensions.DependencyInjection;

namespace Compost.Transcription.Services;

public class TranscriptionService(
    ILogger<TranscriptionService> logger,
    IHubContext<TranscriptionHub> hubContext,
    IContentManager contentManager,
    ISession session,
    Config configuration,
    IAIIntegrationService aiIntegrationService,
    ITranscriptContextExtractor contextExtractor,
    IServiceScopeFactory serviceScopeFactory)
    : ITranscriptionService
{
    public readonly Dictionary<string, SpeechRecognizer> _activeRecognizers = new();
    private readonly Dictionary<string, PushAudioInputStream> _activeStreams = new();
    private readonly string? _azureSpeechKey = configuration["Compost:AzureSpeech:SubscriptionKey"];
    private readonly string? _azureSpeechRegion = configuration["Compost:AzureSpeech:Region"];
    private readonly bool _forceMock = configuration.GetValue<bool>("Compost:Transcription:ForceMock");
    
    // In-memory storage for active recordings (performance optimization)
    private static readonly Dictionary<string, Meeting> _activeMeetingsMemory = new();
    
    private static readonly char[] separator = ['.', '!', '?'];
    private static readonly string[] _mockSentences = new[]
    {
        "Welcome to the meeting. We are discussing the new transcription module.",
        "I have identified some issues with the Azure Speech initialization on Linux.",
        "Specifically, Error 2176 suggests that libssl 1.1 might be missing.",
        "We need to implement a robust fallback to mock transcription so users aren't left with empty results.",
        "Let's also add a ForceMock setting for easier testing without Azure dependencies.",
        "The live transcription should now show updates in real-time.",
        "We are also implementing periodic persistence to ensure no data is lost.",
        "This meeting is being recorded and transcribed for future reference.",
        "Action items will be automatically extracted from the final transcript.",
        "The mind map will visualize the key concepts discussed today."
    };

    public async Task<Meeting> StartRecordingAsync(string projectId, string title)
    {
        var meeting = new Meeting
        {
            WorkContextId = projectId,
            Title = title,
            Status = MeetingStatus.Recording,
            StartedAt = DateTime.UtcNow
        };

        // Create Orchard Core content item for the meeting (this persists it to database)
        try
        {
            var contentItem = await contentManager.NewAsync(nameof(Meeting));
            contentItem.DisplayText = title;
            var meetingPart = contentItem.As<MeetingPart>();
            
            if (meetingPart == null)
            {
                logger.LogWarning("MeetingPart not found on content item 'Meeting'. Attempting to weld it. Please ensure migrations have run successfully.");
                contentItem.Weld<MeetingPart>();
                meetingPart = contentItem.As<MeetingPart>();
            }
            
            if (meetingPart == null)
            {
                logger.LogError("MeetingPart not found on content item and welding failed. Make sure migrations have been run.");
                throw new InvalidOperationException("MeetingPart not configured properly. Please ensure the Transcription module migrations have been run.");
            }
            
            meetingPart.MeetingId = meeting.Id;
            meetingPart.Title = title;
            meetingPart.WorkContextId = projectId;
            meetingPart.Status = "Recording";
            meetingPart.StartedAt = meeting.StartedAt;
            meetingPart.AutoExtractMindMapNodes = true;
            meetingPart.AutoExtractActionItems = true;
            
            contentItem.Apply(meetingPart);
            await contentManager.CreateAsync(contentItem);
            await contentManager.PublishAsync(contentItem);
            
            logger.LogInformation("Meeting persisted to database with ID: {MeetingId}", meeting.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Orchard Core content item for meeting");
            // Continue without creating the content item for now
        }
        
        // Initialize active meeting storage
        _activeMeetingsMemory[meeting.Id] = meeting;
        
        // Notify clients about recording status
        await hubContext.Clients.Group($"meeting_{meeting.Id}").SendAsync("ReceiveRecordingStatus", "recording", "Recording started");
        
        // Start real-time transcription and periodic persistence in background
        var cts = new System.Threading.CancellationTokenSource();
        _ = Task.Run(async () => await StartRealtimeTranscriptionAsync(meeting.Id));
        _ = Task.Run(async () => await StartPeriodicPersistenceAsync(meeting.Id, cts.Token));
        
        logger.LogInformation("Started recording meeting: {Title} ({Id})", title, meeting.Id);
        return meeting;
    }

    public async Task StopRecordingAsync(string meetingId)
    {
        logger.LogInformation("[STOP] StopRecordingAsync called for meeting {MeetingId}", meetingId);
        
        // First, check what's in active memory
        if (_activeMeetingsMemory.TryGetValue(meetingId, out var checkActiveMeeting))
        {
            logger.LogInformation("[STOP] Active memory has meeting {MeetingId} with {Count} transcript segments", 
                meetingId, checkActiveMeeting.Transcript.Count);
        }
        else
        {
            logger.LogWarning("[STOP] Meeting {MeetingId} NOT found in active memory!", meetingId);
        }
        
        var meeting = await GetMeetingByIdAsync(meetingId);
        if (meeting != null)
        {
            logger.LogInformation("[STOP] Found meeting in DB: {MeetingId}, current transcript count: {Count}", 
                meetingId, meeting.Transcript.Count);
            
            meeting.Status = MeetingStatus.Processing;
            meeting.EndedAt = DateTime.UtcNow;
            meeting.DurationSeconds = (int)(meeting.EndedAt.Value - meeting.StartedAt).TotalSeconds;
            
            // CRITICAL: Copy transcript from active memory (where browser sent it) to the DB meeting
            if (_activeMeetingsMemory.TryGetValue(meetingId, out var activeMeetingForSync))
            {
                logger.LogInformation("[STOP] Syncing {Count} segments from active memory to DB meeting {MeetingId}", 
                    activeMeetingForSync.Transcript.Count, meetingId);
                meeting.Transcript = new List<TranscriptSegment>(activeMeetingForSync.Transcript);
                logger.LogInformation("[STOP] After sync, meeting transcript count: {Count}", meeting.Transcript.Count);
            }
            else
            {
                logger.LogWarning("[STOP] Could not find meeting in active memory for sync!");
            }
            
            // Notify clients about processing status
            await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveRecordingStatus", "processing", "Processing transcription...");
            
            // Persist meeting with transcript to database
            logger.LogInformation("[STOP] Calling UpdateMeetingAsync for {MeetingId}", meetingId);
            await UpdateMeetingAsync(meeting);
            logger.LogInformation("[STOP] UpdateMeetingAsync completed for {MeetingId}", meetingId);

            // Clean up Azure Speech resources if active
            if (_activeStreams.TryGetValue(meetingId, out var pushStream))
            {
                pushStream.Close();
                _activeStreams.Remove(meetingId);
            }
            
            if (_activeRecognizers.TryGetValue(meetingId, out var recognizer))
            {
                await recognizer.StopContinuousRecognitionAsync();
                recognizer.Dispose();
                _activeRecognizers.Remove(meetingId);
            }

            // Start final processing of the transcript
            _ = Task.Run(async () => {
                using var scope = serviceScopeFactory.CreateScope();
                var scopedService = scope.ServiceProvider.GetRequiredService<ITranscriptionService>();
                await scopedService.ProcessRecordingAsync(meetingId);
            });
            
            logger.LogInformation("Stopped recording and cleaned up resources for meeting: {Id}", meetingId);
        }
        else
        {
            // If meeting not found in database, still trigger processing if we have active transcript
            if (_activeMeetingsMemory.TryGetValue(meetingId, out var activeMeeting))
            {
                logger.LogWarning("Meeting not found in database, but active transcript exists. Triggering processing for {MeetingId}", meetingId);
                
                // Notify clients about processing status
                await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveRecordingStatus", "processing", "Processing transcription...");
                
                // Start final processing of the transcript
                _ = Task.Run(async () => {
                    using var scope = serviceScopeFactory.CreateScope();
                    var scopedService = scope.ServiceProvider.GetRequiredService<ITranscriptionService>();
                    await scopedService.ProcessRecordingAsync(meetingId);
                });
                
                logger.LogInformation("Stopped recording meeting (active storage): {Id}", meetingId);
            }
            else
            {
                logger.LogError("Meeting not found and no active transcript: {MeetingId}", meetingId);
                await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveRecordingStatus", "error", "Meeting not found");
            }
        }
    }

    private async Task StartRealtimeTranscriptionAsync(string meetingId)
    {
        if (!_activeMeetingsMemory.TryGetValue(meetingId, out var meeting))
        {
            return;
        }

        // NOTE: Browser Web Speech API handles live transcription in development mode.
        // The mock loop is disabled to prevent simulated content from mixing with real recordings.
        // Only use Azure Speech in production when keys are configured.
        if (!string.IsNullOrEmpty(_azureSpeechKey) && !string.IsNullOrEmpty(_azureSpeechRegion))
        {
            logger.LogInformation("Real-time transcription for meeting {MeetingId} will use Azure Speech via streaming chunks.", meetingId);
        }
        else
        {
            logger.LogInformation("Browser Web Speech API will handle transcription for meeting {MeetingId}. Server mock loop disabled.", meetingId);
            // Mock loop intentionally disabled - browser handles transcription via SignalR
        }
    }

    private async Task StartMockLiveTranscriptionLoopAsync(string meetingId)
    {
        var random = new Random();
        var startTime = TimeSpan.Zero;
        
        while (_activeMeetingsMemory.TryGetValue(meetingId, out var meeting) && meeting.Status == MeetingStatus.Recording)
        {
            await Task.Delay(random.Next(3000, 7000)); // New segment every 3-7 seconds
            
            if (meeting.Status != MeetingStatus.Recording) break;

            var sentence = _mockSentences[random.Next(_mockSentences.Length)];
            var duration = TimeSpan.FromSeconds(sentence.Length / 15.0 + 1);
            
            var segment = new TranscriptSegment
            {
                Text = sentence,
                StartTime = startTime,
                EndTime = startTime + duration,
                Confidence = 1.0
            };
            
            meeting.Transcript.Add(segment);
            startTime = segment.EndTime;
            
            await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveTranscriptSegment", segment);
            logger.LogDebug("Sent mock live segment for meeting {MeetingId}: {Text}", meetingId, sentence);
        }
    }

    public async Task AddTranscriptSegmentAsync(string meetingId, TranscriptSegment segment)
    {
        if (_activeMeetingsMemory.TryGetValue(meetingId, out var meeting))
        {
            meeting.Transcript.Add(segment);
            logger.LogInformation("[SEGMENT] Added transcript segment to meeting {MeetingId}: '{Text}' (Total: {Count})", meetingId, segment.Text, meeting.Transcript.Count);
        }
        else
        {
            logger.LogWarning("[SEGMENT] Cannot add segment - meeting {MeetingId} not found in active memory", meetingId);
        }
    }

    public async Task PersistMeetingTranscriptAsync(string meetingId)
    {
        try
        {
            if (!_activeMeetingsMemory.TryGetValue(meetingId, out var meeting)) return;
            
            // Create a new scope to avoid disposed session issues in background tasks
            using var scope = serviceScopeFactory.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<ISession>();
            var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
            
            // Query for existing content item
            var contentItem = await session.Query<ContentItem, ContentItemIndex>()
                .Where(ci => ci.ContentType == nameof(Meeting) && ci.DisplayText == meeting.Title)
                .FirstOrDefaultAsync();
            
            if (contentItem == null && !string.IsNullOrEmpty(meeting.Id))
            {
                contentItem = await contentManager.GetAsync(meeting.Id, VersionOptions.Latest);
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
                    meetingPart.Transcript = meeting.Transcript;
                    meetingPart.ActionItems = meeting.ActionItems;
                    meetingPart.ExtractedNodes = meeting.ExtractedNodes;
                    meetingPart.TranscriptionCompletedAt = meeting.TranscriptionCompletedAt;
                    meetingPart.IsProcessed = meeting.IsProcessed;
                    meetingPart.TranscriptText = string.Join("\n", meeting.Transcript.Select(t => t.Text));

                    contentItem.Apply(meetingPart);
                    await contentManager.UpdateAsync(contentItem);
                    await contentManager.PublishAsync(contentItem);
                    
                    logger.LogInformation("Persisted transcript for meeting {MeetingId} with {Count} segments", meetingId, meeting.Transcript.Count);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist meeting transcript for {MeetingId}", meetingId);
        }
    }

    private async Task StartPeriodicPersistenceAsync(string meetingId, System.Threading.CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _activeMeetingsMemory.TryGetValue(meetingId, out var meeting) && meeting.Status == MeetingStatus.Recording)
        {
            try
            {
                await Task.Delay(10000, cancellationToken); // Persist every 10 seconds
                if (meeting.Status != MeetingStatus.Recording) break;
                
                await PersistMeetingTranscriptAsync(meetingId);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in periodic persistence for meeting {MeetingId}", meetingId);
            }
        }
    }

    public async Task ProcessAudioSegmentAsync(string meetingId, byte[] segment)
    {
        if (_forceMock || string.IsNullOrEmpty(_azureSpeechKey) || string.IsNullOrEmpty(_azureSpeechRegion))
        {
            // If no Azure keys or ForceMock is on, we skip Azure processing
            return;
        }

        try
        {
            if (!_activeStreams.TryGetValue(meetingId, out var pushStream))
            {
                logger.LogInformation("Initializing Azure Speech continuous recognition for meeting {MeetingId}", meetingId);
                
                var speechConfig = SpeechConfig.FromSubscription(_azureSpeechKey, _azureSpeechRegion);
                speechConfig.SpeechRecognitionLanguage = "en-US";
                
                pushStream = AudioInputStream.CreatePushStream();
                var audioInput = AudioConfig.FromStreamInput(pushStream);
                var recognizer = new SpeechRecognizer(speechConfig, audioInput);
                
                _activeStreams[meetingId] = pushStream;
                _activeRecognizers[meetingId] = recognizer;
                
                recognizer.Recognizing += async (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizingSpeech)
                    {
                        var transcriptSegment = new TranscriptSegment
                        {
                            Text = e.Result.Text,
                            StartTime = TimeSpan.FromTicks(e.Result.OffsetInTicks),
                            EndTime = TimeSpan.FromTicks(e.Result.OffsetInTicks + e.Result.Duration.Ticks),
                            Confidence = 0.8,
                            IsInterim = true
                        };
                        await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveTranscriptSegment", transcriptSegment);
                    }
                };
                
                recognizer.Recognized += async (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        var transcriptSegment = new TranscriptSegment
                        {
                            Text = e.Result.Text,
                            StartTime = TimeSpan.FromTicks(e.Result.OffsetInTicks),
                            EndTime = TimeSpan.FromTicks(e.Result.OffsetInTicks + e.Result.Duration.Ticks),
                            Confidence = 0.95,
                            IsInterim = false
                        };
                        
                        if (_activeMeetingsMemory.TryGetValue(meetingId, out var meeting))
                        {
                            meeting.Transcript.Add(transcriptSegment);
                        }
                        
                        await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveTranscriptSegment", transcriptSegment);
                    }
                };
                
                await recognizer.StartContinuousRecognitionAsync();
            }

            pushStream.Write(segment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing audio segment for meeting {MeetingId}", meetingId);
        }
    }

    public async IAsyncEnumerable<TranscriptSegment> GetRealtimeTranscriptAsync(string meetingId)
    {
        // For active recordings, return from memory storage
        if (_activeMeetingsMemory.TryGetValue(meetingId, out var activeMeeting))
        {
            foreach (var segment in activeMeeting.Transcript)
            {
                yield return segment;
            }
            yield break;
        }
        
        // For completed recordings, return from database
        var meeting = await GetMeetingByIdAsync(meetingId);
        if (meeting == null)
        {
            yield break;
        }

        // If Azure Speech is configured, use it
        if (!string.IsNullOrEmpty(_azureSpeechKey) && !string.IsNullOrEmpty(_azureSpeechRegion))
        {
            await foreach (var segment in GetAzureSpeechTranscriptAsync(meetingId))
            {
                meeting.Transcript.Add(segment);
                await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveTranscriptSegment", segment);
                yield return segment;
            }
        }
        else
        {
            logger.LogWarning("Azure Speech is not configured for meeting {MeetingId}. No transcript retrieval possible.", meetingId);
        }
    }

    private async IAsyncEnumerable<TranscriptSegment> GetAzureSpeechTranscriptAsync(string meetingId)
    {
        var speechConfig = SpeechConfig.FromSubscription(_azureSpeechKey, _azureSpeechRegion);
        speechConfig.SpeechRecognitionLanguage = "en-US";
        
        // Create a push stream to receive audio from the browser
        using var pushStream = AudioInputStream.CreatePushStream();
        using var audioInput = AudioConfig.FromStreamInput(pushStream);
        using var recognizer = new SpeechRecognizer(speechConfig, audioInput);
        
        _activeRecognizers[meetingId] = recognizer;
        
        // Subscribe to recognition events
        var taskCompletionSource = new TaskCompletionSource<bool>();
        
        recognizer.Recognizing += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizingSpeech)
            {
                var segment = new TranscriptSegment
                {
                    Text = e.Result.Text,
                    StartTime = TimeSpan.FromTicks(e.Result.OffsetInTicks),
                    EndTime = TimeSpan.FromTicks(e.Result.OffsetInTicks + e.Result.Duration.Ticks),
                    Confidence = 0.8
                };
                // Interim result

                // Send interim result
                hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveTranscriptSegment", segment);
            }
        };
        
        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                var segment = new TranscriptSegment
                {
                    Text = e.Result.Text,
                    StartTime = TimeSpan.FromTicks(e.Result.OffsetInTicks),
                    EndTime = TimeSpan.FromTicks(e.Result.OffsetInTicks + e.Result.Duration.Ticks),
                    Confidence = 0.95
                };
                // Final result

                // This will be yielded from the main method
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                logger.LogWarning("No speech could be recognized: {Text}", e.Result.Text ?? "No text available");
            }
        };
        
        recognizer.SessionStopped += (s, e) =>
        {
            taskCompletionSource.SetResult(true);
        };
        
        recognizer.Canceled += (s, e) =>
        {
            logger.LogError("Speech recognition canceled: {Reason}", e.Reason);
            taskCompletionSource.SetResult(false);
        };
        
        // Start continuous recognition
        await recognizer.StartContinuousRecognitionAsync();
        
        // Wait for session to stop
        await taskCompletionSource.Task;
        
        // Clean up
        await recognizer.StopContinuousRecognitionAsync();
        _activeRecognizers.Remove(meetingId);
        
        yield break; // Segments are sent via SignalR in the event handlers
    }

    public async Task ProcessRecordingAsync(string meetingId)
    {
        logger.LogInformation("Starting ProcessRecordingAsync for meeting {MeetingId}", meetingId);
        
        var meeting = await GetMeetingByIdAsync(meetingId);
        if (meeting == null)
        {
            // If not found in database, check active meeting memory
            if (_activeMeetingsMemory.TryGetValue(meetingId, out meeting))
            {
                logger.LogInformation("Meeting {MeetingId} found in active storage for ProcessRecordingAsync", meetingId);
            }
            else
            {
                // If meeting not found in database and not in active memory, create a placeholder
                logger.LogWarning("Meeting not found in database, creating from active storage for {MeetingId}", meetingId);
                meeting = new Meeting
                {
                    Id = meetingId,
                    Title = "Recording " + meetingId[..8],
                    // Use partial ID as title
                    WorkContextId = "unknown",
                    Status = MeetingStatus.Processing,
                    StartedAt = DateTime.UtcNow.AddMinutes(-5),
                    // Assume started 5 minutes ago
                    Transcript = [], // Initialize empty, will be filled from active memory if available
                    ActionItems = [],
                    ExtractedNodes = []
                };
            }
        }

        try
        {
            meeting.Status = MeetingStatus.Processing;
            await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveRecordingStatus", "processing", "Processing transcription...");
            
            // Ensure we have the latest transcript from active memory
            if (_activeMeetingsMemory.TryGetValue(meetingId, out var activeMeeting))
            {
                meeting.Transcript = activeMeeting.Transcript;
                logger.LogInformation("Found {Count} transcript segments in active storage for meeting {MeetingId}", meeting.Transcript.Count, meetingId);
                logger.LogInformation("Active transcript preview: {TranscriptPreview}", 
                    meeting.Transcript.Count > 0 ? meeting.Transcript.First().Text : "No segments");
            }
            else
            {
                logger.LogWarning("No active transcript found for meeting {MeetingId}, using database transcript with {Count} segments", meetingId, meeting.Transcript.Count);
            }
            
            logger.LogInformation("Extracting action items for meeting {MeetingId}", meetingId);
            // Extract action items using AI
            var actionItems = await ExtractActionItemsAsync(meetingId);
            meeting.ActionItems = actionItems;
            
            logger.LogInformation("Extracting mind map nodes for meeting {MeetingId}", meetingId);
            // Extract mind map nodes using AI
            var mindMapNodes = await ExtractMindMapNodesAsync(meetingId);
            meeting.ExtractedNodes = mindMapNodes;
            
            meeting.Status = MeetingStatus.Completed;
            meeting.TranscriptionCompletedAt = DateTime.UtcNow;
            meeting.IsProcessed = true;
            
            logger.LogInformation("Meeting processing completed, updating Orchard Core content for meeting {MeetingId}", meetingId);
            await UpdateMeetingAsync(meeting);
            
            logger.LogInformation("Sending 'completed' status to SignalR group for meeting {MeetingId}", meetingId);
            await hubContext.Clients.Group("meeting_" + meetingId).SendAsync("ReceiveRecordingStatus", "completed", "Processing completed!");
            
            // Final persistence to ensure everything (transcript, action items, mind map) is saved to Orchard Core
            await PersistMeetingTranscriptAsync(meetingId);
            
            logger.LogInformation("Completed processing meeting: {MeetingId} with {ActionItemCount} action items and {MindMapNodeCount} mind map nodes", 
                meetingId, actionItems.Count, mindMapNodes.Count);
            
            // Clean up active meeting storage after a delay to allow list view to show it
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(5)); // Keep for 5 minutes
                if (_activeMeetingsMemory.Remove(meetingId))
                {
                    logger.LogInformation("Cleaned up active meeting storage for meeting {MeetingId} after delay", meetingId);
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ProcessRecordingAsync for meeting {MeetingId}", meetingId);
            meeting.Status = MeetingStatus.Failed;
            await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveRecordingStatus", "error", "Processing failed");
        }
    }

    private async Task<string> ConvertWebMToWavAsync(string webmFilePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                logger.LogInformation("Converting WebM to WAV for file: {FilePath}", webmFilePath);
                
                var wavFilePath = Path.ChangeExtension(webmFilePath, ".wav");
                
                // Check if FFmpeg is available by trying to run it
                try
                {
                    var ffmpegCheck = Process.Start(new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = "-version",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    });
                    
                    if (ffmpegCheck == null || ffmpegCheck.WaitForExit(5000) && ffmpegCheck.ExitCode != 0)
                    {
                        var errorMessage = "FFmpeg not found on this system. To install FFmpeg for audio conversion, run: sudo apt update && sudo apt install -y ffmpeg";
                        logger.LogWarning("{ErrorMessage}", errorMessage);
                        throw new InvalidOperationException(errorMessage);
                    }
                    
                    ffmpegCheck?.Dispose();
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    var errorMessage = "FFmpeg not found on this system. To install FFmpeg for audio conversion, run: sudo apt update && sudo apt install -y ffmpeg";
                    logger.LogWarning("{ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                
                // Use FFmpeg for cross-platform audio conversion
                var processInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{webmFilePath}\" -acodec pcm_s16le -ar 16000 -ac 1 \"{wavFilePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start FFmpeg process");
                }
                
                // Read output for logging
                var output = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    logger.LogError("FFmpeg conversion failed with exit code {ExitCode}: {Output}", process.ExitCode, output);
                    throw new InvalidOperationException($"FFmpeg conversion failed with exit code {process.ExitCode}");
                }
                
                if (!File.Exists(wavFilePath))
                {
                    throw new InvalidOperationException("FFmpeg did not create output file");
                }
                
                logger.LogInformation("Successfully converted WebM to WAV: {WavFilePath}", wavFilePath);
                return wavFilePath;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to convert WebM to WAV for file: {FilePath}", webmFilePath);
                throw;
            }
        });
    }

    public async Task ProcessAudioAsync(string meetingId, string audioPath)
    {
        logger.LogInformation("ProcessAudioAsync called for meeting {MeetingId} with audio path {AudioPath}", meetingId, audioPath);
        
        // Use a timeout to prevent hanging
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(2));
        
        string wavFilePath = audioPath; // Initialize here so it's accessible in finally block
        
        var meeting = await GetMeetingByIdAsync(meetingId);
        if (meeting == null)
        {
            // If meeting not found in database, try to use the one from active meeting storage
            if (_activeMeetingsMemory.TryGetValue(meetingId, out meeting))
            {
                logger.LogWarning("Meeting not found in database, using from active storage for ProcessAudioAsync {MeetingId}", meetingId);
            }
            else
            {
                logger.LogError("Meeting {MeetingId} not found in database or active storage.", meetingId);
                return;
            }
        }

        try
        {
            // For WebM files, first try Azure Speech directly (some versions may support it)
            // If that fails, try conversion, then fall back to mock processing
            bool isWebM = Path.GetExtension(audioPath).ToLower() == ".webm";
            
            if (isWebM)
            {
                logger.LogInformation("WebM file detected, attempting Azure Speech processing directly first: {FilePath}", audioPath);
                
                // Try Azure Speech directly with WebM
                if (!string.IsNullOrEmpty(_azureSpeechKey) && !string.IsNullOrEmpty(_azureSpeechRegion))
                {
                    try
                    {
                        logger.LogInformation("Attempting Azure Speech directly with WebM for meeting {MeetingId}", meetingId);
                        await ProcessAudioWithAzureAsync(meetingId, audioPath, cts.Token);
                        logger.LogInformation("Azure Speech succeeded with WebM format for meeting {MeetingId}", meetingId);
                        
                        // If successful, we can skip the rest
                        logger.LogInformation("Audio processing completed, calling ProcessRecordingAsync for meeting {MeetingId}", meetingId);
                        await ProcessRecordingAsync(meetingId);
                        return;
                    }
                    catch (Exception ex) when (ex.Message.Contains("SPXERR_INVALID_HEADER") || ex.Message.Contains("0xa") || ex.Message.Contains("format"))
                    {
                        logger.LogWarning(ex, "Azure Speech failed with WebM format for meeting {MeetingId}, attempting conversion", meetingId);
                        
                        // Try conversion if FFmpeg is available
                        string convertedWavPath;
                        try
                        {
                            convertedWavPath = await ConvertWebMToWavAsync(audioPath);
                            logger.LogInformation("WebM to WAV conversion successful, proceeding with Azure Speech for meeting {MeetingId}", meetingId);
                            
                            // Continue with converted file
                            wavFilePath = convertedWavPath;
                        }
                        catch (Exception convEx)
                        {
                            logger.LogWarning(convEx, "WebM to WAV conversion failed for meeting {MeetingId}, falling back to mock processing", meetingId);
                            await ProcessAudioWithMockAsync(meetingId);
                            return;
                        }
                    }
                }
                else
                {
                    logger.LogInformation("Azure Speech not configured, using mock processing for WebM file for meeting {MeetingId}", meetingId);
                    await ProcessAudioWithMockAsync(meetingId);
                    return;
                }
            }
            else
            {
                // For non-WebM files, use the original path
                wavFilePath = audioPath;
            }
            
            // If Azure Speech is configured, process the audio file
            bool azureSuccess = false;
            if (!_forceMock && !string.IsNullOrEmpty(_azureSpeechKey) && !string.IsNullOrEmpty(_azureSpeechRegion))
            {
                try
                {
                    logger.LogInformation("Using Azure Speech to process audio for meeting {MeetingId}", meetingId);
                    azureSuccess = await ProcessAudioWithAzureAsync(meetingId, wavFilePath, cts.Token);
                }
                catch (Exception ex) when (ex.Message.Contains("SPXERR_INVALID_HEADER") || ex.Message.Contains("0xa"))
                {
                    logger.LogWarning(ex, "Azure Speech failed due to invalid audio format, falling back to mock processing for meeting {MeetingId}", meetingId);
                }
            }

            if (!azureSuccess)
            {
                logger.LogInformation("Azure Speech failed or was bypassed, using mock processing for meeting {MeetingId}", meetingId);
                await ProcessAudioWithMockAsync(meetingId);
            }
            
            logger.LogInformation("Audio processing completed, calling ProcessRecordingAsync for meeting {MeetingId}", meetingId);
            // After audio processing is complete, process the recording
            await ProcessRecordingAsync(meetingId);
        }
        catch (OperationCanceledException)
        {
            logger.LogError("ProcessAudioAsync timed out for meeting {MeetingId}", meetingId);
            await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveRecordingStatus", "error", "Processing timed out");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing audio for meeting {MeetingId}", meetingId);
            await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveRecordingStatus", "error", "Processing failed");
        }
        finally
        {
            // Clean up temporary files
            if (File.Exists(audioPath))
            {
                File.Delete(audioPath);
            }
            
            // Clean up converted WAV file if it's different from the original
            if (wavFilePath != audioPath && File.Exists(wavFilePath))
            {
                File.Delete(wavFilePath);
            }
        }
    }

    private async Task<bool> ProcessAudioWithAzureAsync(string meetingId, string audioPath, System.Threading.CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing Azure Speech with region: {Region}, language: en-US", 
            string.IsNullOrEmpty(_azureSpeechRegion) ? "NOT_SET" : _azureSpeechRegion.Substring(0, Math.Min(3, _azureSpeechRegion.Length)) + "***");
        
        var speechConfig = SpeechConfig.FromSubscription(_azureSpeechKey, _azureSpeechRegion);
        speechConfig.SpeechRecognitionLanguage = "en-US";
        
        logger.LogInformation("Audio file for Azure Speech: {AudioPath}, file size: {FileSize} bytes", 
            audioPath, File.Exists(audioPath) ? new FileInfo(audioPath).Length : 0);
        
        using var audioInput = AudioConfig.FromWavFileInput(audioPath);
        using var recognizer = new SpeechRecognizer(speechConfig, audioInput);
        
        var finalSegments = new List<TranscriptSegment>();
        
        var recognitionTask = new TaskCompletionSource<bool>();
        
        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrWhiteSpace(e.Result.Text))
            {
                var segment = new TranscriptSegment
                {
                    Text = e.Result.Text,
                    StartTime = TimeSpan.FromTicks(e.Result.OffsetInTicks),
                    EndTime = TimeSpan.FromTicks(e.Result.OffsetInTicks + e.Result.Duration.Ticks),
                    Confidence = 0.95
                };

                finalSegments.Add(segment);
                
                // Send real-time update
                hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveTranscriptSegment", segment);
            }
        };
        
        recognizer.SessionStopped += (s, e) =>
        {
            logger.LogInformation("Azure Speech session stopped for meeting {MeetingId}", meetingId);
            recognitionTask.TrySetResult(true);
        };
        
        recognizer.Canceled += (s, e) =>
        {
            var errorMessage = $"Azure Speech recognition canceled. Reason: {e.Reason}, ErrorCode: {e.ErrorCode}, ErrorDetails: {e.ErrorDetails}";
            
            if (e.ErrorDetails.Contains("2176"))
            {
                errorMessage += "\nTIP: Error 2176 on Linux often indicates missing libssl1.1. Try: sudo apt update && sudo apt install -y libssl1.1";
            }
            
            logger.LogError("{ErrorMessage}", errorMessage);
            recognitionTask.TrySetResult(false);
        };
        
        // Start recognition
        await recognizer.StartContinuousRecognitionAsync();
        
        // Wait for session to stop
        await using (cancellationToken.Register(() => recognitionTask.TrySetCanceled()))
        {
            await recognitionTask.Task;
        }
        
        await recognizer.StopContinuousRecognitionAsync();
        
        // Update meeting with final transcript
        var meeting = await GetMeetingByIdAsync(meetingId);
        if (meeting != null)
        {
            // Clear existing transcript (which might have real-time fragments) 
            // to avoid duplication since the full-file processing is more accurate.
            meeting.Transcript.Clear();
            meeting.Transcript.AddRange(finalSegments);
            
            // If the active meeting in memory is a different reference, update it too
            // though GetMeetingByIdAsync usually returns the one from _activeMeetingsMemory
            if (_activeMeetingsMemory.TryGetValue(meetingId, out var activeMeeting) && activeMeeting != meeting)
            {
                activeMeeting.Transcript.Clear();
                activeMeeting.Transcript.AddRange(finalSegments);
            }
            
            logger.LogInformation("Updated meeting with {Count} final segments for meeting {MeetingId}", finalSegments.Count, meetingId);
        }

        return finalSegments.Count > 0;
    }

    private async Task ProcessAudioWithMockAsync(string meetingId)
    {
        // Mock processing for development
        await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveRecordingStatus", "processing", "Simulating transcription fallback...");
        
        // Use a subset of shared mock sentences
        var random = new Random();
        var mockSegments = new List<TranscriptSegment>();
        var startTime = TimeSpan.Zero;
        
        for (int i = 0; i < 5; i++)
        {
            var sentence = _mockSentences[random.Next(_mockSentences.Length)];
            var duration = TimeSpan.FromSeconds(sentence.Length / 15.0 + 1);
            
            mockSegments.Add(new TranscriptSegment
            {
                Text = sentence,
                StartTime = startTime,
                EndTime = startTime + duration,
                Confidence = 1.0
            });
            startTime += duration + TimeSpan.FromSeconds(1);
        }

        await Task.Delay(2000); // Simulate processing time
        
        if (_activeMeetingsMemory.TryGetValue(meetingId, out var meeting))
        {
            // Only add mock data if there's no real transcript - never overwrite real data
            if (meeting.Transcript.Count == 0)
            {
                meeting.Transcript.AddRange(mockSegments);
                logger.LogInformation("Mock processing (fallback) completed for meeting {MeetingId}. Added {Count} mock segments (no real transcript existed).", meetingId, mockSegments.Count);
                
                // Send updates via SignalR
                foreach (var segment in mockSegments)
                {
                    await hubContext.Clients.Group($"meeting_{meetingId}").SendAsync("ReceiveTranscriptSegment", segment);
                    await Task.Delay(200);
                }
            }
            else
            {
                logger.LogInformation("Skipping mock data for meeting {MeetingId} - {Count} real transcript segments already exist.", meetingId, meeting.Transcript.Count);
            }
        }
    }

    public Task<Speaker> CreateSpeakerProfileAsync(string name, string? role, Stream audioSample)
    {
        // For now, just return a speaker object - speaker profiles would need their own content type
        var speaker = new Speaker
        {
            Name = name,
            Role = role
        };
        return Task.FromResult(speaker);
    }

    public Task<List<Speaker>> GetSpeakerProfilesAsync()
    {
        // For now, return empty list - speaker profiles would need their own content type
        return Task.FromResult(new List<Speaker>());
    }

    public Task UpdateSegmentSpeakerAsync(string meetingId, string segmentId, string speakerId)
    {
        var meeting = GetMeetingByIdAsync(meetingId).Result;
        var segment = meeting?.Transcript.FirstOrDefault(s => s.Id == segmentId);
        if (segment != null)
        {
            segment.SpeakerId = speakerId;
        }
        return Task.CompletedTask;
    }

    public async Task<List<ActionItem>> ExtractActionItemsAsync(string meetingId)
    {
        var meeting = await GetMeetingByIdAsync(meetingId);
        if (meeting == null)
        {
            return [];
        }

        var transcriptText = string.Join(" ", meeting.Transcript.Select(t => t.Text));
        var actionItems = await aiIntegrationService.ExtractActionItemsFromTextAsync(transcriptText, meeting.Title);
        
        // Ensure SourceMeetingId is set
        foreach(var item in actionItems) item.SourceMeetingId = meetingId;
        
        // Update the meeting with extracted action items
        meeting.ActionItems = actionItems;
        
        // Update active storage if exists
        if (_activeMeetingsMemory.ContainsKey(meetingId))
        {
            logger.LogInformation("Updated meeting {MeetingId} with {ActionItemCount} action items", meetingId, actionItems.Count);
        }
        
        return actionItems;
    }

    public async Task<List<MindMapNode>> ExtractMindMapNodesAsync(string meetingId)
    {
        var meeting = await GetMeetingByIdAsync(meetingId);
        if (meeting == null)
        {
            return [];
        }

        logger.LogInformation("Extracting mind map nodes for meeting {MeetingId} using intelligent context extraction", meetingId);

        // Use the new intelligent context extractor for multi-node semantic extraction
        var contextResult = await contextExtractor.ExtractContextAsync(meeting.Transcript, meeting.Title);
        var mindMapNodes = contextResult.GeneratedNodes;

        // Ensure SourceMeetingId is set
        foreach(var node in mindMapNodes) node.SourceMeetingId = meetingId;

        // Update the meeting with extracted mind map nodes
        meeting.ExtractedNodes = mindMapNodes;

        // Update active storage if exists
        if (_activeMeetingsMemory.ContainsKey(meetingId))
        {
            _activeMeetingsMemory[meetingId].ExtractedNodes = mindMapNodes;
            logger.LogInformation("Updated meeting {MeetingId} with {MindMapNodeCount} mind map nodes from {SegmentCount} segments using {Method}",
                meetingId, mindMapNodes.Count, contextResult.Metadata.TotalSegments, contextResult.Metadata.ExtractionMethod);
        }

        return mindMapNodes;
    }

    public async Task<Meeting> GetMeetingByIdAsync(string meetingId)
    {
        if (string.IsNullOrEmpty(meetingId)) return null;

        // First check active memory (no DB query needed)
        if (_activeMeetingsMemory.TryGetValue(meetingId, out var activeMeeting))
        {
            return activeMeeting;
        }

        // Create a new scope to avoid disposed session issues
        using var scope = serviceScopeFactory.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<ISession>();

        // Query Orchard Core content items for Meeting content type
        var meetingItems = await session.Query<ContentItem, ContentItemIndex>()
            .Where(ci => ci.ContentType == nameof(Meeting) && ci.Published)
            .ListAsync();
            
        foreach (var contentItem in meetingItems)
        {
            var meetingPart = contentItem.As<MeetingPart>();
            if (meetingPart?.MeetingId == meetingId || contentItem.ContentItemId == meetingId)
            {
                // Convert Orchard Core content item back to Meeting model
                var meeting = new Meeting
                {
                    Id = meetingPart.MeetingId,
                    Title = meetingPart.Title,
                    WorkContextId = meetingPart.WorkContextId,
                    Status = Enum.Parse<MeetingStatus>(meetingPart.Status ?? "Recording"),
                    StartedAt = meetingPart.StartedAt ?? DateTime.UtcNow,
                    EndedAt = meetingPart.EndedAt,
                    DurationSeconds = meetingPart.DurationSeconds,
                    Transcript = meetingPart.Transcript ?? [],
                    ActionItems = meetingPart.ActionItems ?? [],
                    ExtractedNodes = meetingPart.ExtractedNodes ?? [],
                    TranscriptionCompletedAt = meetingPart.TranscriptionCompletedAt,
                    IsProcessed = meetingPart.IsProcessed
                };
                return meeting;
            }
        }
        
        return null;
    }

    public async Task UpdateMeetingAsync(Meeting meeting)
    {
        if (meeting == null || string.IsNullOrEmpty(meeting.Id)) return;

        try
        {
            // Create a new scope to avoid disposed session issues
            using var scope = serviceScopeFactory.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<ISession>();
            var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
            
            // Query Orchard Core content items for Meeting content type
            var meetingItems = await session.Query<ContentItem, ContentItemIndex>()
                .Where(ci => ci.ContentType == nameof(Meeting))
                .ListAsync();

            var contentItem = meetingItems.FirstOrDefault(ci => ci.As<MeetingPart>()?.MeetingId == meeting.Id || ci.ContentItemId == meeting.Id);

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
                    meetingPart.Transcript = meeting.Transcript;
                    meetingPart.ActionItems = meeting.ActionItems;
                    meetingPart.ExtractedNodes = meeting.ExtractedNodes;
                    meetingPart.TranscriptionCompletedAt = meeting.TranscriptionCompletedAt;
                    meetingPart.IsProcessed = meeting.IsProcessed;
                    meetingPart.TranscriptText = string.Join("\n", meeting.Transcript.Select(t => t.Text));

                    contentItem.Apply(meetingPart);
                    await contentManager.UpdateAsync(contentItem);
                    await contentManager.PublishAsync(contentItem);
                    
                    logger.LogInformation("[DB] Successfully updated meeting {MeetingId} with {Count} transcript segments", meeting.Id, meeting.Transcript.Count);
                }
                else
                {
                    logger.LogWarning("[DB] MeetingPart not found on content item {MeetingId}", meeting.Id);
                }
            }
            else
            {
                logger.LogWarning("[DB] Meeting {MeetingId} not found in database", meeting.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DB] Failed to update meeting {MeetingId}", meeting.Id);
            throw;
        }
    }

    public async Task<List<Meeting>> GetAllMeetingsAsync()
    {
        var meetings = new List<Meeting>();
        
        // Create a new scope to avoid disposed session issues
        using var scope = serviceScopeFactory.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<ISession>();
        
        // Query Orchard Core content items for Meeting content type
        var meetingItems = await session.Query<ContentItem, ContentItemIndex>()
            .Where(ci => ci.ContentType == nameof(Meeting) && ci.Published)
            .ListAsync();
            
        foreach (var contentItem in meetingItems)
        {
            var meetingPart = contentItem.As<MeetingPart>();
            if (meetingPart == null) continue;
            
            // If MeetingId is missing in the database, fallback to ContentItemId 
            // to ensure we have a valid key for links and identification.
            var id = string.IsNullOrEmpty(meetingPart.MeetingId) ? contentItem.ContentItemId : meetingPart.MeetingId;
            
            // Convert Orchard Core content item back to Meeting model
            var item = new Meeting
            {
                Id = id,
                Title = contentItem.DisplayText ?? meetingPart.Title ?? "Untitled Meeting",
                WorkContextId = meetingPart.WorkContextId ?? "",
                Status = meetingPart.Status != null && Enum.TryParse<MeetingStatus>(meetingPart.Status, out var status) ? status : MeetingStatus.Recording,
                StartedAt = meetingPart.StartedAt ?? DateTime.UtcNow,
                EndedAt = meetingPart.EndedAt,
                DurationSeconds = meetingPart.DurationSeconds,
                Transcript = meetingPart.Transcript ?? [],
                ActionItems = meetingPart.ActionItems ?? [],
                ExtractedNodes = meetingPart.ExtractedNodes ?? [],
                TranscriptionCompletedAt = meetingPart.TranscriptionCompletedAt,
                IsProcessed = meetingPart.IsProcessed
            };
            meetings.Add(item);
        }
        
        return meetings.OrderByDescending(m => m.StartedAt).ToList();
    }

    public async Task<List<Meeting>> GetMeetingsByContextAsync(string projectId)
    {
        var meetings = new List<Meeting>();
        
        // Create a new scope to avoid disposed session issues
        using var scope = serviceScopeFactory.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<ISession>();
        
        // Query Orchard Core content items for Meeting content type
        var meetingItems = await session.Query<ContentItem, ContentItemIndex>()
            .Where(ci => ci.ContentType == nameof(Meeting) && ci.Published)
            .ListAsync();
            
        foreach (var contentItem in meetingItems)
        {
            var meetingPart = contentItem.As<MeetingPart>();
            if (meetingPart?.WorkContextId != projectId) continue;
            // Convert Orchard Core content item back to Meeting model
            var item = new Meeting
            {
                Id = meetingPart.MeetingId,
                Title = meetingPart.Title,
                WorkContextId = meetingPart.WorkContextId,
                Status = Enum.Parse<MeetingStatus>(meetingPart.Status ?? "Recording"),
                StartedAt = meetingPart.StartedAt ?? DateTime.UtcNow,
                EndedAt = meetingPart.EndedAt,
                DurationSeconds = meetingPart.DurationSeconds,
                Transcript = meetingPart.Transcript ?? [],
                ActionItems = meetingPart.ActionItems ?? [],
                ExtractedNodes = meetingPart.ExtractedNodes ?? [],
                TranscriptionCompletedAt = meetingPart.TranscriptionCompletedAt,
                IsProcessed = meetingPart.IsProcessed
            };
            meetings.Add(item);
        }
        
        return meetings.OrderByDescending(m => m.StartedAt).ToList();
    }

    public List<Meeting> GetActiveMeetings()
    {
        return _activeMeetingsMemory.Values.OrderByDescending(m => m.StartedAt).ToList();
    }

    public async Task<bool> DeleteMeetingAsync(string meetingId)
    {
        if (string.IsNullOrEmpty(meetingId)) return false;

        // 1. Remove from active memory if it exists
        bool removedFromMemory = _activeMeetingsMemory.Remove(meetingId);

        // Create a new scope to avoid disposed session issues
        using var scope = serviceScopeFactory.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<ISession>();
        var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();

        // 2. Find and remove from Orchard Core database
        var meetingItems = await session.Query<ContentItem, ContentItemIndex>()
            .Where(ci => ci.ContentType == nameof(Meeting) && ci.Published)
            .ListAsync();
            
        bool removedFromDb = false;
        foreach (var contentItem in meetingItems)
        {
            var meetingPart = contentItem.As<MeetingPart>();
            if (meetingPart?.MeetingId == meetingId || contentItem.ContentItemId == meetingId)
            {
                await contentManager.RemoveAsync(contentItem);
                removedFromDb = true;
                logger.LogInformation("Deleted meeting content item: {MeetingId}", meetingId);
                break;
            }
        }

        return removedFromMemory || removedFromDb;
    }

    public int GetMaxRecordingDurationSeconds() => 3600; // 1 hour

    private string FormatTranscriptWithContext(Meeting meeting)
    {
        if (meeting.Transcript.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        string? currentSpeakerId = null;
        var speakerMap = meeting.Speakers?.ToDictionary(s => s.Id, s => s.Name) ?? new Dictionary<string, string>();

        foreach (var segment in meeting.Transcript)
        {
            // Group consecutive segments from the same speaker
            if (segment.SpeakerId != currentSpeakerId)
            {
                currentSpeakerId = segment.SpeakerId;
                var speakerName = currentSpeakerId != null && speakerMap.TryGetValue(currentSpeakerId, out var name) 
                    ? name 
                    : (!string.IsNullOrEmpty(currentSpeakerId) ? $"Speaker {currentSpeakerId}" : "Unknown");
                
                sb.Append($"\n{speakerName}: ");
            }
            else
            {
                // Join fragmented snippets with a space
                sb.Append(" ");
            }
            sb.Append(segment.Text);
        }

        return sb.ToString().Trim();
    }
}
