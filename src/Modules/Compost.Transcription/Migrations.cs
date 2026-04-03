using System.Threading.Tasks;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;

namespace Compost.Transcription;

public class Migrations(IContentDefinitionManager contentDefinitionManager) : DataMigration
{
    public async Task<int> CreateAsync()
    {
        // Create Meeting content type with MeetingPart - properties will be serialized as JSON
        await contentDefinitionManager.AlterTypeDefinitionAsync("Meeting", type => type
            .Creatable()
            .Listable()
            .Securable()
            .WithPart("MeetingPart", part => part
                .WithPosition("1")
                .WithDisplayName("Meeting Details"))
            .WithPart("TitlePart", part => part
                .WithPosition("0")
                .WithDisplayName("Title"))
            .WithPart("AutoroutePart", part => part
                .WithPosition("2")
                .WithDisplayName("URL"))
            .WithPart("CommonPart", part => part
                .WithPosition("3")
                .WithDisplayName("Common")));

        return 1;
    }

    public async Task<int> UpdateFrom1Async()
    {
        // Ensure the Meeting content type exists and has all the necessary parts if it was already created
        await contentDefinitionManager.AlterTypeDefinitionAsync("Meeting", type => type
            .Creatable()
            .Listable()
            .Securable()
            .WithPart("MeetingPart", part => part
                .WithPosition("1")
                .WithDisplayName("Meeting Details"))
            .WithPart("TitlePart", part => part
                .WithPosition("0")
                .WithDisplayName("Title"))
            .WithPart("AutoroutePart", part => part
                .WithPosition("2")
                .WithDisplayName("URL"))
            .WithPart("CommonPart", part => part
                .WithPosition("3")
                .WithDisplayName("Common")));

        return 2;
    }
}
