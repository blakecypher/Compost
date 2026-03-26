namespace Compost.Core.Interfaces;

public interface IMindMapService
{
    Task<List<MindMapSummary>> GetAllMindMapsAsync();
    Task<MindMapSummary?> GetMindMapSummaryAsync(string id);
    Task<List<MindMapSummary>> GetMindMapsByContextAsync(string workContextId);
}

public class MindMapSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? WorkContextId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int NodeCount { get; set; }
}
