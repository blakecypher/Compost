using System;
using System.Threading.Tasks;
using Compost.Transcription.Models;
using OrchardCore.ContentManagement.Handlers;

namespace Compost.Transcription.Handlers;

public class MeetingPartHandler : ContentPartHandler<MeetingPart>
{
    public override Task UpdatedAsync(UpdateContentContext context, MeetingPart part)
    {
        // Auto-generate meeting ID if not set
        if (string.IsNullOrEmpty(part.MeetingId))
        {
            part.MeetingId = $"meeting_{context.ContentItem.Id}_{Guid.NewGuid():N}";
        }

        // Set default status if not set
        if (string.IsNullOrEmpty(part.Status))
        {
            part.Status = "Draft";
        }

        return base.UpdatedAsync(context, part);
    }

    public override Task PublishedAsync(PublishContentContext context, MeetingPart part)
    {
        // When published, ensure the meeting has proper status
        if (string.IsNullOrEmpty(part.Status) || part.Status == "Draft")
        {
            part.Status = "Ready";
        }

        return base.PublishedAsync(context, part);
    }
}
