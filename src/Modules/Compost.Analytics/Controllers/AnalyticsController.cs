using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compost.Analytics.ViewModels;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Navigation;
using YesSql;

namespace Compost.Analytics.Controllers;

public class AnalyticsController : Controller
{
    private readonly ISession _session;
    private readonly IContentManager _contentManager;

    public AnalyticsController(ISession session, IContentManager contentManager)
    {
        _session = session;
        _contentManager = contentManager;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new AnalyticsDashboardViewModel
        {
            VelocityData = await GetVelocityDataAsync(),
            PatternUsageData = await GetPatternUsageDataAsync(),
            ModuleUsageData = await GetModuleUsageDataAsync(),
            ActivityData = await GetActivityDataAsync(),
            SummaryStats = await GetSummaryStatsAsync()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Velocity()
    {
        var data = await GetVelocityDataAsync();
        return Json(data);
    }

    public async Task<IActionResult> PatternUsage()
    {
        var data = await GetPatternUsageDataAsync();
        return Json(data);
    }

    public async Task<IActionResult> ModuleUsage()
    {
        var data = await GetModuleUsageDataAsync();
        return Json(data);
    }

    public async Task<IActionResult> Activity()
    {
        var data = await GetActivityDataAsync();
        return Json(data);
    }

    private async Task<VelocityData> GetVelocityDataAsync()
    {
        // Get Kanban cards for velocity calculation
        var kanbanCards = await _session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.ContentType == "KanbanCard" && x.Published)
            .ListAsync();

        var completedCards = kanbanCards
            .Where(x => x.As<KanbanCardPart>()?.Status == "Done")
            .ToList();

        // Calculate velocity by week (last 12 weeks)
        var velocityByWeek = new List<WeeklyVelocity>();
        var today = DateTime.Today;
        
        for (int i = 11; i >= 0; i--)
        {
            var weekStart = today.AddDays(-((today.DayOfWeek - DayOfWeek.Monday + 7) % 7) - (i * 7));
            var weekEnd = weekStart.AddDays(6);
            
            var weekCards = completedCards.Where(x => 
            {
                var part = x.As<KanbanCardPart>();
                // This would need a completion date field in the KanbanCardPart
                // For now, we'll use a placeholder
                return true; // TODO: Add completion date tracking
            }).ToList();

            var totalStoryPoints = weekCards.Sum(x => x.As<KanbanCardPart>()?.StoryPoints ?? 0);
            
            velocityByWeek.Add(new WeeklyVelocity
            {
                Week = weekStart.ToString("MMM dd"),
                CompletedCards = weekCards.Count,
                StoryPoints = totalStoryPoints
            });
        }

        // Calculate average velocity
        var avgVelocity = velocityByWeek.Skip(4).Average(x => x.StoryPoints); // Last 8 weeks
        
        return new VelocityData
        {
            WeeklyVelocities = velocityByWeek,
            AverageVelocity = avgVelocity,
            TotalCompleted = completedCards.Count,
            TotalStoryPoints = completedCards.Sum(x => x.As<KanbanCardPart>()?.StoryPoints ?? 0)
        };
    }

    private async Task<PatternUsageData> GetPatternUsageDataAsync()
    {
        // Get patterns and their usage in snippets
        var patterns = await _session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.ContentType == "ArchitecturalPattern" && x.Published)
            .ListAsync();

        var snippets = await _session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.ContentType == "CodeSnippet" && x.Published)
            .ListAsync();

        var patternUsage = new List<PatternUsage>();
        
        foreach (var pattern in patterns)
        {
            var relatedSnippets = snippets.Count(x => 
                x.As<CodeSnippetPart>()?.RelatedPatternId == pattern.ContentItemId);

            patternUsage.Add(new PatternUsage
            {
                PatternName = pattern.DisplayText ?? "Unknown Pattern",
                UsageCount = relatedSnippets,
                Category = pattern.As<ArchitecturalPatternPart>()?.Category ?? "General"
            });
        }

        return new PatternUsageData
        {
            Patterns = patternUsage.OrderByDescending(x => x.UsageCount).ToList(),
            TotalPatterns = patterns.Count,
            TotalUsage = patternUsage.Sum(x => x.UsageCount)
        };
    }

    private async Task<ModuleUsageData> GetModuleUsageDataAsync()
    {
        // Get content items by type for module usage statistics
        var contentItems = await _session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.Published)
            .ListAsync();

        var moduleStats = new List<ModuleStat>
        {
            new ModuleStat { ModuleName = "Mind Maps", ContentCount = contentItems.Count(x => x.ContentType == "MindMapCollection"), Icon = "fas fa-project-diagram", Color = "#2563eb" },
            new ModuleStat { ModuleName = "Kanban", ContentCount = contentItems.Count(x => x.ContentType == "KanbanCard"), Icon = "fas fa-columns", Color = "#16a34a" },
            new ModuleStat { ModuleName = "Snippets", ContentCount = contentItems.Count(x => x.ContentType == "CodeSnippet"), Icon = "fas fa-code", Color = "#0891b2" },
            new ModuleStat { ModuleName = "Patterns", ContentCount = contentItems.Count(x => x.ContentType == "ArchitecturalPattern"), Icon = "fas fa-shapes", Color = "#ea580c" },
            new ModuleStat { ModuleName = "Meetings", ContentCount = contentItems.Count(x => x.ContentType == "Meeting"), Icon = "fas fa-microphone", Color = "#dc2626" },
            new ModuleStat { ModuleName = "Tree Nodes", ContentCount = contentItems.Count(x => x.ContentType == "TreeNode"), Icon = "fas fa-sitemap", Color = "#7c3aed" }
        };

        return new ModuleUsageData
        {
            ModuleStats = moduleStats.OrderByDescending(x => x.ContentCount).ToList(),
            TotalContent = contentItems.Count
        };
    }

    private async Task<ActivityData> GetActivityDataAsync()
    {
        // Get recent activity (last 30 days)
        var contentItems = await _session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.Published)
            .ListAsync();

        var today = DateTime.Today;
        var activityByDay = new List<DailyActivity>();
        
        for (int i = 29; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayActivity = contentItems.Count(x => 
            {
                // This would need created/modified date tracking
                // For now, we'll simulate some activity
                return true; // TODO: Add date tracking
            });

            activityByDay.Add(new DailyActivity
            {
                Date = date.ToString("MMM dd"),
                MindMaps = dayActivity / 6, // Simulated distribution
                KanbanCards = dayActivity / 4,
                Snippets = dayActivity / 3,
                Patterns = dayActivity / 8
            });
        }

        return new ActivityData
        {
            DailyActivities = activityByDay,
            TotalActivity = contentItems.Count
        };
    }

    private async Task<SummaryStats> GetSummaryStatsAsync()
    {
        var contentItems = await _session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.Published)
            .ListAsync();

        return new SummaryStats
        {
            TotalMindMaps = contentItems.Count(x => x.ContentType == "MindMapCollection"),
            TotalKanbanCards = contentItems.Count(x => x.ContentType == "KanbanCard"),
            TotalSnippets = contentItems.Count(x => x.ContentType == "CodeSnippet"),
            TotalPatterns = contentItems.Count(x => x.ContentType == "ArchitecturalPattern"),
            TotalMeetings = contentItems.Count(x => x.ContentType == "Meeting"),
            TotalTreeNodes = contentItems.Count(x => x.ContentType == "TreeNode"),
            TotalContent = contentItems.Count
        };
    }
}

// View Models
public class AnalyticsDashboardViewModel
{
    public VelocityData VelocityData { get; set; } = new();
    public PatternUsageData PatternUsageData { get; set; } = new();
    public ModuleUsageData ModuleUsageData { get; set; } = new();
    public ActivityData ActivityData { get; set; } = new();
    public SummaryStats SummaryStats { get; set; } = new();
}

public class VelocityData
{
    public List<WeeklyVelocity> WeeklyVelocities { get; set; } = new();
    public double AverageVelocity { get; set; }
    public int TotalCompleted { get; set; }
    public int TotalStoryPoints { get; set; }
}

public class WeeklyVelocity
{
    public string Week { get; set; } = string.Empty;
    public int CompletedCards { get; set; }
    public int StoryPoints { get; set; }
}

public class PatternUsageData
{
    public List<PatternUsage> Patterns { get; set; } = new();
    public int TotalPatterns { get; set; }
    public int TotalUsage { get; set; }
}

public class PatternUsage
{
    public string PatternName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class ModuleUsageData
{
    public List<ModuleStat> ModuleStats { get; set; } = new();
    public int TotalContent { get; set; }
}

public class ModuleStat
{
    public string ModuleName { get; set; } = string.Empty;
    public int ContentCount { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class ActivityData
{
    public List<DailyActivity> DailyActivities { get; set; } = new();
    public int TotalActivity { get; set; }
}

public class DailyActivity
{
    public string Date { get; set; } = string.Empty;
    public int MindMaps { get; set; }
    public int KanbanCards { get; set; }
    public int Snippets { get; set; }
    public int Patterns { get; set; }
}

public class SummaryStats
{
    public int TotalMindMaps { get; set; }
    public int TotalKanbanCards { get; set; }
    public int TotalSnippets { get; set; }
    public int TotalPatterns { get; set; }
    public int TotalMeetings { get; set; }
    public int TotalTreeNodes { get; set; }
    public int TotalContent { get; set; }
}
