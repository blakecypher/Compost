using System.Threading.Tasks;
using Compost.Transcription.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;

namespace Compost.Transcription.Migrations;

public class MeetingMigrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionManager;

    public MeetingMigrations(IContentDefinitionManager contentDefinitionManager)
    {
        _contentDefinitionManager = contentDefinitionManager;
    }

    public async Task<int> CreateAsync()
    {
        // Create the Meeting content type with MeetingPart attached
        // The MeetingPart properties (TranscriptJson, ActionItems, etc.) are
        // automatically serialized by Orchard Core - no field definitions needed
        await _contentDefinitionManager.AlterTypeDefinitionAsync("Meeting", builder => builder
            .DisplayedAs("Meeting")
            .WithDescription("A recorded meeting with transcription data.")
            .WithPart(nameof(MeetingPart))
            .Creatable(false)
            .Listable(false)
            .Draftable(false)
            .Versionable(false)
            .Securable(false)
        );

        return 1;
    }

    // Retry creating Meeting content type after fixing the migration
    public async Task<int> UpdateFrom1Async()
    {
        await _contentDefinitionManager.AlterTypeDefinitionAsync("Meeting", builder => builder
            .DisplayedAs("Meeting")
            .WithDescription("A recorded meeting with transcription data.")
            .WithPart(nameof(MeetingPart))
            .Creatable(false)
            .Listable(false)
            .Draftable(false)
            .Versionable(false)
            .Securable(false)
        );

        return 2;
    }
}
