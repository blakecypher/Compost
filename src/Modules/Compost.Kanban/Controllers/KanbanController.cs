using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Compost.Kanban.Models;
using Compost.Kanban.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using YesSql;

namespace Compost.Kanban.Controllers;

public class KanbanController(
    IContentManager contentManager,
    ISession session,
    IProjectManager projectManager,
    ILogger<KanbanController> logger) : Controller
{
    public async Task<IActionResult> Index(string projectId = null)
    {
        var projects = await projectManager.GetAllProjectsAsync();

        logger.LogInformation("Loading Kanban board for project: {ProjectId}", projectId ?? "ALL");

        // Fetch all Published KanbanCard content items.
        var allRows = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.ContentType == nameof(KanbanCard) && x.Published)
            .ListAsync();

        // Deduplicate by ContentItemId - prefer the Latest version.
        var allCards = allRows
            .GroupBy(c => c.ContentItemId)
            .Select(g => g.FirstOrDefault(c => c.Latest) ?? g.First())
            .ToList();

        logger.LogInformation(
            "KanbanCard query: {TotalRows} total rows → {Distinct} distinct published cards",
            allRows.Count(), allCards.Count);

        // Extract all unique WorkContextId values from the cards themselves
        // so we can build a complete filter list (includes "research", "meeting", etc.)
        var cardContextIds = allCards
            .Select(c => c.As<KanbanCardPart>()?.WorkContextId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id)
            .ToList();

        // Build a merged set of filter options:
        // registered projects (with proper names) + any raw context IDs found only in cards
        var registeredIds = new HashSet<string>(projects.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);
        var cardOnlyContexts = cardContextIds
            .Where(id => !registeredIds.Contains(id))
            .Select(id => new Project { Id = id, Name = id }) // treat raw ID as display name
            .ToList();

        var allContextOptions = projects.Concat(cardOnlyContexts).ToList();

        // Filter cards: if no project selected, show all; otherwise match by WorkContextId directly
        List<ContentItem> kanbanCards;
        var selectedContextId = projectId; // null = All

        if (string.IsNullOrEmpty(selectedContextId))
        {
            // Show all published cards
            kanbanCards = allCards.Where(c => c.As<KanbanCardPart>() != null).ToList();
        }
        else
        {
            // Find the matching project for display-name resolution
            var selectedProject = projects.FirstOrDefault(p =>
                string.Equals(p.Id, selectedContextId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, selectedContextId, StringComparison.OrdinalIgnoreCase));

            // Normalize to ID if a name was passed
            var matchId = selectedProject?.Id ?? selectedContextId;

            kanbanCards = [];
            foreach (var card in allCards)
            {
                var part = card.As<KanbanCardPart>();
                if (part == null) continue;

                var matches =
                    // Direct WorkContextId match (covers raw labels like "research", "meeting")
                    string.Equals(part.WorkContextId, matchId, StringComparison.OrdinalIgnoreCase) ||
                    // Also match by registered project name
                    (selectedProject != null && string.Equals(part.WorkContextId, selectedProject.Name, StringComparison.OrdinalIgnoreCase)) ||
                    // Orphaned cards (no context) are included when "All" OR when viewing any specific project
                    string.IsNullOrEmpty(part.WorkContextId) ||
                    string.Equals(part.WorkContextId, "default", StringComparison.OrdinalIgnoreCase);

                if (matches) kanbanCards.Add(card);
            }
        }

        logger.LogInformation("Final Kanban cards count: {Count} for context {Project}", kanbanCards.Count, selectedContextId ?? "ALL");

        var model = new KanbanBoardViewModel
        {
            Contexts = allContextOptions,
            SelectedContextId = selectedContextId, // null = All
            Cards = kanbanCards
        };

        return View(model);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateStatus([FromForm] string contentItemId, [FromForm] KanbanStatus newStatus, [FromForm] int newOrder)
    {
        var contentItem = await contentManager.GetAsync(contentItemId, VersionOptions.Latest)
                         ?? await contentManager.GetAsync(contentItemId, VersionOptions.Published);
        if (contentItem == null) return NotFound();

        var part = contentItem.As<KanbanCardPart>();
        if (part == null) return BadRequest();

        part.Status = newStatus;
        part.OrderInColumn = newOrder;
        
        contentItem.Apply(part);
        await contentManager.UpdateAsync(contentItem);
        await contentManager.PublishAsync(contentItem);

        return Ok();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateCardField([FromForm] string contentItemId, [FromForm] string field, [FromForm] string value)
    {
        var contentItem = await GetCardContentItemAsync(contentItemId);
        if (contentItem == null) return NotFound();

        var part = contentItem.As<KanbanCardPart>();
        if (part == null) return BadRequest();

        switch (field.ToLower())
        {
            case "title":
                contentItem.DisplayText = value;
                break;
            case "description":
                if (contentItem.Content.MarkdownBodyPart != null)
                {
                    contentItem.Content.MarkdownBodyPart.Markdown = value;
                }
                break;
            case "storypoints":
                if (int.TryParse(value, out var points))
                {
                    part.StoryPoints = points;
                }
                break;
        }
        
        contentItem.Apply(part);
        await contentManager.UpdateAsync(contentItem);
        await contentManager.PublishAsync(contentItem);

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetCard(string id)
    {
        try
        {
            logger.LogInformation("Getting card data for ID: {CardId}", id);
            
            var contentItem = await GetCardContentItemAsync(id);
            if (contentItem == null) 
            {
                logger.LogWarning("Card not found: {CardId}", id);
                return NotFound();
            }

            var part = contentItem.As<KanbanCardPart>();
            if (part == null) 
            {
                logger.LogWarning("KanbanCardPart not found for card: {CardId}", id);
                return BadRequest();
            }

            // Build response with safe null handling
            var response = new
            {
                contentItemId = contentItem.ContentItemId ?? "",
                title = contentItem.DisplayText ?? "",
                description = contentItem.Content.MarkdownBodyPart?.Markdown ?? "",
                storyPoints = part.StoryPoints,
                priority = part.Priority.ToString(),
                dueDate = part.DueDate?.ToString("yyyy-MM-dd") ?? "",
                assignee = part.Assignee ?? "",
                status = part.Status.ToString(),
                isBlocked = part.IsBlocked,
                blockedReason = part.BlockedReason ?? "",
                sourceTranscriptExcerpt = part.SourceTranscriptExcerpt ?? ""
            };

            logger.LogInformation("Successfully loaded card: {CardId}, Title: {Title}", id, response.title);
            return Json(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading card data for ID: {CardId}", id);
            return Json(new { success = false, error = "Error loading card data", details = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateCard([FromBody] CardUpdateRequest request)
    {
        var contentItem = await GetCardContentItemAsync(request.ContentItemId);
        if (contentItem == null) return NotFound();

        var part = contentItem.As<KanbanCardPart>();
        if (part == null) return BadRequest();

        // Update basic fields
        contentItem.DisplayText = request.Title;
        if (contentItem.Content.MarkdownBodyPart != null)
        {
            contentItem.Content.MarkdownBodyPart.Markdown = request.Description;
        }

        // Update KanbanCardPart fields
        part.StoryPoints = request.StoryPoints;
        if (Enum.TryParse<PriorityLevel>(request.Priority, out var priority))
        {
            part.Priority = priority;
        }
        if (!string.IsNullOrEmpty(request.DueDate) && DateTime.TryParse(request.DueDate, out var dueDate))
        {
            part.DueDate = dueDate;
        }
        else if (string.IsNullOrEmpty(request.DueDate))
        {
            part.DueDate = null;
        }
        part.Assignee = request.Assignee;
        if (Enum.TryParse<KanbanStatus>(request.Status, out var status))
        {
            part.Status = status;
        }
        part.IsBlocked = request.IsBlocked;
        part.BlockedReason = request.BlockedReason;
        part.SourceTranscriptExcerpt = request.SourceTranscriptExcerpt;

        contentItem.Apply(part);
        await contentManager.UpdateAsync(contentItem);
        await contentManager.PublishAsync(contentItem);

        return Json(new { success = true });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeleteCard([FromForm] string contentItemId)
    {
        try
        {
            var contentItem = await GetCardContentItemAsync(contentItemId);
            if (contentItem == null) return NotFound();

            await contentManager.RemoveAsync(contentItem);
            logger.LogInformation("Deleted Kanban card: {CardId}", contentItemId);

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Kanban card: {CardId}", contentItemId);
            return Json(new { success = false, error = ex.Message });
        }
    }

    private async Task<ContentItem?> GetCardContentItemAsync(string id)
    {
        return await contentManager.GetAsync(id, VersionOptions.Latest)
               ?? await contentManager.GetAsync(id, VersionOptions.Published);
    }
}
