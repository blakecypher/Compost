using System.Text.Json.Serialization;
using OrchardCore.ContentManagement;

namespace Compost.Contexts.Models;

/// <summary>
/// Content part for storing project template data
/// </summary>
public class ProjectTemplatePart : ContentPart
{
    /// <summary>
    /// Template name
    /// </summary>
    [JsonInclude]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    [JsonInclude]
    public string? Description { get; set; }

    /// <summary>
    /// Default repository name
    /// </summary>
    [JsonInclude]
    public string? DefaultRepositoryName { get; set; }

    /// <summary>
    /// Default repository URL
    /// </summary>
    [JsonInclude]
    public string? DefaultRepositoryUrl { get; set; }

    /// <summary>
    /// Default branch
    /// </summary>
    [JsonInclude]
    public string? DefaultBranch { get; set; }

    /// <summary>
    /// Default tags
    /// </summary>
    [JsonPropertyName("defaultTags")]
    [JsonInclude]
    public List<string> DefaultTags { get; set; } = [];

    /// <summary>
    /// Predefined testing steps
    /// </summary>
    [JsonPropertyName("testingSteps")]
    [JsonInclude]
    public List<string> TestingSteps { get; set; } = [];

    /// <summary>
    /// Template notes/instructions
    /// </summary>
    [JsonInclude]
    public string? Notes { get; set; }

    /// <summary>
    /// Category for grouping templates
    /// </summary>
    [JsonInclude]
    public string? Category { get; set; }

    /// <summary>
    /// Whether this template is built-in (not deletable)
    /// </summary>
    [JsonInclude]
    public bool IsBuiltIn { get; set; }
}
