using Newtonsoft.Json;

namespace Compost.Core.Models;

/// <summary>
/// Represents a hierarchical structure node - team/department level organization
/// </summary>
public class StructureNode
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Title of this structure node (team/department name)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the structure
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the project context
    /// </summary>
    public string WorkContextId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the originating tree node
    /// </summary>
    public string? SourceTreeNodeId { get; set; }

    /// <summary>
    /// Parent structure node ID (null for root structure)
    /// </summary>
    public string? ParentStructureId { get; set; }

    /// <summary>
    /// Child structure node IDs
    /// </summary>
    public List<string> ChildStructureIds { get; set; } = [];

    /// <summary>
    /// Type of structure (team, department, project, etc.)
    /// </summary>
    public StructureType StructureType { get; set; } = StructureType.Team;

    /// <summary>
    /// Kanban board ID for this structure
    /// </summary>
    public string? KanbanBoardId { get; set; }

    /// <summary>
    /// Whether this structure has an active kanban board
    /// </summary>
    public bool HasKanbanBoard { get; set; }

    /// <summary>
    /// Team/department members
    /// </summary>
    public List<string> MemberIds { get; set; } = [];

    /// <summary>
    /// Structure-level objectives and goals
    /// </summary>
    public List<string> Objectives { get; set; } = [];

    /// <summary>
    /// Key performance indicators for this structure
    /// </summary>
    public List<string> KpIs { get; set; } = [];

    /// <summary>
    /// Whether this structure has been promoted to kanban cards
    /// </summary>
    public bool IsPromotedToKanban { get; set; }

    /// <summary>
    /// Related kanban card IDs from this structure
    /// </summary>
    public List<string> KanbanCardIds { get; set; } = [];

    /// <summary>
    /// Structure metadata and configuration
    /// </summary>
    public StructureMetadata Metadata { get; set; } = new();

    /// <summary>
    /// When this structure was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this structure was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cosmos DB partition key
    /// </summary>
    [JsonProperty("partitionKey")]
    public string PartitionKey => WorkContextId;
}

/// <summary>
/// Structure metadata and configuration
/// </summary>
public class StructureMetadata
{
    /// <summary>
    /// Team lead or manager ID
    /// </summary>
    public string? LeadId { get; set; }

    /// <summary>
    /// Budget allocation (if applicable)
    /// </summary>
    public decimal? Budget { get; set; }

    /// <summary>
    /// Timeline or sprint duration
    /// </summary>
    public string? Timeline { get; set; }

    /// <summary>
    /// Structure color coding for visual organization
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// External dependencies or integrations
    /// </summary>
    public List<string> ExternalDependencies { get; set; } = [];

    /// <summary>
    /// Communication channels (Slack, Teams, etc.)
    /// </summary>
    public List<string> CommunicationChannels { get; set; } = [];
}

/// <summary>
/// Types of organizational structures
/// </summary>
public enum StructureType
{
    Team,           // Development team
    Department,     // Department level
    Project,        // Project-based structure
    Initiative,     // Strategic initiative
    Program,        // Program management
    Squad,          // Agile squad
    Tribe,          // Agile tribe
    Guild,          // Practice guild
    Chapter,        // Practice chapter
    Committee,      // Committee structure
    WorkingGroup    // Working group
}
