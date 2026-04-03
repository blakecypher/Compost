using Compost.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Compost.Contexts.ViewComponents;

public class ContextStatsViewComponent(IProjectManager projectManager) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var projects = await projectManager.GetAllProjectsAsync();

        var stats = new ContextStatsViewModel
        {
            TotalContexts = projects.Count,
            ActiveContexts = projects.Count(p => p.IsActive),
            TrackingContexts = projects.Count(p => p.CurrentSessionStartedAt.HasValue),
            TotalTimeSpent = TimeSpan.FromSeconds(projects.Sum(p => p.TotalTimeSpentSeconds)),
            StatusCounts = projects
                .GroupBy(p => p.Status)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return View(stats);
    }
}

public class ContextStatsViewModel
{
    public int TotalContexts { get; set; }
    public int ActiveContexts { get; set; }
    public int TrackingContexts { get; set; }
    public TimeSpan TotalTimeSpent { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = new();
}
