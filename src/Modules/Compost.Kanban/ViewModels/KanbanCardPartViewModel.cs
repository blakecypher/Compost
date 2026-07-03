using System;
using Compost.Core.Models;
using OrchardCore.ContentManagement;

namespace Compost.Kanban.ViewModels;

public class KanbanCardPartViewModel
{
    public KanbanStatus Status { get; set; }
    public int? StoryPoints { get; set; }
    public string WorkContextId { get; set; }
    public bool IsBlocked { get; set; }
    public string BlockedReason { get; set; }
    public DateTime? CompletedDate { get; set; }

    public ContentItem ContentItem { get; set; }
}
