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
        // Add Analytics routes
        routes.MapAreaControllerRoute(
            name: "AnalyticsAPI",
            areaName: "Compost.Analytics",
            pattern: "Analytics/Api/{action}",
            defaults: new { controller = "Analytics" }
        );

        routes.MapAreaControllerRoute(
            name: "Analytics",
            areaName: "Compost.Analytics",
            pattern: "Analytics/{action}/{id?}",
            defaults: new { controller = "Analytics", action = "Index" }
        );
    }
}

public class AnalyticsMenu : INavigationProvider
{
    private readonly IStringLocalizer S;

    public AnalyticsMenu(IStringLocalizer<AnalyticsMenu> localizer)
    {
        S = localizer;
    }

    public Task BuildNavigationAsync(string name, NavigationBuilder builder)
    {
        if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        builder
            .Add(S["Analytics"], "after:Kanban", analytics => analytics
                .Add(S["Dashboard"], "1", dashboard => dashboard
                    .Action("Index", "Analytics", new { area = "Compost.Analytics" })
                    .LocalNav()
                )
                .Add(S["Velocity"], "2", velocity => velocity
                    .Action("Velocity", "Analytics", new { area = "Compost.Analytics" })
                    .LocalNav()
                )
                .Add(S["Patterns"], "3", patterns => patterns
                    .Action("PatternUsage", "Analytics", new { area = "Compost.Analytics" })
                    .LocalNav()
                )
            );

        return Task.CompletedTask;
    }
}

// Empty migrations class for now
public class Migrations : OrchardCore.Data.Migration.DataMigration
{
    public async Task<int> CreateAsync()
    {
        // Analytics doesn't need its own tables - it queries existing content
        return 1;
    }
}
