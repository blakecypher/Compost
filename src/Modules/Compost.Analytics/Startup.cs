using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrchardCore.Modules;
using OrchardCore.Navigation;

namespace Compost.Analytics;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register Migrations
        services.AddScoped<OrchardCore.Data.Migration.IDataMigration, Migrations>();
        
        // Register Navigation
        services.AddScoped<INavigationProvider, AnalyticsMenu>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        // Main Analytics Dashboard route (accessible from main site)
        routes.MapAreaControllerRoute(
            name: "AnalyticsDashboard",
            areaName: "Compost.Analytics",
            pattern: "Analytics",
            defaults: new { controller = "Analytics", action = "Index" }
        );

        // Analytics sub-pages
        routes.MapAreaControllerRoute(
            name: "AnalyticsPages",
            areaName: "Compost.Analytics",
            pattern: "Analytics/{action}",
            defaults: new { controller = "Analytics" }
        );

        // API routes for JSON data
        routes.MapAreaControllerRoute(
            name: "AnalyticsApi",
            areaName: "Compost.Analytics",
            pattern: "api/analytics/{action}",
            defaults: new { controller = "Analytics" }
        );
    }
}

public class AnalyticsMenu(IStringLocalizer<AnalyticsMenu> localizer) : INavigationProvider
{
    private readonly IStringLocalizer _s = localizer;

    public Task BuildNavigationAsync(string name, NavigationBuilder builder)
    {
        // Support both admin and main site menus
        var isAdmin = string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase);
        var isMainMenu = string.Equals(name, "main-menu", StringComparison.OrdinalIgnoreCase);
        
        if (!isAdmin && !isMainMenu)
        {
            return Task.CompletedTask;
        }

        // For main menu, add simple link
        if (isMainMenu)
        {
            builder.Add(_s["Analytics"], "10", analytics => analytics
                .Action("Index", "Analytics", new { area = "Compost.Analytics" }));
        }
        else
        {
            // For admin, add submenu
            builder.Add(_s["Analytics"], "10", analytics => analytics
                .Add(_s["Dashboard"], "1", dashboard => dashboard
                    .Action("Index", "Analytics", new { area = "Compost.Analytics" })
                    .LocalNav()
                )
                .Add(_s["Velocity"], "2", velocity => velocity
                    .Action("Velocity", "Analytics", new { area = "Compost.Analytics" })
                    .LocalNav()
                )
                .Add(_s["Patterns"], "3", patterns => patterns
                    .Action("PatternUsage", "Analytics", new { area = "Compost.Analytics" })
                    .LocalNav()
                )
            );
        }

        return Task.CompletedTask;
    }
}

// Empty migrations class for now
public class Migrations : OrchardCore.Data.Migration.DataMigration
{
}
