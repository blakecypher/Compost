using Compost.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Compost.Contexts.ViewComponents;

public class ActiveProjectIndicatorViewComponent(IProjectManager projectManager, IGitService gitService, IMemoryCache cache) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var activeProject = await projectManager.GetActiveProjectAsync();
        
        if (activeProject != null && activeProject.IsGitActive && !string.IsNullOrEmpty(activeProject.GitLocalPath))
        {
            var cacheKey = $"git_status_active_{activeProject.Id}";
            if (!cache.TryGetValue<GitStatus>(cacheKey, out var status))
            {
                status = gitService.GetRepositoryStatus(activeProject.GitLocalPath);
                cache.Set(cacheKey, status, TimeSpan.FromMinutes(1));
            }
            ViewData["GitStatus"] = status;
        }

        return View(activeProject);
    }
}
