using Newtonsoft.Json;

namespace Compost.Core.Models;

/// <summary>
/// Represents a team/department level kanban board
/// </summary>
public class KanbanBoard
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Board title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Board description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the structure node that owns this board
    /// </summary>
    public string StructureNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the project context
    /// </summary>
    public string WorkContextId { get; set; } = string.Empty;

    /// <summary>
    /// Custom columns for this board
    /// </summary>
    public List<KanbanColumn> Columns { get; set; } = KanbanBoardDefaults.GetDefaultColumns();

    /// <summary>
    /// Board configuration and settings
    /// </summary>
    public KanbanBoardConfig Config { get; set; } = new();

    /// <summary>
    /// Board access control
    /// </summary>
    public KanbanBoardAccess Access { get; set; } = new();

    /// <summary>
    /// When this board was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this board was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cosmos DB partition key
    /// </summary>
    [JsonProperty("partitionKey")]
    public string PartitionKey => WorkContextId;
}

/// <summary>
/// Kanban board column configuration
/// </summary>
public class KanbanColumn
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public KanbanStatus Status { get; set; }
    public int Order { get; set; }
    public string? Color { get; set; }
    public int? WipLimit { get; set; } // Work in progress limit
}

/// <summary>
/// Kanban board configuration
/// </summary>
public class KanbanBoardConfig
{
    public bool EnableWipLimits { get; set; } = false;
    public bool EnableSwimlanes { get; set; } = false;
    public bool EnableTimeTracking { get; set; } = true;
    public bool EnableStoryPoints { get; set; } = true;
    public string? Theme { get; set; }
    public List<string> Tags { get; set; } = [];
}

/// <summary>
/// Kanban board access control
/// </summary>
public class KanbanBoardAccess
{
    public List<string> AdminIds { get; set; } = [];
    public List<string> MemberIds { get; set; } = [];
    public List<string> ViewerIds { get; set; } = [];
    public bool IsPublic { get; set; } = false;
}

/// <summary>
/// Default kanban columns
/// </summary>
public static class KanbanBoardDefaults
{
    public static List<KanbanColumn> GetDefaultColumns()
    {
        var column = new KanbanColumn
        {
            Name = "Backlog",
            Status = KanbanStatus.Backlog,
            Order = 1,
            Color = "#6c757d"
        };
        return
        [
            column,
            new() { Name = "Ready", Status = KanbanStatus.Ready, Order = 2, Color = "#007bff" },
            new()
            {
                Name = "In Progress", Status = KanbanStatus.InProgress, Order = 3, Color = "#ffc107", WipLimit = 3
            },
            new() { Name = "In Review", Status = KanbanStatus.InReview, Order = 4, Color = "#17a2b8" },
            new() { Name = "Testing", Status = KanbanStatus.Testing, Order = 5, Color = "#6f42c1" },
            new() { Name = "Done", Status = KanbanStatus.Done, Order = 6, Color = "#28a745" }
        ];
    }
}
