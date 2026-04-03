using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace Compost.Contexts.Navigation;

public class AdminMenu(IStringLocalizer<AdminMenu> localizer) : INavigationProvider
{
    private readonly IStringLocalizer _s = localizer;

    public Task BuildNavigationAsync(string name, NavigationBuilder builder)
    {
        if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        builder
            .Add(_s["Projects"], "after:Content", projects => projects
                .Add(_s["Projects"], "1", tree => tree
                    .Action("TreeView", "Project", new { area = "Compost.Contexts" })
                    .LocalNav()
                )
                .Add(_s["Create New"], "2", create => create
                    .Action("Create", "Project", new { area = "Compost.Contexts" })
                    .LocalNav()
                )
            );

        return Task.CompletedTask;
    }
}
