
using System.Collections.Generic;

namespace Compost.Snippets.ViewModels;

public class SnippetViewModel
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Code { get; set; }
    public string Language { get; set; }
    public string Category { get; set; }
    public string Tags { get; set; } // Comma separated for editing
    public string ProjectName { get; set; }
    public string RelatedPatternId { get; set; }
    
    // For SelectList in view
    public Dictionary<string, string> AvailablePatterns { get; set; } = new();
}
