using System.Collections.Generic;
using Compost.Core.Models;
using OrchardCore.ContentManagement;

namespace Compost.Kanban.ViewModels;

public class KanbanBoardViewModel
{
    public List<Project> Contexts { get; set; } = [];
    public string? SelectedContextId { get; set; }
    public List<ContentItem> Cards { get; set; } = [];
}

public class CardUpdateRequest
{
    public string ContentItemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? StoryPoints { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Assignee { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public string BlockedReason { get; set; } = string.Empty;
    public string SourceTranscriptExcerpt { get; set; } = string.Empty;
}
