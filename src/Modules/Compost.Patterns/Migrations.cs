using Compost.Core.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;

namespace Compost.Patterns;

public class Migrations(IContentDefinitionManager contentDefinitionManager) : DataMigration
{
    public int Create()
    {
        // Define ArchitecturalPatternPart
        contentDefinitionManager.AlterPartDefinitionAsync(nameof(ArchitecturalPatternPart), part => part
            .WithDescription("Provides fields for architectural pattern templates.")
        );

        // Define ArchitecturalPattern Content Type
        contentDefinitionManager.AlterTypeDefinitionAsync("ArchitecturalPattern", type => type
            .WithPart("TitlePart", part => part.WithPosition("1"))
            .WithPart("MarkdownBodyPart", part => part.WithPosition("2"))
            .WithPart(nameof(ArchitecturalPatternPart), part => part.WithPosition("3"))
            .Creatable()
            .Listable()
            .Versionable()
        );

        return 1;
    }
}
