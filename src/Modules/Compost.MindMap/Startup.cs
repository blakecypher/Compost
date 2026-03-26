using Compost.MindMap.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrchardCore.Modules;
using OrchardCore.Navigation;

namespace Compost.MindMap;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register mind map services - both Core interface and MindMap-specific interface
        services.AddScoped<Compost.Core.Interfaces.IMindMapService, MindMapService>();
        services.AddScoped<Compost.MindMap.Services.IMindMapService, MindMapService>();
        
        // Register navigation
        services.AddScoped<INavigationProvider, MindMapMenu>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        // Add specific API routes first (more specific routes should come first)
        routes.MapAreaControllerRoute(
            name: "MindMapApiPromoteToKanban",
            areaName: "Compost.MindMap",
            pattern: "MindMap/ApiPromoteToKanban",
            defaults: new { controller = nameof(MindMap), action = "ApiPromoteToKanban" }
        );

        routes.MapAreaControllerRoute(
            name: "MindMapApiPromoteNode",
            areaName: "Compost.MindMap",
            pattern: "MindMap/ApiPromoteNode",
            defaults: new { controller = nameof(MindMap), action = "ApiPromoteNode" }
        );

        routes.MapAreaControllerRoute(
            name: "MindMapApiPromoteToStructure",
            areaName: "Compost.MindMap",
            pattern: "MindMap/ApiPromoteToStructure",
            defaults: new { controller = nameof(MindMap), action = "ApiPromoteToStructure" }
        );

        routes.MapAreaControllerRoute(
            name: "MindMapApiUpdateNode",
            areaName: "Compost.MindMap",
            pattern: "MindMap/ApiUpdateNode",
            defaults: new { controller = nameof(MindMap), action = "ApiUpdateNode" }
        );

        routes.MapAreaControllerRoute(
            name: "MindMapApiMap",
            areaName: "Compost.MindMap",
            pattern: "MindMap/ApiMap/{id}",
            defaults: new { controller = nameof(MindMap), action = "ApiMap" }
        );

        routes.MapAreaControllerRoute(
            name: "MindMapApiDeleteNode",
            areaName: "Compost.MindMap",
            pattern: "MindMap/ApiDeleteNode",
            defaults: new { controller = nameof(MindMap), action = "ApiDeleteNode" }
        );

        // Then add general routes (less specific - must come last)
        routes.MapAreaControllerRoute(
            name: nameof(MindMap),
            areaName: "Compost.MindMap",
            pattern: "MindMap/{action}/{id?}",
            defaults: new { controller = nameof(MindMap), action = nameof(Index) }
        );
    }
}

public class MindMapMenu(IStringLocalizer<MindMapMenu> localizer) : INavigationProvider
{
    private readonly IStringLocalizer S = localizer;

    public Task BuildNavigationAsync(string name, NavigationBuilder builder)
    {
        if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        builder
            .Add(S["Mind Maps"], "after:Content", mindmap => mindmap
                .Add(S["All Mind Maps"], "1", list => list
                    .Action(nameof(Index), nameof(MindMap), new { area = "Compost.MindMap" })
                    .LocalNav()
                )
                .Add(S["Create New"], "2", create => create
                    .Action("Create", nameof(MindMap), new { area = "Compost.MindMap" })
                    .LocalNav()
                )
            );

        return Task.CompletedTask;
    }
}
