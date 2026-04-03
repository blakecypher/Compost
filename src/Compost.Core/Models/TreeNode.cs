using Newtonsoft.Json;

namespace Compost.Core.Models;

/// <summary>
/// Represents a node in the structured tree - more detailed than mind map
/// </summary>
public class TreeNode
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Title of this tree node
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
    /// Reference to the originating mind map node
    /// </summary>
    public string? SourceMindMapNodeId { get; set; }

    /// <summary>
    /// Reference to the originating meeting ID
    /// </summary>
    public string? SourceMeetingId { get; set; }

    /// <summary>
    /// The excerpt from the transcript that generated this requirement
    /// </summary>
    public string? SourceTranscriptExcerpt { get; set; }

    /// <summary>
    /// Parent tree node ID (null for root)
    /// </summary>
    public string? ParentNodeId { get; set; }

    /// <summary>
    /// Child tree node IDs
    /// </summary>
    public List<string> ChildNodeIds { get; set; } = [];

    /// <summary>
    /// Acceptance criteria
    /// </summary>
    public List<string> AcceptanceCriteria { get; set; } = [];

    /// <summary>
    /// Technical requirements
    /// </summary>
    public List<string> TechnicalRequirements { get; set; } = [];

    /// <summary>
    /// Dependencies on other nodes or external factors
    /// </summary>
    public List<Dependency> Dependencies { get; set; } = [];

    /// <summary>
    /// Architectural patterns to be used
    /// </summary>
    public List<string> ArchitecturalPatternIds { get; set; } = [];

    /// <summary>
    /// Estimated complexity/size
    /// </summary>
    public ComplexityLevel Complexity { get; set; } = ComplexityLevel.Unknown;

    /// <summary>
    /// Priority level
    /// </summary>
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

    /// <summary>
    /// Refinement conversation history (Q&A with AI)
    /// </summary>
    public List<RefinementMessage> RefinementHistory { get; set; } = [];

    /// <summary>
    /// Whether this has been promoted to kanban cards
    /// </summary>
    public bool IsPromotedToKanban { get; set; }

    /// <summary>
    /// Related kanban card IDs
    /// </summary>
    public List<string> KanbanCardIds { get; set; } = [];

    /// <summary>
    /// Whether this has been promoted to a structure node
    /// </summary>
    public bool IsPromotedToStructure { get; set; }

    /// <summary>
    /// Reference to the structure node if promoted
    /// </summary>
    public string? StructureNodeId { get; set; }

    /// <summary>
    /// When this node was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this node was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cosmos DB partition key
    /// </summary>
    [JsonProperty("partitionKey")]
    public string PartitionKey => WorkContextId;
}

public class Dependency
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DependencyType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference to another tree node if internal dependency
    /// </summary>
    public string? DependentTreeNodeId { get; set; }
    
    /// <summary>
    /// Whether this dependency is resolved
    /// </summary>
    public bool IsResolved { get; set; }
}

public class RefinementMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum DependencyType
{
    InternalNode,       // Depends on another tree node
    ExternalTeam,       // Depends on another team
    ExternalApi,        // Depends on external API/service
    Infrastructure,     // Depends on infrastructure changes
    DataMigration,      // Depends on data migration
    Other
}

public enum ComplexityLevel
{
    Unknown,
    VeryLow,    // < 1 hour
    Low,        // 1-4 hours
    Medium,     // 4-16 hours (1-2 days)
    High,       // 16-40 hours (2-5 days)
    VeryHigh    // > 40 hours
}

public enum PriorityLevel
{
    Critical,
    High,
    Medium,
    Low,
    Backlog
}

public enum MessageRole
{
    User,
    Assistant,
    System
}
