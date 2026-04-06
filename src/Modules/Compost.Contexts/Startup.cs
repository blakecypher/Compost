using Compost.Contexts.Drivers;
using Compost.Contexts.Handlers;
using Compost.Contexts.Migrations;
using Compost.Contexts.Models;
using Compost.Contexts.Navigation;
using Compost.Contexts.Services;
using Compost.Core.Interfaces;
using Compost.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.BackgroundTasks;
using Compost.Contexts.Tasks;

namespace Compost.Contexts;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register content parts
        services.AddContentPart<ProjectPart>()
            .UseDisplayDriver<WorkContextPartDisplayDriver>()
            .AddHandler<ProjectPartHandler>();
            
        services.AddContentPart<ProjectTemplatePart>();
        
        services.AddContentPart<GitSettingsPart>();

        // Register services
        services.AddScoped<IProjectManager, ProjectManager>();
        services.AddScoped<ITimeTrackingService, TimeTrackingService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<IGitService, GitService>();
        services.AddScoped<IGitCredentialProvider, GitCredentialProvider>();

        // Register migrations
        services.AddScoped<IDataMigration, ContextMigrations>();
        
        // Register index provider
        services.AddIndexProvider<WorkProjectPartIndexProvider>();

        // Register navigation
        services.AddScoped<INavigationProvider, AdminMenu>();

        // Register background tasks
        services.AddSingleton<IBackgroundTask, GitSyncBackgroundTask>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        // Default route for root URL - redirect to Dashboard
        routes.MapAreaControllerRoute(
            name: "Default",
            areaName: "Compost.Contexts",
            pattern: "",
            defaults: new { controller = "Dashboard", action = "Index" }
        );

        routes.MapAreaControllerRoute(
            name: "Projects",
            areaName: "Compost.Contexts",
            pattern: "Projects/{action}/{id?}",
            defaults: new { controller = "Project", action = nameof(Index) }
        );

        routes.MapAreaControllerRoute(
            name: "GitSettings",
            areaName: "Compost.Contexts",
            pattern: "Projects/GitSettings/{action}",
            defaults: new { controller = "GitSettings", action = "Edit" }
        );

        // Legacy route to redirect old Context URLs to Project URLs
        routes.MapAreaControllerRoute(
            name: "LegacyContexts",
            areaName: "Compost.Contexts", 
            pattern: "Contexts/{action}/{id?}",
            defaults: new { controller = "Project", action = nameof(Index) }
        );
    }
}
