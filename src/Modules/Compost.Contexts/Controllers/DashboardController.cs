using Compost.Core.Interfaces;
using Compost.Core.Models;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using YesSql;
using ISession = YesSql.ISession;

namespace Compost.Contexts.Controllers;

public class DashboardController(
    IProjectManager projectManager,
    IMindMapService mindMapService,
    ITranscriptionService transcriptionService,
    ISession session) : Controller
{
    public async Task<IActionResult> Index()
    {
        // Get latest projects
        var projects = await projectManager.GetAllProjectsAsync();
        var latestProjects = projects.OrderByDescending(p => p.LastAccessedAt).Take(5).ToList();

        // Get latest meetings
        var meetings = await transcriptionService.GetAllMeetingsAsync();
        var latestMeetings = meetings.OrderByDescending(m => m.StartedAt).Take(5).ToList();

        // Get latest mindmaps
        var mindMaps = await mindMapService.GetAllMindMapsAsync();
        var latestMindMaps = mindMaps.OrderByDescending(m => m.UpdatedAt).Take(5).ToList();

        // Get latest Kanban cards
        var cards = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.ContentType == "KanbanCard" && x.Latest && x.Published)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(5)
            .ListAsync();

        // Get latest snippets
        var snippets = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.ContentType == "CodeSnippet" && x.Latest && x.Published)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(5)
            .ListAsync();

        // Get latest patterns
        var patterns = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.ContentType == "ArchitecturalPattern" && x.Latest && x.Published)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(5)
            .ListAsync();

        var viewModel = new DashboardViewModel
        {
            TotalProjects = projects.Count,
            ActiveProjects = projects.Count(p => p.IsActive),
            TrackingProjects = projects.Count(p => p.CurrentSessionStartedAt.HasValue),
            LatestProjects = latestProjects,
            TotalTimeSpent = TimeSpan.FromSeconds(projects.Sum(p => p.TotalTimeSpentSeconds)),
            LatestMindMaps = latestMindMaps,
            LatestKanbanCards = cards.Cast<object>().ToList(),
            LatestSnippets = snippets.Cast<object>().ToList(),
            LatestPatterns = patterns.Cast<object>().ToList(),
            LatestTranscriptions = latestMeetings.Cast<object>().ToList()
        };

        return View(viewModel);
    }
}

public class DashboardViewModel
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int TrackingProjects { get; set; }
    public TimeSpan TotalTimeSpent { get; set; }
    public List<Project> LatestProjects { get; set; } = new();
    
    // Placeholders for other modules
    public List<MindMapSummary> LatestMindMaps { get; set; } = new();
    public List<object> LatestKanbanCards { get; set; } = new();
    public List<object> LatestSnippets { get; set; } = new();
    public List<object> LatestPatterns { get; set; } = new();
    public List<object> LatestTranscriptions { get; set; } = new();
}
