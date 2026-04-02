using System;
using Compost.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;

namespace Compost.Patterns;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register Content Parts
        services.AddContentPart<ArchitecturalPatternPart>();

        // Register Migrations
        services.AddScoped<IDataMigration, Migrations>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: nameof(Patterns),
            areaName: "Compost.Patterns",
            pattern: "Patterns/{action}/{id?}",
            defaults: new { controller = nameof(Patterns), action = nameof(Index) }
        );
    }
}
