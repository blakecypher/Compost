using System;
using System.Threading.Tasks;
using Compost.Transcription.Models;
using OrchardCore.ContentManagement.Handlers;

namespace Compost.Transcription.Handlers;

public class MeetingPartHandler : ContentPartHandler<MeetingPart>
{
    public override Task UpdatedAsync(UpdateContentContext context, MeetingPart part)
    {
        // The ID and Status are now managed by TranscriptionService to ensure reliability
        return Task.CompletedTask;
    }

    public override Task PublishedAsync(PublishContentContext context, MeetingPart part)
    {
        return Task.CompletedTask;
    }
}
