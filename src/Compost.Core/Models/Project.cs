using Newtonsoft.Json;

namespace Compost.Core.Models;

/// <summary>
/// Represents an isolated project/workspace for a project or feature
/// </summary>
public class Project
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User-friendly name for the project (e.g., "Frontend Refactor", "API Optimization")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description or notes about this project
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The repository this project is associated with
    /// </summary>
    public string? RepositoryName { get; set; }

    /// <summary>
    /// URL to the repository
    /// </summary>
    public string? RepositoryUrl { get; set; }

    /// <summary>
    /// Current branch being worked on
    /// </summary>
    public string? CurrentBranch { get; set; }

    /// <summary>
    /// Testing steps or commands needed for this project
    /// </summary>
    public List<string> TestingSteps { get; set; } = [];

    /// <summary>
    /// Open questions that need answering
    /// </summary>
    public List<OpenQuestion> OpenQuestions { get; set; } = [];

    /// <summary>
    /// Notes specific to this project
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Root mind map nodes for this project
    /// </summary>
    public List<string> MindMapNodeIds { get; set; } = [];

    /// <summary>
    /// Total time spent in this project (in seconds)
    /// </summary>
    public long TotalTimeSpentSeconds { get; set; }

    /// <summary>
    /// When this project was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this project was last accessed
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the current session started (null if not active)
    /// </summary>
    public DateTime? CurrentSessionStartedAt { get; set; }

    /// <summary>
    /// Whether this project is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Cosmos DB partition key
    /// </summary>
    [JsonProperty("partitionKey")]
    public string PartitionKey => "project";

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<string>? Tags { get; set; } = [];

    /// <summary>
    /// Parent project ID for hierarchical organization
    /// </summary>
    public string? ParentProjectId { get; set; }

    /// <summary>
    /// Display order in tree view
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Status for project workflow
    /// </summary>
    public string Status { get; set; } = "To Do";
}

/// <summary>
/// Represents an open question that needs answering
/// </summary>
public class OpenQuestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Question { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
