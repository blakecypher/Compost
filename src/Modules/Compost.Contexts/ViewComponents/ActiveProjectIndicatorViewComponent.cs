using Compost.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Compost.Contexts.ViewComponents;

public class ActiveProjectIndicatorViewComponent : ViewComponent
{
    private readonly IProjectManager _projectManager;

    public ActiveProjectIndicatorViewComponent(IProjectManager projectManager)
    {
        _projectManager = projectManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var activeProject = await _projectManager.GetActiveProjectAsync();
        return View(activeProject);
    }
}
