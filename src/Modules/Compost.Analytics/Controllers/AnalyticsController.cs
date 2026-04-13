using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Compost.Kanban.Models;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using YesSql;

namespace Compost.Analytics.Controllers;

public class AnalyticsController(ISession session, IMindMapService mindMapService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var viewModel = new AnalyticsDashboardViewModel
        {
            VelocityData = await GetVelocityDataAsync(),
            PatternUsageData = await GetPatternUsageDataAsync(),
            ModuleUsageData = await GetModuleUsageDataAsync(),
            ActivityData = await GetActivityDataAsync(),
            SummaryStats = await GetSummaryStatsAsync(),
            RecentActivities = await GetRecentActivitiesAsync()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Velocity(int weeks = 12)
    {
        var data = await GetVelocityDataAsync(weeks);
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

    private async Task<VelocityData> GetVelocityDataAsync(int weeks = 12)
    {
        // Get Kanban cards for velocity calculation
        var kanbanCards = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.ContentType == "KanbanCard" && x.Published)
            .ListAsync();

        var completedCards = kanbanCards
            .Where(x => x.As<KanbanCardPart>()?.Status == KanbanStatus.Done)
            .ToList();

        // Calculate velocity by week (last N weeks)
        var velocityByWeek = new List<WeeklyVelocity>();
        var today = DateTime.Today;
        var thisWeekStart = today.AddDays(-((today.DayOfWeek - DayOfWeek.Monday + 7) % 7));
        
        for (var i = weeks - 1; i >= 0; i--)
        {
            var weekStart = thisWeekStart.AddDays(-(i * 7));
            var weekEnd = weekStart.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
            
            var weekCards = completedCards.Where(x => 
            {
                var part = x.As<KanbanCardPart>();
                return part?.CompletedDate.HasValue == true && 
                       part.CompletedDate.Value >= weekStart && 
                       part.CompletedDate.Value <= weekEnd;
            }).ToList();

            var totalStoryPoints = weekCards.Sum(x => x.As<KanbanCardPart>()?.StoryPoints ?? 0);
            
            velocityByWeek.Add(new WeeklyVelocity
            {
                Week = weekStart.ToString("MMM dd"),
                CompletedCards = weekCards.Count,
                StoryPoints = totalStoryPoints
            });
        }

        // Calculate average velocity (last min(8, weeks) weeks with data)
        var nonZeroWeeks = velocityByWeek.Where(x => x.StoryPoints > 0 || x.CompletedCards > 0).ToList();
        var avgWindow = Math.Min(8, weeks);
        var avgVelocity = nonZeroWeeks.Count >= 4 
            ? nonZeroWeeks.Skip(Math.Max(0, nonZeroWeeks.Count - avgWindow)).Average(x => x.StoryPoints)
            : velocityByWeek.Average(x => x.StoryPoints);
        
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
        var patterns = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.ContentType == "ArchitecturalPattern" && x.Published)
            .ListAsync();
        var patternsList = patterns.ToList();

        var snippets = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.ContentType == "CodeSnippet" && x.Published)
            .ListAsync();
        var snippetsList = snippets.ToList();

        var patternUsage = new List<PatternUsage>();
        
        foreach (var pattern in patternsList)
        {
            var relatedSnippets = snippetsList.Count(x => 
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
            TotalPatterns = patternsList.Count,
            TotalUsage = patternUsage.Sum(x => x.UsageCount)
        };
    }

    private async Task<ModuleUsageData> GetModuleUsageDataAsync()
    {
        // Get content items by type for module usage statistics
        var contentItems = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.Published)
            .ListAsync();
        var contentItemsList = contentItems.ToList();

        // Get mind maps from MindMapService (stored separately from Orchard Core content items)
        var allMindMaps = mindMapService != null
            ? await mindMapService.GetAllMindMapsAsync()
            : [];
        var mindMapNodeCount = allMindMaps.Sum(m => m.NodeCount);

        var moduleStats = new List<ModuleStat>
        {
            new ModuleStat { ModuleName = "Mind Map Nodes", ContentCount = mindMapNodeCount, Icon = "fas fa-project-diagram", Color = "#2563eb" },
            new ModuleStat { ModuleName = "Kanban", ContentCount = contentItemsList.Count(x => x.ContentType == "KanbanCard"), Icon = "fas fa-columns", Color = "#16a34a" },
            new ModuleStat { ModuleName = "Snippets", ContentCount = contentItemsList.Count(x => x.ContentType == "CodeSnippet"), Icon = "fas fa-code", Color = "#0891b2" },
            new ModuleStat { ModuleName = "Patterns", ContentCount = contentItemsList.Count(x => x.ContentType == "ArchitecturalPattern"), Icon = "fas fa-shapes", Color = "#ea580c" },
            new ModuleStat { ModuleName = "Meetings", ContentCount = contentItemsList.Count(x => x.ContentType == "Meeting"), Icon = "fas fa-microphone", Color = "#dc2626" },
            new ModuleStat { ModuleName = "Tree Nodes", ContentCount = contentItemsList.Count(x => x.ContentType == "TreeNode"), Icon = "fas fa-sitemap", Color = "#7c3aed" }
        };

        return new ModuleUsageData
        {
            ModuleStats = moduleStats.OrderByDescending(x => x.ContentCount).ToList(),
            TotalContent = contentItemsList.Count + mindMapNodeCount
        };
    }

    private async Task<ActivityData> GetActivityDataAsync()
    {
        // Get recent activity (last 30 days) using CreatedUtc
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var today = DateTime.Today;

        // Get Orchard Core content items
        var contentItems = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.Published && x.CreatedUtc >= thirtyDaysAgo)
            .ListAsync();
        var contentItemsList = contentItems.ToList();

        // Get mind maps from MindMapService for activity tracking
        var allMindMaps = mindMapService != null
            ? await mindMapService.GetAllMindMapsAsync()
            : [];

        var activityByDay = new List<DailyActivity>();

        for (var i = 29; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayEnd = date.AddDays(1);

            // Count Orchard Core items created on this specific day
            var dayItems = contentItemsList.Where(x =>
            {
                if (x.CreatedUtc == null) return false;
                var createdDate = x.CreatedUtc.Value;
                return createdDate >= date && createdDate < dayEnd;
            }).ToList();

            // Count mind maps created on this specific day (using NodeCount for total nodes)
            var mindMapNodesInDay = allMindMaps
                .Where(m => m.CreatedAt >= date && m.CreatedAt < dayEnd)
                .Sum(m => m.NodeCount);

            activityByDay.Add(new DailyActivity
            {
                Date = date.ToString("MMM dd"),
                MindMaps = mindMapNodesInDay,
                KanbanCards = dayItems.Count(x => x.ContentType == "KanbanCard"),
                Snippets = dayItems.Count(x => x.ContentType == "CodeSnippet"),
                Patterns = dayItems.Count(x => x.ContentType == "ArchitecturalPattern")
            });
        }

        var totalMindMapNodesInPeriod = allMindMaps
            .Where(m => m.CreatedAt >= thirtyDaysAgo)
            .Sum(m => m.NodeCount);

        return new ActivityData
        {
            DailyActivities = activityByDay,
            TotalActivity = contentItemsList.Count + totalMindMapNodesInPeriod
        };
    }

    private async Task<SummaryStats> GetSummaryStatsAsync()
    {
        var contentItems = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.Published)
            .ListAsync();
        var contentItemsList = contentItems.ToList();

        // Get mind maps from MindMapService (stored separately from Orchard Core content items)
        var localMindMapService = mindMapService;
        var allMindMaps = localMindMapService != null
            ? await localMindMapService.GetAllMindMapsAsync()
            : [];
        var totalMindMaps = allMindMaps.Count;
        var totalMindMapNodes = allMindMaps.Sum(m => m.NodeCount);

        return new SummaryStats
        {
            TotalMindMaps = totalMindMaps,
            TotalMindMapNodes = totalMindMapNodes,
            TotalKanbanCards = contentItemsList.Count(x => x.ContentType == "KanbanCard"),
            TotalSnippets = contentItemsList.Count(x => x.ContentType == "CodeSnippet"),
            TotalPatterns = contentItemsList.Count(x => x.ContentType == "ArchitecturalPattern"),
            TotalMeetings = contentItemsList.Count(x => x.ContentType == "Meeting"),
            TotalTreeNodes = contentItemsList.Count(x => x.ContentType == "TreeNode"),
            TotalContent = contentItemsList.Count + totalMindMapNodes
        };
    }

    private async Task<List<RecentActivity>> GetRecentActivitiesAsync()
    {
        var activities = new List<RecentActivity>();
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // Get recent content items
        var contentItems = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.Published && x.CreatedUtc >= sevenDaysAgo)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(10)
            .ListAsync();

        foreach (var item in contentItems)
        {
            var (icon, color, typeName) = item.ContentType switch
            {
                "KanbanCard" => ("fa-tasks", "#2563eb", "Kanban Card"),
                "CodeSnippet" => ("fa-code", "#dc2626", "Snippet"),
                "ArchitecturalPattern" => ("fa-layer-group", "#7c3aed", "Pattern"),
                _ => ("fa-file", "#64748b", "Content")
            };

            activities.Add(new RecentActivity
            {
                Title = item.DisplayText ?? $"New {typeName}",
                Type = typeName,
                Timestamp = item.CreatedUtc ?? DateTime.UtcNow,
                Icon = icon,
                Color = color
            });
        }

        // Get recent mind maps
        if (mindMapService != null)
        {
            var mindMaps = await mindMapService.GetAllMindMapsAsync();
            var recentMindMaps = mindMaps
                .Where(m => m.CreatedAt >= sevenDaysAgo)
                .OrderByDescending(m => m.CreatedAt)
                .Take(5);

            foreach (var mindMap in recentMindMaps)
            {
                activities.Add(new RecentActivity
                {
                    Title = $"{mindMap.Name} ({mindMap.NodeCount} nodes)",
                    Type = "Mind Map",
                    Timestamp = mindMap.CreatedAt,
                    Icon = "fa-project-diagram",
                    Color = "#ff6b35"
                });
            }
        }

        // Sort by timestamp and take top 10
        return activities
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToList();
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
    public List<RecentActivity> RecentActivities { get; set; } = [];
}

public class VelocityData
{
    public List<WeeklyVelocity> WeeklyVelocities { get; set; } = [];
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
    public List<PatternUsage> Patterns { get; set; } = [];
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
    public List<ModuleStat> ModuleStats { get; set; } = [];
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
    public List<DailyActivity> DailyActivities { get; set; } = [];
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
    public int TotalMindMapNodes { get; set; }
    public int TotalKanbanCards { get; set; }
    public int TotalSnippets { get; set; }
    public int TotalPatterns { get; set; }
    public int TotalMeetings { get; set; }
    public int TotalTreeNodes { get; set; }
    public int TotalContent { get; set; }
}

public class RecentActivity
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
