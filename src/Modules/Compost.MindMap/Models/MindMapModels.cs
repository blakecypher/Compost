using Compost.Core.Models;

namespace Compost.MindMap.Models;

// ── MindMapNode ───────────────────────────────────────────────────────────────
// The canonical MindMapNode has been consolidated into Compost.Core.Models.MindMapNode.
// All code in this module should reference that type directly.
// The type alias below is kept for backwards compatibility with any tooling/serialisers
// that resolve against this namespace.
using MindMapNode = Compost.Core.Models.MindMapNode;

// ── MindMapNodeStatus ─────────────────────────────────────────────────────────
// Status values are now stored as the string property MindMapNode.Status.
// This enum is retained for switch-statement compatibility.
public enum MindMapNodeStatus
{
    Draft,
    Pending,
    Approved,
    Rejected
}

/// <summary>
/// A mind map collection for a project context.
/// </summary>
public class MindMapCollection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? WorkContextId { get; set; }
    public string? Description { get; set; }
    public List<MindMapNode> Nodes { get; set; } = [];
    public List<NodeEdge> Edges { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Parsed segment from text/transcript used during initial mind-map generation.
/// </summary>
public class ParsedSegment
{
    public string Text { get; set; } = string.Empty;
    public string SegmentType { get; set; } = "Idea"; // Idea, Requirement, Question, Action
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public double Confidence { get; set; } = 1.0;
}
