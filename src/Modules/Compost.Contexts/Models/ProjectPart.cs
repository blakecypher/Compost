using System.Text.Json.Serialization;
using OrchardCore.ContentManagement;

namespace Compost.Contexts.Models;

/// <summary>
/// Content part for Project - attaches project-specific data to content items
/// </summary>
public class ProjectPart : ContentPart
{
    /// <summary>
    /// Repository name for this project
    /// </summary>
    [JsonInclude]
    public string? RepositoryName { get; set; }

    /// <summary>
    /// Repository URL
    /// </summary>
    [JsonInclude]
    public string? RepositoryUrl { get; set; }

    /// <summary>
    /// Current branch being worked on
    /// </summary>
    [JsonInclude]
    public string? CurrentBranch { get; set; }

    /// <summary>
    /// Testing steps or commands for this project
    /// </summary>
    [JsonPropertyName("testingSteps")]
    public List<string> TestingSteps { get; set; } = [];

    /// <summary>
    /// Open questions that need answering for this project
    /// </summary>
    [JsonPropertyName("openQuestions")]
    public List<Core.Models.OpenQuestion> OpenQuestions { get; set; } = [];

    /// <summary>
    /// Total time spent in seconds
    /// </summary>
    [JsonInclude]
    public long TotalTimeSpentSeconds { get; set; }

    /// <summary>
    /// Current session start time (UTC)
    /// </summary>
    [JsonInclude]
    public DateTime? CurrentSessionStartedAt { get; set; }

    /// <summary>
    /// Is this project currently active
    /// </summary>
    [JsonInclude]
    public bool IsActive { get; set; }

    /// <summary>
    /// Tags for categorization of this project
    /// </summary>
    [JsonPropertyName("tags")]
    [JsonInclude]
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Description of this project
    /// </summary>
    [JsonInclude]
    public string? Description { get; set; }

    /// <summary>
    /// Notes specific to this project
    /// </summary>
    [JsonInclude]
    public string? Notes { get; set; }

    /// <summary>
    /// Parent project ID for hierarchical organization
    /// </summary>
    [JsonInclude]
    public string? ParentProjectId { get; set; }

    /// <summary>
    /// Display order in tree view
    /// </summary>
    [JsonInclude]
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Workflow status (e.g., "To Do", "In Progress", "Done", "Blocked")
    /// </summary>
    [JsonInclude]
    public string Status { get; set; } = "To Do";

    /// <summary>
    /// Local path where the Git repository is cloned
    /// </summary>
    [JsonInclude]
    public string? GitLocalPath { get; set; }

    /// <summary>
    /// Is Git integration active for this project
    /// </summary>
    [JsonInclude]
    public bool IsGitActive { get; set; }

    /// <summary>
    /// Date and time of last sync with remote
    /// </summary>
    [JsonInclude]
    public DateTime? LastSyncAt { get; set; }
}

/// <summary>
/// Open question within a project
/// </summary>
public class OpenQuestion
{
    [JsonInclude]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonInclude]
    public string Question { get; init; } = string.Empty;
    
    [JsonInclude]
    public string? Answer { get; set; }
    
    [JsonInclude]
    public bool IsResolved { get; set; }
    
    [JsonInclude]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    [JsonInclude]
    public DateTime? ResolvedAt { get; set; }
}
