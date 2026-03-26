using Newtonsoft.Json;

namespace Compost.Core.Models;

/// <summary>
/// Represents a node in the mind map - raw ideas, meeting notes, requirements
/// </summary>
public class MindMapNode
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Title/summary of this node
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed content/notes
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the project context this belongs to
    /// </summary>
    public string WorkContextId { get; set; } = string.Empty;

    /// <summary>
    /// Parent node ID (null for root nodes)
    /// </summary>
    public string? ParentNodeId { get; set; }

    /// <summary>
    /// Child node IDs
    /// </summary>
    public List<string> ChildNodeIds { get; set; } = [];

    /// <summary>
    /// Position in the visual mind map
    /// </summary>
    public NodePosition Position { get; set; } = new();

    /// <summary>
    /// Color coding for visual organization
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Type of node (requirement, idea, note, action, etc.)
    /// </summary>
    public MindMapNodeType NodeType { get; set; } = MindMapNodeType.Idea;

    /// <summary>
    /// Source of this node (meeting, manual entry, etc.)
    /// </summary>
    public NodeSource Source { get; set; } = new();

    /// <summary>
    /// Whether this node has been promoted to a tree node
    /// </summary>
    public bool IsPromotedToTree { get; set; }

    /// <summary>
    /// Reference to the tree node if promoted
    /// </summary>
    public string? TreeNodeId { get; set; }

    /// <summary>
    /// Whether this node has been promoted to a structure node
    /// </summary>
    public bool IsPromotedToStructure { get; set; }

    /// <summary>
    /// Reference to the structure node if promoted
    /// </summary>
    public string? StructureNodeId { get; set; }

    /// <summary>
    /// Description of the node (different from title)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Original transcript text that generated this node
    /// </summary>
    public string? OriginalTranscript { get; set; }

    /// <summary>
    /// Reference to source meeting ID
    /// </summary>
    public string? SourceMeetingId { get; set; }

    /// <summary>
    /// Shape of the node for visualization (circle, rectangle, diamond, etc.)
    /// </summary>
    public string? Shape { get; set; } = "circle";

    /// <summary>
    /// Font size for the node text
    /// </summary>
    public int FontSize { get; set; } = 12;

    /// <summary>
    /// Size of the node (width/height)
    /// </summary>
    public double Size { get; set; } = 1.0;

    /// <summary>
    /// Edges/connections to other nodes
    /// </summary>
    public List<NodeEdge> Edges { get; set; } = [];

    /// <summary>
    /// AI-suggested architectural patterns relevant to this node
    /// </summary>
    public List<string> SuggestedPatternIds { get; set; } = [];

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = [];

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

public class NodePosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class NodeEdge
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FromNodeId { get; set; } = string.Empty;
    public string ToNodeId { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string Type { get; set; } = "default";
}

public class NodeSource
{
    public NodeSourceType Type { get; set; } = NodeSourceType.Manual;
    
    /// <summary>
    /// Reference ID (e.g., meeting ID if from a meeting)
    /// </summary>
    public string? ReferenceId { get; set; }
    
    /// <summary>
    /// Timestamp in the source (e.g., timestamp in meeting recording)
    /// </summary>
    public TimeSpan? SourceTimestamp { get; set; }
}

public enum MindMapNodeType
{
    Idea,
    Requirement,
    Note,
    Action,
    Question,
    Decision,
    Risk
}

public enum NodeSourceType
{
    Manual,
    Meeting,
    TeamsMessage,
    Email,
    TicketImport
}
