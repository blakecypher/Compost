using Compost.Core.Interfaces;
using Compost.Core.Models;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using YesSql;
using ISession = YesSql.ISession;

namespace Compost.Web.Controllers;

public class DashboardController(
    IProjectManager projectManager,
    IMindMapService mindMapService,
    ITranscriptionService transcriptionService,
    ISession session) : Controller
{
    public async Task<IActionResult> Index()
    {
        // Get latest projects/contexts
        var projects = await projectManager.GetAllProjectsAsync();
        var latestProjects = projects.OrderByDescending(p => p.LastAccessedAt).Take(5).ToList();

        // Get latest meetings
        var meetings = await transcriptionService.GetAllMeetingsAsync();
        var latestMeetings = meetings.OrderByDescending(m => m.StartedAt).Take(5).ToList();

        // Get latest mindmaps
        var mindMaps = await mindMapService.GetAllMindMapsAsync();
        var latestMindMaps = mindMaps.OrderByDescending(m => m.UpdatedAt).Take(5).ToList();

        // Get latest Kanban cards
        // Similar to KanbanController, fetch from session
        var cards = await session.Query<ContentItem, OrchardCore.ContentManagement.Records.ContentItemIndex>()
            .Where(x => x.ContentType == "KanbanCard" && x.Latest && x.Published)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(5)
            .ListAsync();

        // Get latest snippets
        var snippets = await session.Query<ContentItem, OrchardCore.ContentManagement.Records.ContentItemIndex>()
            .Where(x => x.ContentType == "CodeSnippet" && x.Latest && x.Published)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(5)
            .ListAsync();

        // Get latest patterns
        var patterns = await session.Query<ContentItem, OrchardCore.ContentManagement.Records.ContentItemIndex>()
            .Where(x => x.ContentType == "ArchitecturalPattern" && x.Latest && x.Published)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(5)
            .ListAsync();

        var viewModel = new DashboardViewModel
        {
            TotalContexts = projects.Count,
            ActiveContexts = projects.Count(p => p.IsActive),
            TrackingContexts = projects.Count(p => p.CurrentSessionStartedAt.HasValue),
            LatestContexts = latestProjects,
            LatestMeetings = latestMeetings,
            LatestMindMaps = latestMindMaps,
            LatestKanbanCards = cards.ToList(),
            LatestSnippets = snippets.ToList(),
            LatestPatterns = patterns.ToList(),
            TotalTimeSpent = TimeSpan.FromSeconds(projects.Sum(p => p.TotalTimeSpentSeconds))
        };

        return View(viewModel);
    }
}

public class DashboardViewModel
{
    public int TotalContexts { get; set; }
    public int ActiveContexts { get; set; }
    public int TrackingContexts { get; set; }
    public TimeSpan TotalTimeSpent { get; set; }
    public List<Project> LatestContexts { get; set; } = new();
    public List<Meeting> LatestMeetings { get; set; } = new();
    public List<MindMapSummary> LatestMindMaps { get; set; } = new();
    public List<ContentItem> LatestKanbanCards { get; set; } = new();
    public List<ContentItem> LatestSnippets { get; set; } = new();
    public List<ContentItem> LatestPatterns { get; set; } = new();
}
