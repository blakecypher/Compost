using Newtonsoft.Json;

namespace Compost.Core.Models;

/// <summary>
/// Represents a node in the mind map - raw ideas, meeting notes, requirements.
/// This is the single canonical MindMapNode used across every layer of the application.
/// </summary>
public class MindMapNode
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Title/summary of this node.
    /// Also exposed as <see cref="Text"/> for the rendering pipeline.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Alias for <see cref="Title"/> used by the MindMap rendering pipeline.</summary>
    [JsonIgnore]
    public string Text { get => Title; set => Title = value; }

    /// <summary>Detailed content/body of this node.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Additional freeform notes displayed in the node detail panel.</summary>
    public string? Notes { get; set; }

    /// <summary>Reference to the project context this belongs to.</summary>
    public string WorkContextId { get; set; } = string.Empty;

    /// <summary>Parent node ID (null for root nodes).</summary>
    public string? ParentNodeId { get; set; }

    /// <summary>Alias for <see cref="ParentNodeId"/> used by the rendering pipeline.</summary>
    [JsonIgnore]
    public string? ParentId { get => ParentNodeId; set => ParentNodeId = value; }

    /// <summary>Child node IDs.</summary>
    public List<string> ChildNodeIds { get; set; } = [];

    /// <summary>Backward compatibility for code that expects ChildIds.</summary>
    [JsonIgnore]
    public List<string> ChildIds { get => ChildNodeIds; set => ChildNodeIds = value; }

    /// <summary>Position in the visual mind map.</summary>
    public NodePosition Position { get; set; } = new();

    /// <summary>Direct X position accessor.</summary>
    [JsonIgnore]
    public double PositionX { get => Position.X; set => Position.X = value; }

    /// <summary>Direct Y position accessor.</summary>
    [JsonIgnore]
    public double PositionY { get => Position.Y; set => Position.Y = value; }

    /// <summary>Color coding for visual organization.</summary>
    public string? Color { get; set; }

    /// <summary>FontAwesome icon class for the node.</summary>
    public string? Icon { get; set; }

    /// <summary>Depth level in the tree (0 = root).</summary>
    public int Level { get; set; }

    /// <summary>Type of node (requirement, idea, note, action, etc.).</summary>
    public string NodeType { get; set; } = "Idea";

    /// <summary>Workflow status: Draft, Pending, Approved, Rejected.</summary>
    public string Status { get; set; } = "Draft";

    /// <summary>Whether the node is expanded in the UI.</summary>
    public bool IsExpanded { get; set; } = true;

    /// <summary>Source provenance of this node.</summary>
    public NodeSource Source { get; set; } = new();

    /// <summary>Original text from the transcript/document that generated this node.</summary>
    public string? SourceText { get; set; }

    /// <summary>Timestamp string within the source transcript or recording.</summary>
    public string? SourceTimestamp { get; set; }

    /// <summary>Backward compatibility for code that expects SourceType as string.</summary>
    [JsonIgnore]
    public string SourceType 
    { 
        get => Source.Type.ToString(); 
        set => Source.Type = Enum.TryParse<NodeSourceType>(value, true, out var t) ? t : NodeSourceType.Manual; 
    }

    /// <summary>Backward compatibility for code that expects SourceReference.</summary>
    [JsonIgnore]
    public string? SourceReference { get => Source.ReferenceId; set => Source.ReferenceId = value; }

    // ── Promotion state ───────────────────────────────────────────────────────

    /// <summary>Whether this node has been promoted to a tree node.</summary>
    public bool IsPromotedToTree { get; set; }

    /// <summary>TreeNode ID if promoted.</summary>
    public string? TreeNodeId { get; set; }

    /// <summary>Whether this node has been promoted to a Kanban card.</summary>
    public bool IsPromoted { get; set; }

    /// <summary>Generic promoted-resource ID. Prefer typed IDs below where possible.</summary>
    public string? PromotedToId { get; set; }

    /// <summary>
    /// Kanban card ID when this node has been promoted to the board.
    /// Replaces the previous pattern of encoding this into the <see cref="Notes"/> field.
    /// </summary>
    public string? KanbanCardId { get; set; }

    /// <summary>Whether this node has been promoted to a structure node.</summary>
    public bool IsPromotedToStructure { get; set; }

    /// <summary>StructureNode ID if promoted.</summary>
    public string? StructureNodeId { get; set; }

    // ── Content & metadata ────────────────────────────────────────────────────

    /// <summary>Longer description shown in detail panels.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Original transcript excerpt that produced this node.</summary>
    public string? OriginalTranscript { get; set; }

    /// <summary>Reference to the source meeting.</summary>
    public string? SourceMeetingId { get; set; }

    /// <summary>Shape for visualization (circle, rectangle, diamond, etc.).</summary>
    public string? Shape { get; set; } = "circle";

    /// <summary>Font size for the node label.</summary>
    public int FontSize { get; set; } = 12;

    /// <summary>Relative scale of the node.</summary>
    public double Size { get; set; } = 1.0;

    /// <summary>Edges/connections to other nodes.</summary>
    public List<NodeEdge> Edges { get; set; } = [];

    /// <summary>AI-suggested architectural pattern IDs relevant to this node.</summary>
    public List<string> SuggestedPatternIds { get; set; } = [];

    /// <summary>Tags for categorisation.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>When this node was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When this node was last modified.</summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Cosmos DB partition key.</summary>
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

    /// <summary>Reference ID (e.g., meeting ID if from a meeting).</summary>
    public string? ReferenceId { get; set; }

    /// <summary>Timestamp in the source (e.g., timestamp in meeting recording).</summary>
    public TimeSpan? SourceTimestamp { get; set; }
}

public enum MindMapNodeType
{
    Root,
    Idea,
    Requirement,
    Note,
    Action,
    Question,
    Decision,
    Risk,
    Goal,
    Timeline,
    Resource,
    Recommendation,
    Optional
}

public enum NodeSourceType
{
    Manual,
    Meeting,
    TeamsMessage,
    Email,
    TicketImport
}
