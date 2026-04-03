using Compost.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Compost.Contexts.ViewComponents;

public class ActiveProjectIndicatorViewComponent(IProjectManager projectManager) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var activeProject = await projectManager.GetActiveProjectAsync();
        return View(activeProject);
    }
}
