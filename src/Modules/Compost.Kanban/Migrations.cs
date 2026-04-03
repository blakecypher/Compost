using Compost.Kanban.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;

namespace Compost.Kanban;

public class Migrations(IContentDefinitionManager contentDefinitionManager) : DataMigration
{
    public int Create()
    {
        // Define TreeNodePart
        contentDefinitionManager.AlterPartDefinitionAsync(nameof(TreeNodePart), part => part
            .WithDescription("Provides fields for structured requirement tree nodes.")
            .WithField("SourceMeetingId", field => field.OfType("TextField").WithDisplayName("Source Meeting ID"))
        );

        // Define TreeNode Content Type
        contentDefinitionManager.AlterTypeDefinitionAsync("TreeNode", type => type
            .WithPart("TitlePart", part => part.WithPosition("1"))
            .WithPart("MarkdownBodyPart", part => part.WithPosition("2"))
            .WithPart(nameof(TreeNodePart), part => part.WithPosition("3"))
            .Creatable()
            .Listable()
            .Versionable()
        );

        // Define KanbanCardPart
        contentDefinitionManager.AlterPartDefinitionAsync(nameof(KanbanCardPart), part => part
            .WithDescription("Provides fields for kanban board cards.")
            .WithField("SourceMeetingId", field => field.OfType("TextField").WithDisplayName("Source Meeting ID"))
        );

        // Define KanbanCard Content Type
        contentDefinitionManager.AlterTypeDefinitionAsync("KanbanCard", type => type
            .WithPart("TitlePart", part => part.WithPosition("1"))
            .WithPart("MarkdownBodyPart", part => part.WithPosition("2"))
            .WithPart(nameof(KanbanCardPart), part => part.WithPosition("3"))
            .Creatable()
            .Listable()
            .Versionable()
        );

        return 1;
    }
}
