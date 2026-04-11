using Compost.Core.Models;

namespace Compost.Core.Interfaces;

/// <summary>
/// Handles meeting recording, transcription, and speaker identification
/// </summary>
public interface ITranscriptionService
{
    /// <summary>
    /// Start recording a meeting
    /// </summary>
    Task<Meeting> StartRecordingAsync(string projectId, string title);

    /// <summary>
    /// Stop recording and begin processing
    /// </summary>
    Task StopRecordingAsync(string meetingId);

    /// <summary>
    /// Get real-time transcript updates during recording
    /// </summary>
    IAsyncEnumerable<TranscriptSegment> GetRealtimeTranscriptAsync(string meetingId);

    /// <summary>
    /// Process a completed recording (transcription + speaker diarization)
    /// </summary>
    Task ProcessRecordingAsync(string meetingId);

    /// <summary>
    /// Process an audio segment (chunk) during live recording
    /// </summary>
    Task ProcessAudioSegmentAsync(string meetingId, byte[] segment);

    /// <summary>
    /// Process an audio file for transcription
    /// </summary>
    Task ProcessAudioAsync(string meetingId, string audioPath);

    /// <summary>
    /// Create or update a speaker voice profile
    /// </summary>
    Task<Speaker> CreateSpeakerProfileAsync(string name, string? role, Stream audioSample);

    /// <summary>
    /// Get all speaker profiles
    /// </summary>
    Task<List<Speaker>> GetSpeakerProfilesAsync();

    /// <summary>
    /// Update speaker identification for a transcript segment
    /// </summary>
    Task UpdateSegmentSpeakerAsync(string meetingId, string segmentId, string speakerId);

    /// <summary>
    /// Extract action items from a meeting transcript
    /// </summary>
    Task<List<ActionItem>> ExtractActionItemsAsync(string meetingId);

    /// <summary>
    /// Extract mind map nodes from a meeting transcript
    /// </summary>
    Task<List<MindMapNode>> ExtractMindMapNodesAsync(string meetingId);

    /// <summary>
    /// Creates proper Orchard Core MindMapNode content items from meeting-extracted nodes.
    /// This bridges the gap between meeting transcription and the decomposition engine pipeline.
    /// </summary>
    Task<List<string>> CreateMindMapNodeContentItemsAsync(string meetingId);

    /// <summary>
    /// Get a meeting by ID
    /// </summary>
    Task<Meeting?> GetMeetingByIdAsync(string meetingId);

    /// <summary>
    /// Get all meetings for a context
    /// </summary>
    Task<List<Meeting>> GetMeetingsByContextAsync(string projectId);

    /// <summary>
    /// Get all meetings regardless of context
    /// </summary>
    Task<List<Meeting>> GetAllMeetingsAsync();

    /// <summary>
    /// Get all active meetings from memory storage
    /// </summary>
    List<Meeting> GetActiveMeetings();

    /// <summary>
    /// Add a transcript segment to a meeting (used during live recording)
    /// </summary>
    Task AddTranscriptSegmentAsync(string meetingId, TranscriptSegment segment);

    /// <summary>
    /// Update an existing meeting's data in the database
    /// </summary>
    Task UpdateMeetingAsync(Meeting meeting);

    /// <summary>
    /// Delete a meeting by ID
    /// </summary>
    Task<bool> DeleteMeetingAsync(string meetingId);

    /// <summary>
    /// Get the maximum recording duration in seconds
    /// </summary>
    int GetMaxRecordingDurationSeconds();
}
