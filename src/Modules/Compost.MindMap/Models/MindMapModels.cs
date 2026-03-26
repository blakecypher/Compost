using System.ComponentModel.DataAnnotations;

namespace Compost.MindMap.Models;

/// <summary>
/// Represents a mind map node in the procedural flow
/// </summary>
public class MindMapNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Node type: Root, Idea, Requirement, Question, Action, Decision, Risk, Note
    /// </summary>
    public string NodeType { get; set; } = "Idea";
    
    /// <summary>
    /// Parent node ID (null for root nodes)
    /// </summary>
    public string? ParentId { get; set; }
    
    /// <summary>
    /// Child node IDs
    /// </summary>
    public List<string> ChildIds { get; set; } = [];
    
    /// <summary>
    /// Position for visualization
    /// </summary>
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    
    /// <summary>
    /// Visual properties
    /// </summary>
    public string? Color { get; set; }
    public string? Icon { get; set; } // FontAwesome icon class
    public int Level { get; set; } // Depth in the tree
    public string? Shape { get; set; } = "circle";
    public int FontSize { get; set; } = 12;
    public double Size { get; set; } = 1.0;
    public List<Compost.Core.Models.NodeEdge> Edges { get; set; } = [];
    
    /// <summary>
    /// Source information - where this node came from
    /// </summary>
    public string? SourceType { get; set; } // "Manual", "Transcript", "Requirement", "TextParse"
    public string? SourceReference { get; set; } // Meeting ID, document ID, etc.
    public string? SourceTimestamp { get; set; } // Time in transcript
    public string? SourceText { get; set; } // Original text that generated this node
    
    /// <summary>
    /// Metadata and State
    /// </summary>
    public List<string> Tags { get; set; } = [];
    public string? Notes { get; set; }
    public bool IsExpanded { get; set; } = true;
    public string Status { get; set; } = "Draft"; // Draft, Pending, Approved, Rejected
    public bool IsPromoted { get; set; }
    public string? PromotedToId { get; set; } // Reference to created ProjectContext or TreeNode
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum MindMapNodeStatus
{
    Draft,
    Pending,
    Approved,
    Rejected
}

/// <summary>
/// A mind map collection for a project context
/// </summary>
public class MindMapCollection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? WorkContextId { get; set; }
    public string? Description { get; set; }
    public List<MindMapNode> Nodes { get; set; } = [];
    public List<Compost.Core.Models.NodeEdge> Edges { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Parsed segment from text/transcript
/// </summary>
public class ParsedSegment
{
    public string Text { get; set; } = string.Empty;
    public string SegmentType { get; set; } = "Idea"; // Idea, Requirement, Question, Action
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public double Confidence { get; set; } = 1.0;
}
