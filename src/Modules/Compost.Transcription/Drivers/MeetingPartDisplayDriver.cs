using System;
using System.Threading.Tasks;
using Compost.Transcription.Models;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Display.ViewModels;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;

namespace Compost.Transcription.Drivers;

public class MeetingPartDisplayDriver : ContentPartDisplayDriver<MeetingPart>
{
    public override IDisplayResult Display(MeetingPart part, BuildPartDisplayContext context)
    {
        return Initialize<MeetingPartViewModel>(nameof(MeetingPart), m =>
        {
            m.MeetingId = part.MeetingId;
            m.Title = part.Title;
            m.WorkContextId = part.WorkContextId;
            m.Status = part.Status;
            m.StartedAt = part.StartedAt;
            m.EndedAt = part.EndedAt;
            m.DurationSeconds = part.DurationSeconds;
            m.TranscriptionCompletedAt = part.TranscriptionCompletedAt;
            m.IsProcessed = part.IsProcessed;
            m.TranscriptCount = part.Transcript?.Count ?? 0;
            m.ActionItemCount = part.ActionItems?.Count ?? 0;
            m.ExtractedNodesCount = part.ExtractedNodes?.Count ?? 0;
        });
    }

    public override IDisplayResult Edit(MeetingPart part, BuildPartEditorContext context)
    {
        return Initialize<MeetingPartViewModel>("MeetingPart.Edit", m =>
        {
            m.Title = part.Title;
            m.WorkContextId = part.WorkContextId;
            m.AutoExtractMindMapNodes = part.AutoExtractMindMapNodes;
            m.AutoExtractActionItems = part.AutoExtractActionItems;
            m.Notes = part.Notes;
            m.Summary = part.Summary;
        });
    }

    public override async Task<IDisplayResult> UpdateAsync(MeetingPart part, IUpdateModel updater, UpdatePartEditorContext context)
    {
        await updater.TryUpdateModelAsync(part, Prefix);
        return Edit(part, context);
    }
}

public class MeetingPartViewModel : ContentPartViewModel
{
    public string MeetingId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string WorkContextId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime? TranscriptionCompletedAt { get; set; }
    public bool IsProcessed { get; set; }
    public int TranscriptCount { get; set; }
    public int ActionItemCount { get; set; }
    public int ExtractedNodesCount { get; set; }
    
    // Edit properties
    public bool AutoExtractMindMapNodes { get; set; } = true;
    public bool AutoExtractActionItems { get; set; } = true;
    public string Notes { get; set; }
    public string Summary { get; set; }
}
