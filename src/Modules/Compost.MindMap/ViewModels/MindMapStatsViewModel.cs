namespace Compost.MindMap.ViewModels;

public class MindMapStatsViewModel
{
    public string MindMapId { get; set; } = string.Empty;
    public string MindMapName { get; set; } = string.Empty;
    public int TotalNodes { get; set; }
    public Dictionary<string, int> NodesByType { get; set; } = new();
    public int RootNodes { get; set; }
    public int LeafNodes { get; set; }
    public int MaxDepth { get; set; }
    public int TotalTags { get; set; }
    public Dictionary<string, int> TopTags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? WorkContextId { get; set; }
    
    public double AverageChildrenPerNode => TotalNodes > 0 
        ? (double)(TotalNodes - RootNodes) / TotalNodes 
        : 0;
}
