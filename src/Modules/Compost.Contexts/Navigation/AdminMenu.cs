using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace Compost.Contexts.Navigation;

public class AdminMenu(IStringLocalizer<AdminMenu> localizer) : INavigationProvider
{
    private readonly IStringLocalizer S = localizer;

    public Task BuildNavigationAsync(string name, NavigationBuilder builder)
    {
        if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        builder
            .Add(S["Projects"], "after:Content", projects => projects
                .Add(S["Projects"], "1", tree => tree
                    .Action("TreeView", "Project", new { area = "Compost.Contexts" })
                    .LocalNav()
                )
                .Add(S["Create New"], "2", create => create
                    .Action("Create", "Project", new { area = "Compost.Contexts" })
                    .LocalNav()
                )
            );

        return Task.CompletedTask;
    }
}
