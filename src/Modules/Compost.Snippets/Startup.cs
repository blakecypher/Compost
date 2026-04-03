using System;
using Compost.Core.Models;
using Compost.Core.Services;
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
        services.AddHttpClient<AiIntegrationService>();
        services.AddScoped<AiIntegrationService>();

        // Register Migrations
        services.AddScoped<IDataMigration, Migrations>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        // Add specific routes for API endpoints first (more specific routes should come first)
        routes.MapAreaControllerRoute(
            name: "SnippetsRecognizePatterns",
            areaName: "Compost.Snippets",
            pattern: "Snippets/RecognizePatterns",
            defaults: new { controller = nameof(Snippets), action = "RecognizePatterns" }
        );

        routes.MapAreaControllerRoute(
            name: "SnippetsAnalyzeCode",
            areaName: "Compost.Snippets",
            pattern: "Snippets/AnalyzeCode",
            defaults: new { controller = nameof(Snippets), action = "AnalyzeCode" }
        );

        routes.MapAreaControllerRoute(
            name: "SnippetsGenerateSuggestion",
            areaName: "Compost.Snippets",
            pattern: "Snippets/GenerateSuggestion",
            defaults: new { controller = nameof(Snippets), action = "GenerateSuggestion" }
        );

        routes.MapAreaControllerRoute(
            name: nameof(Snippets),
            areaName: "Compost.Snippets",
            pattern: "Snippets/{action}/{id?}",
            defaults: new { controller = nameof(Snippets), action = nameof(Index) }
        );
    }
}
