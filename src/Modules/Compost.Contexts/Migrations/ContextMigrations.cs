using Compost.Contexts.Models;
using Compost.Contexts.Services;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using YesSql.Sql;

namespace Compost.Contexts.Migrations;

public class ContextMigrations(IContentDefinitionManager contentDefinitionManager) : DataMigration
{
    public int Create()
    {
        // Create the index table for ProjectPart
        SchemaBuilder.CreateMapIndexTableAsync<WorkProjectPartIndex>(table => table
            .Column<bool>(nameof(WorkProjectPartIndex.IsActive))
            .Column<long>(nameof(WorkProjectPartIndex.TotalTimeSpentSeconds))
        );

        // Create Project content type and attach ProjectPart
        contentDefinitionManager.AlterTypeDefinitionAsync("Project", type => type
            .Creatable()
            .Listable()
            .Draftable()
            .Versionable()
            .Securable()
            .WithPart(nameof(ProjectPart))
        );

        return 1;
    }
    
    public int UpdateFrom1()
    {
        // Ensure ProjectPart is properly attached with settings that enable persistence
        contentDefinitionManager.AlterTypeDefinitionAsync("Project", type => type
            .WithPart(nameof(ProjectPart), part =>
            {
                var settings = new ContentPartSettings
                {
                    Attachable = true,
                    Reusable = true
                };
                part
                    .WithSettings(settings);
            })
        );
        
        return 2;
    }
    
    public int UpdateFrom2()
    {
        // Create ContextTemplate content type
        contentDefinitionManager.AlterTypeDefinitionAsync(nameof(ContextTemplate), type => type
            .Creatable()
            .Listable()
            .Draftable()
            .Versionable()
            .Securable()
            .WithPart(nameof(ProjectTemplatePart))
        );
        
        return 3;
    }
}
