using Compost.Core.Models;
using Compost.MindMap.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;

namespace Compost.MindMap;

public class Migrations(IContentDefinitionManager contentDefinitionManager) : DataMigration
{
    public int Create()
    {
        // Define MindMapNodePart
        contentDefinitionManager.AlterPartDefinitionAsync(nameof(MindMapNodePart), part => part
            .WithDescription("Provides fields for mind map nodes including position, color, and node type.")
            .WithField("WorkContextId", field => field.OfType("TextField").WithDisplayName("Work Context ID"))
            .WithField("ParentNodeId", field => field.OfType("TextField").WithDisplayName("Parent Node ID"))
        );

        // Define MindMapNode Content Type
        contentDefinitionManager.AlterTypeDefinitionAsync("MindMapNode", type => type
            .WithPart("TitlePart", part => part.WithPosition("1"))
            .WithPart("MarkdownBodyPart", part => part.WithPosition("2"))
            .WithPart(nameof(MindMapNodePart), part => part.WithPosition("3"))
            .Creatable()
            .Listable()
            .Versionable()
        );

        return 1;
    }
}
