using Compost.Structure.Drivers;
using Compost.Structure.Models;
using Compost.Structure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Navigation;

namespace Compost.Structure;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register Content Part for StructureNode
        services.AddContentPart<StructureNodePart>()
            .UseDisplayDriver<StructureNodePartDisplayDriver>();

        // Register Structure Service
        services.AddScoped<IStructureService, StructureService>();

        // Register Migrations
        services.AddScoped<IDataMigration, Migrations>();

        // Register Navigation
        services.AddScoped<INavigationProvider, StructureMenu>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        // Structure API routes
        routes.MapAreaControllerRoute(
            name: "StructureAPI",
            areaName: "Compost.Structure",
            pattern: "Structure/Api/{action}",
            defaults: new { controller = "Structure" }
        );

        // Structure hierarchy routes
        routes.MapAreaControllerRoute(
            name: "StructureHierarchy",
            areaName: "Compost.Structure",
            pattern: "Structure/Hierarchy/{action}/{id?}",
            defaults: new { controller = "Hierarchy", action = "Index" }
        );

        // Structure kanban association routes
        routes.MapAreaControllerRoute(
            name: "StructureBoard",
            areaName: "Compost.Structure",
            pattern: "Structure/Board/{action}/{id?}",
            defaults: new { controller = "Board", action = "Index" }
        );

        // Main Structure routes
        routes.MapAreaControllerRoute(
            name: "Structure",
            areaName: "Compost.Structure",
            pattern: "Structure/{action}/{id?}",
            defaults: new { controller = "Structure", action = "Index" }
        );
    }
}

public class StructureMenu(IStringLocalizer<StructureMenu> localizer) : INavigationProvider
{
    private readonly IStringLocalizer _s = localizer;

    public Task BuildNavigationAsync(string name, NavigationBuilder builder)
    {
        if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        builder
            .Add(_s["Structure"], "after:Kanban", structure => structure
                .Add(_s["Hierarchy"], "1", hierarchy => hierarchy
                    .Action("Index", "Hierarchy", new { area = "Compost.Structure" })
                    .LocalNav()
                )
                .Add(_s["Teams"], "2", teams => teams
                    .Action("Teams", "Structure", new { area = "Compost.Structure" })
                    .LocalNav()
                )
                .Add(_s["Boards"], "3", boards => boards
                    .Action("Boards", "Board", new { area = "Compost.Structure" })
                    .LocalNav()
                )
            );

        return Task.CompletedTask;
    }
}
