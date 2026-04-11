using Newtonsoft.Json;

namespace Compost.Core.Models;

/// <summary>
/// Represents a recorded meeting with transcription
/// </summary>
public class Meeting
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Meeting title/name
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description or context
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Reference to the project context
    /// </summary>
    public string WorkContextId { get; set; } = string.Empty;

    /// <summary>
    /// Azure Blob Storage URL for the audio recording
    /// </summary>
    public string? RecordingBlobUrl { get; set; }

    /// <summary>
    /// Duration of the recording in seconds
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Status of the meeting recording/transcription
    /// </summary>
    public MeetingStatus Status { get; set; } = MeetingStatus.Recording;

    /// <summary>
    /// Full transcript of the meeting
    /// </summary>
    public List<TranscriptSegment> Transcript { get; set; } = [];

    /// <summary>
    /// Identified speakers in the meeting
    /// </summary>
    public List<Speaker> Speakers { get; set; } = [];

    /// <summary>
    /// Mind map nodes extracted from this meeting
    /// </summary>
    public List<string> ExtractedNodeIds { get; set; } = [];

    /// <summary>
    /// Mind map nodes extracted from this meeting (convenience property)
    /// </summary>
    public List<MindMapNode> ExtractedNodes { get; set; } = [];

    /// <summary>
    /// Whether this meeting has been processed
    /// </summary>
    public bool IsProcessed { get; set; }

    /// <summary>
    /// Action items identified during the meeting
    /// </summary>
    public List<ActionItem> ActionItems { get; set; } = [];

    /// <summary>
    /// Key decisions made during the meeting
    /// </summary>
    public List<string> KeyDecisions { get; set; } = [];

    /// <summary>
    /// Questions raised during the meeting
    /// </summary>
    public List<string> QuestionsRaised { get; set; } = [];

    /// <summary>
    /// When the meeting started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the meeting ended
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// When transcription processing completed
    /// </summary>
    public DateTime? TranscriptionCompletedAt { get; set; }
}

public class TranscriptSegment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Speaker ID (reference to Speaker)
    /// </summary>
    public string? SpeakerId { get; set; }
    
    /// <summary>
    /// The transcribed text
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Start time in the recording
    /// </summary>
    public TimeSpan StartTime { get; set; }
    
    /// <summary>
    /// End time in the recording
    /// </summary>
    public TimeSpan EndTime { get; set; }
    
    /// <summary>
    /// Confidence score from speech service (0.0 - 1.0)
    /// </summary>
    public double Confidence { get; set; }
    
    /// <summary>
    /// Whether this segment is an interim/partial result
    /// </summary>
    public bool IsInterim { get; set; }

    /// <summary>
    /// Whether this segment has been processed/reviewed
    /// </summary>
    public bool IsProcessed { get; set; }
    
    /// <summary>
    /// Culture context for localization (e.g., "en-US", "es-ES", "fr-FR")
    /// </summary>
    public string Culture { get; set; } = "en-US";
    
    /// <summary>
    /// Localized context dictionary for dynamic parameters
    /// </summary>
    public Dictionary<string, string> Context { get; set; } = new();
}

public class Speaker
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Display name for the speaker
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Role or designation (e.g., "CTO", "Me", "Tech Lead")
    /// </summary>
    public string? Role { get; set; }
    
    /// <summary>
    /// Voice profile ID from Azure Speech Service
    /// </summary>
    public string? VoiceProfileId { get; set; }
    
    /// <summary>
    /// Color coding for this speaker in UI
    /// </summary>
    public string? Color { get; set; }
}

public class ActionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Title of the action item
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Priority level (High, Medium, Low)
    /// </summary>
    public string Priority { get; set; } = "Medium";
    
    /// <summary>
    /// Current status (Open, In Progress, Completed, Blocked)
    /// </summary>
    public string Status { get; set; } = "Open";
    
    /// <summary>
    /// When this action item was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Who is responsible
    /// </summary>
    public string? AssignedTo { get; set; }
    
    /// <summary>
    /// Reference to transcript segment where this was mentioned
    /// </summary>
    public string? TranscriptSegmentId { get; set; }
    
    /// <summary>
    /// Whether this has been converted to a task
    /// </summary>
    public bool IsConvertedToTask { get; set; }
    
    /// <summary>
    /// Reference to created kanban card if converted
    /// </summary>
    public string? KanbanCardId { get; set; }

    /// <summary>
    /// The original transcript text that generated this action item
    /// </summary>
    public string? OriginalTranscript { get; set; }

    /// <summary>
    /// Reference to source meeting ID
    /// </summary>
    public string? SourceMeetingId { get; set; }
}

public enum MeetingStatus
{
    Recording,          // Currently being recorded
    Processing,         // Transcription in progress
    Completed,          // Transcription done, ready for review
    Reviewed,           // User has reviewed and extracted info
    Archived,           // Archived for future reference
    Failed              // Processing failed due to error
}
