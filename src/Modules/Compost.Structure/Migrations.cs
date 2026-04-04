using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.ContentFields.Settings;
using OrchardCore.Markdown.Settings;

namespace Compost.Structure;

public class Migrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionManager;

    public Migrations(IContentDefinitionManager contentDefinitionManager)
    {
        _contentDefinitionManager = contentDefinitionManager;
    }

    public async Task<int> CreateAsync()
    {
        // Create StructureNode content type
        await _contentDefinitionManager.AlterTypeDefinitionAsync("StructureNode", type => type
            .DisplayedAs("Structure Node")
            .WithPart("StructureNodePart")
            .WithPart("TitlePart")
            .WithPart("MarkdownBodyPart", part => part
                .WithSettings(new MarkdownBodyPartSettings
                {
                    SanitizeHtml = true
                })
            )
            .Versionable()
            .Securable()
        );

        // Configure StructureNodePart
        await _contentDefinitionManager.AlterPartDefinitionAsync("StructureNodePart", part => part
            .WithField("StructureType", field => field
                .OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Type of structure (Team, Department, Project, etc.)"
                })
            )
            .WithField("ParentStructureId", field => field
                .OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "ID of parent structure node"
                })
            )
            .WithField("ChildStructureIds", field => field
                .OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Comma-separated list of child structure IDs"
                })
            )
            .WithField("KanbanBoardId", field => field
                .OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Associated kanban board ID"
                })
            )
            .WithField("WorkContextId", field => field
                .OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Required = true,
                    Hint = "Project/Context this structure belongs to"
                })
            )
            .WithField("MemberIds", field => field
                .OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Comma-separated list of member IDs"
                })
            )
            .WithField("LeadId", field => field
                .OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Team lead or manager ID"
                })
            )
            .WithField("Color", field => field
                .OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Color coding for visual organization"
                })
            )
        );

        return 1;
    }
}
