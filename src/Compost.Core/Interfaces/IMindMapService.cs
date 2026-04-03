namespace Compost.Core.Interfaces;

public interface IMindMapService
{
    Task<List<MindMapSummary>> GetAllMindMapsAsync();
    Task<MindMapSummary?> GetMindMapSummaryAsync(string id);
    Task<List<MindMapSummary>> GetMindMapsByContextAsync(string workContextId);
}

public class MindMapSummary
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? WorkContextId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int NodeCount { get; init; }
}
