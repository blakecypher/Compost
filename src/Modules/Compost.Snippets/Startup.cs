using System;
using Compost.Snippets.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;

namespace Compost.Snippets;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register Content Parts
        services.AddContentPart<CodeSnippetPart>();

        // Register AI service
        services.AddHttpClient<Core.Services.AIIntegrationService>();
        services.AddScoped<Core.Services.AIIntegrationService>();

        // Register Migrations
        services.AddScoped<IDataMigration, Migrations>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: nameof(Snippets),
            areaName: "Compost.Snippets",
            pattern: "Snippets/{action}/{id?}",
            defaults: new { controller = nameof(Snippets), action = nameof(Index) }
        );
    }
}
