using Compost.Core.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;

namespace Compost.Snippets;

public class Migrations(IContentDefinitionManager contentDefinitionManager) : DataMigration
{
    public int Create()
    {
        // Define CodeSnippetPart
        contentDefinitionManager.AlterPartDefinition(nameof(CodeSnippetPart), part => part
            .WithDescription("Provides fields for code snippets collection.")
        );

        // Define CodeSnippet Content Type
        contentDefinitionManager.AlterTypeDefinition("CodeSnippet", type => type
            .WithPart("TitlePart", part => part.WithPosition("1"))
            .WithPart(nameof(CodeSnippetPart), part => part.WithPosition("2"))
            .Creatable()
            .Listable()
            .Versionable()
            .Securable()
        );

        return 1;
    }
}
