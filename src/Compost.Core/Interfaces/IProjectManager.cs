using Compost.Core.Models;

namespace Compost.Core.Interfaces;

/// <summary>
/// Manages projects - switching, snapshots, time tracking
/// </summary>
public interface IProjectManager
{
    /// <summary>
    /// Get all projects ordered by last accessed
    /// </summary>
    Task<List<Project>> GetAllProjectsAsync();

    /// <summary>
    /// Get the currently active project
    /// </summary>
    Task<Project?> GetActiveProjectAsync();

    /// <summary>
    /// Get a specific project by ID
    /// </summary>
    Task<Project?> GetProjectByIdAsync(string projectId);

    /// <summary>
    /// Create a new project
    /// </summary>
    Task<Project> CreateProjectAsync(string name, string? description = null, string? repositoryName = null, string? repositoryUrl = null, string? currentBranch = null, List<string>? tags = null, string status = "To Do", string? parentProjectId = null, int displayOrder = 0, string? gitLocalPath = null, bool isGitActive = false);

    /// <summary>
    /// Switch to a different project
    /// </summary>
    Task SwitchProjectAsync(string projectId);

    /// <summary>
    /// Update an existing project
    /// </summary>
    Task UpdateProjectAsync(Project project);

    /// <summary>
    /// Delete a project
    /// </summary>
    Task DeleteProjectAsync(string projectId);

    /// <summary>
    /// Start a new session in the current project (for time tracking)
    /// </summary>
    Task StartSessionAsync(string projectId);

    /// <summary>
    /// End the current session and update time tracking
    /// </summary>
    Task EndSessionAsync(string projectId);

    /// <summary>
    /// Get total time spent across all projects
    /// </summary>
    Task<Dictionary<string, TimeSpan>> GetTimeSpentByProjectAsync();

    /// <summary>
    /// Add a testing step to a project
    /// </summary>
    Task AddTestingStepAsync(string projectId, string step);

    /// <summary>
    /// Add an open question to a project
    /// </summary>
    Task AddOpenQuestionAsync(string projectId, string question);

    /// <summary>
    /// Resolve an open question
    /// </summary>
    Task ResolveQuestionAsync(string projectId, string questionId, string answer);
}
