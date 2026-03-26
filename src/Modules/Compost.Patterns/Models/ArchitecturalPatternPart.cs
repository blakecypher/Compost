using System.Collections.Generic;
using OrchardCore.ContentManagement;

namespace Compost.Patterns.Models;

/// <summary>
/// Content part for Architectural Pattern - stores pattern templates and AI learning data
/// </summary>
public class ArchitecturalPatternPart : ContentPart
{
    /// <summary>
    /// When to use this pattern
    /// </summary>
    public string? WhenToUse { get; set; }

    /// <summary>
    /// How the pattern works
    /// </summary>
    public string? HowItWorks { get; set; }

    /// <summary>
    /// Potential pitfalls or gotchas
    /// </summary>
    public string? Gotchas { get; set; }

    /// <summary>
    /// External documentation links
    /// </summary>
    public List<string> ResourceUrls { get; set; } = [];

    /// <summary>
    /// Keywords for AI suggestion matching
    /// </summary>
    public List<string> Keywords { get; set; } = [];

    /// <summary>
    /// Related code snippet IDs
    /// </summary>
    public List<string> RelatedSnippetIds { get; set; } = [];

    /// <summary>
    /// Success score (adjusted by user feedback)
    /// </summary>
    public double SuccessScore { get; set; } = 1.0;
}
