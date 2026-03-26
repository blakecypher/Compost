using Compost.Core.Models;

namespace Compost.Contexts.Services;

/// <summary>
/// Service for managing context templates
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Get all available templates
    /// </summary>
    Task<List<ContextTemplate>> GetAllTemplatesAsync();

    /// <summary>
    /// Get a template by ID
    /// </summary>
    Task<ContextTemplate?> GetTemplateByIdAsync(string templateId);

    /// <summary>
    /// Create a new template from an existing project
    /// </summary>
    Task<ContextTemplate> CreateTemplateFromContextAsync(string projectId, string templateName, string? description = null);

    /// <summary>
    /// Create a new template from scratch
    /// </summary>
    Task<ContextTemplate> CreateTemplateAsync(string name, string? description, string? repositoryName,
        string? repositoryUrl, string? branch, List<string>? tags, List<string>? testingSteps, string? category);

    /// <summary>
    /// Delete a template
    /// </summary>
    Task DeleteTemplateAsync(string templateId);

    /// <summary>
    /// Create a new context from a template
    /// </summary>
    Task<Project> CreateContextFromTemplateAsync(string templateId, string contextName);

    /// <summary>
    /// Get templates by category
    /// </summary>
    Task<List<ContextTemplate>> GetTemplatesByCategoryAsync(string category);

    /// <summary>
    /// Get all template categories
    /// </summary>
    Task<List<string>> GetTemplateCategoriesAsync();
}

/// <summary>
/// Project template data transfer object
/// </summary>
public class ContextTemplate
{
    public string Id { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultRepositoryName { get; set; }
    public string? DefaultRepositoryUrl { get; set; }
    public string? DefaultBranch { get; set; }
    public List<string> DefaultTags { get; set; } = [];
    public List<string> TestingSteps { get; set; } = [];
    public string? Notes { get; set; }
    public string? Category { get; set; }
    public bool IsBuiltIn { get; set; }
    public DateTime CreatedAt { get; set; }
}
