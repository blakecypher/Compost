using System.Collections.Generic;
using Compost.Core.Models;
using OrchardCore.ContentManagement;

namespace Compost.Kanban.ViewModels;

public class TreeNodePartViewModel
{
    public string? WorkContextId { get; set; }
    public ComplexityLevel Complexity { get; set; }
    public PriorityLevel Priority { get; set; }
    public List<string> AcceptanceCriteria { get; set; } = [];
    public List<string> TechnicalRequirements { get; set; } = [];
    public bool IsPromotedToKanban { get; set; }

    public ContentItem? ContentItem { get; set; }
}
