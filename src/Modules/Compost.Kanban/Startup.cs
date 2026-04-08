using System;
using System.Threading.Tasks;
using Compost.Core.Interfaces;
using Compost.Core.Services;
using Compost.Kanban.Drivers;
using Compost.Kanban.Models;
using Compost.Kanban.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Navigation;

namespace Compost.Kanban;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register Content Parts
        services.AddContentPart<TreeNodePart>()
            .UseDisplayDriver<TreeNodePartDisplayDriver>();
        services.AddContentPart<KanbanCardPart>()
            .UseDisplayDriver<KanbanCardPartDisplayDriver>();

        // Register services
        services.AddScoped<IDecompositionEngine, DecompositionEngine>();
        services.AddHttpClient<IAiIntegrationService, AiIntegrationService>();

        // Register Migrations
        services.AddScoped<IDataMigration, Migrations>();

        // Register navigation
        services.AddScoped<INavigationProvider, KanbanMenu>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        // Add specific routes for Refinement POST actions first (more specific routes should come first)
        routes.MapAreaControllerRoute(
            name: "RefinementAddMessage",
            areaName: $"Compost.{nameof(Kanban)}",
            pattern: "Kanban/Refinement/AddMessage",
            defaults: new { controller = "Refinement", action = "AddMessage" }
        );

        routes.MapAreaControllerRoute(
            name: "RefinementPromote",
            areaName: $"Compost.{nameof(Kanban)}",
            pattern: "Kanban/Refinement/Promote",
            defaults: new { controller = "Refinement", action = "Promote" }
        );

        // Add Kanban specific routes (must come before general Kanban route)
        routes.MapAreaControllerRoute(
            name: "KanbanUpdateStatus",
            areaName: "Compost.Kanban",
            pattern: "Kanban/UpdateStatus",
            defaults: new { controller = nameof(Kanban), action = "UpdateStatus" }
        );

        routes.MapAreaControllerRoute(
            name: "KanbanUpdateCardField",
            areaName: "Compost.Kanban",
            pattern: "Kanban/UpdateCardField",
            defaults: new { controller = nameof(Kanban), action = "UpdateCardField" }
        );

        routes.MapAreaControllerRoute(
            name: "KanbanGetCard",
            areaName: "Compost.Kanban",
            pattern: "Kanban/GetCard/{id}",
            defaults: new { controller = nameof(Kanban), action = "GetCard" }
        );

        routes.MapAreaControllerRoute(
            name: "KanbanUpdateCard",
            areaName: "Compost.Kanban",
            pattern: "Kanban/UpdateCard",
            defaults: new { controller = nameof(Kanban), action = "UpdateCard" }
        );

        routes.MapAreaControllerRoute(
            name: "KanbanCreateTestCard",
            areaName: "Compost.Kanban",
            pattern: "Kanban/CreateTestCard",
            defaults: new { controller = nameof(Kanban), action = "CreateTestCard" }
        );

        routes.MapAreaControllerRoute(
            name: "KanbanDeleteCard",
            areaName: "Compost.Kanban",
            pattern: "Kanban/DeleteCard",
            defaults: new { controller = nameof(Kanban), action = "DeleteCard" }
        );

        routes.MapAreaControllerRoute(
            name: "KanbanPublishAllCards",
            areaName: "Compost.Kanban",
            pattern: "Kanban/PublishAllCards",
            defaults: new { controller = nameof(Kanban), action = "PublishAllCards" }
        );

        routes.MapAreaControllerRoute(
            name: "KanbanListAllCards",
            areaName: "Compost.Kanban",
            pattern: "Kanban/ListAllCards",
            defaults: new { controller = nameof(Kanban), action = "ListAllCards" }
        );

        // Then add general routes (less specific - must come last)
        routes.MapAreaControllerRoute(
            name: "Refinement",
            areaName: "Compost.Kanban",
            pattern: "Kanban/Refinement/{id}",
            defaults: new { controller = "Refinement", action = nameof(Index) }
        );

        routes.MapAreaControllerRoute(
            name: nameof(Kanban),
            areaName: "Compost.Kanban",
            pattern: "Kanban/{action}/{id?}",
            defaults: new { controller = nameof(Kanban), action = nameof(Index) }
        );
    }
}

public class KanbanMenu(IStringLocalizer<KanbanMenu> localizer) : INavigationProvider
{
    private readonly IStringLocalizer _s = localizer;

    public Task BuildNavigationAsync(string name, NavigationBuilder builder)
    {
        if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        builder
            .Add(_s[nameof(Kanban)], "after:Mind Maps", kanban => kanban
                .Add(_s["Board"], "1", board => board
                    .Action(nameof(Index), nameof(Kanban), new { area = "Compost.Kanban" })
                    .LocalNav()
                )
                .Add(_s["Refinement"], "2", refinement => refinement
                    .Action(nameof(Index), "Refinement", new { area = "Compost.Kanban" })
                    .LocalNav()
                )
            );

        return Task.CompletedTask;
    }
}
