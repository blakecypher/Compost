using OrchardCore.ContentManagement;

namespace Compost.MindMap.Models;

/// <summary>
/// Content part for Mind Map Node - stores position and visualization data
/// </summary>
public class MindMapNodePart : ContentPart
{
    /// <summary>
    /// X position in the mind map canvas
    /// </summary>
    public double PositionX { get; set; }

    /// <summary>
    /// Y position in the mind map canvas
    /// </summary>
    public double PositionY { get; set; }

    /// <summary>
    /// Color for this node (hex code)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Node type (Idea, Requirement, Note, Action, Question, Decision, Risk)
    /// </summary>
    public string NodeType { get; set; } = "Idea";

    /// <summary>
    /// Parent node content item ID
    /// </summary>
    public string? ParentNodeId { get; set; }

    /// <summary>
    /// Reference to the project context this belongs to
    /// </summary>
    public string? WorkContextId { get; set; }

    /// <summary>
    /// Whether this node has been promoted to a tree node
    /// </summary>
    public bool IsPromotedToTree { get; set; }

    /// <summary>
    /// Reference to the tree node if promoted
    /// </summary>
    public string? TreeNodeId { get; set; }

    /// <summary>
    /// Source information (from meeting, manual, etc.)
    /// </summary>
    public string? SourceType { get; set; }

    /// <summary>
    /// Source reference ID (e.g., meeting ID)
    /// </summary>
    public string? SourceReferenceId { get; set; }

    /// <summary>
    /// Timestamp in source (e.g., time in meeting recording)
    /// </summary>
    public TimeSpan? SourceTimestamp { get; set; }

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = [];
}
