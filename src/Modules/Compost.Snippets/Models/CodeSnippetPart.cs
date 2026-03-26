using System.Collections.Generic;
using OrchardCore.ContentManagement;

namespace Compost.Snippets.Models;

/// <summary>
/// Content part for Code Snippet - stores code, language, and metadata
/// </summary>
public class CodeSnippetPart : ContentPart
{
    /// <summary>
    /// Programming language (e.g., csharp, javascript, python)
    /// </summary>
    public string Language { get; set; } = "csharp";

    /// <summary>
    /// The actual code content
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Project name this snippet originated from
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>
    /// Category for classification
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Tags for searchable categorization
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Documentation or explanation of the snippet
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// Reference to a related architectural pattern
    /// </summary>
    public string? RelatedPatternId { get; set; }
}
