
using System.Collections.Generic;

namespace Compost.Patterns.ViewModels;

public class PatternViewModel
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string WhenToUse { get; set; }
    public string HowItWorks { get; set; }
    public string Gotchas { get; set; }
    public string Keywords { get; set; }

    /// <summary>One URL per line for Create/Edit form.</summary>
    public string ResourceUrlsText { get; set; }

    /// <summary>For display on Detail (links to external docs).</summary>
    public List<string> ResourceUrls { get; set; } = [];

    /// <summary>Linked code snippets (with id for edit link).</summary>
    public List<LinkedSnippetItem> LinkedSnippets { get; set; } = [];
}

public class LinkedSnippetItem
{
    public string ContentItemId { get; set; }
    public string Title { get; set; }
    public string Language { get; set; }
    public string CodePreview { get; set; }
}
