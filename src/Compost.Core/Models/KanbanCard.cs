using Newtonsoft.Json;

namespace Compost.Core.Models;

/// <summary>
/// Represents an actionable task card on the Kanban board
/// </summary>
public class KanbanCard
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Task title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the project context
    /// </summary>
    public string WorkContextId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the source tree node
    /// </summary>
    public string? SourceTreeNodeId { get; set; }

    /// <summary>
    /// Reference to the source meeting if applicable
    /// </summary>
    public string? SourceMeetingId { get; set; }

    /// <summary>
    /// The excerpt from the transcript that generated this card
    /// </summary>
    public string? SourceTranscriptExcerpt { get; set; }

    /// <summary>
    /// Reference to the source structure node
    /// </summary>
    public string? SourceStructureNodeId { get; set; }

    /// <summary>
    /// Reference to the kanban board this card belongs to
    /// </summary>
    public string? KanbanBoardId { get; set; }

    /// <summary>
    /// Story points estimation
    /// </summary>
    public int? StoryPoints { get; set; }

    /// <summary>
    /// AI-suggested story points (before user confirmation)
    /// </summary>
    public int? SuggestedStoryPoints { get; set; }

    /// <summary>
    /// Current status column
    /// </summary>
    public KanbanStatus Status { get; set; } = KanbanStatus.Backlog;

    /// <summary>
    /// Priority within the status column
    /// </summary>
    public int OrderInColumn { get; set; }

    /// <summary>
    /// Acceptance criteria
    /// </summary>
    public List<string> AcceptanceCriteria { get; set; } = [];

    /// <summary>
    /// Checklist items for this task
    /// </summary>
    public List<ChecklistItem> Checklist { get; set; } = [];

    /// <summary>
    /// Related code snippets
    /// </summary>
    public List<string> CodeSnippetIds { get; set; } = [];

    /// <summary>
    /// Related architectural patterns
    /// </summary>
    public List<string> ArchitecturalPatternIds { get; set; } = [];

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Blocked status and reason
    /// </summary>
    public BlockedInfo? BlockedInfo { get; set; }

    /// <summary>
    /// When work started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When work completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Time spent on this task (in seconds)
    /// </summary>
    public long TimeSpentSeconds { get; set; }

    /// <summary>
    /// When this card was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this card was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cosmos DB partition key
    /// </summary>
    [JsonProperty("partitionKey")]
    public string PartitionKey => WorkContextId;
}

public class ChecklistItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class BlockedInfo
{
    public bool IsBlocked { get; set; }
    public string? Reason { get; set; }
    public DateTime? BlockedSince { get; set; }
    
    /// <summary>
    /// Reference to dependency or other blocker
    /// </summary>
    public string? BlockerReference { get; set; }
}

public enum KanbanStatus
{
    Backlog,
    Ready,
    InProgress,
    InReview,
    Testing,
    Done
}
