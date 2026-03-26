using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Compost.Kanban.Models;
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
        string selectedContextId = projectId; // null = All

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

            kanbanCards = new List<ContentItem>();
            foreach (var card in allCards)
            {
                var part = card.As<KanbanCardPart>();
                if (part == null) continue;

                bool matches =
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
        var contentItem = await contentManager.GetAsync(contentItemId, VersionOptions.Latest)
                         ?? await contentManager.GetAsync(contentItemId, VersionOptions.Published);
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
                if (int.TryParse(value, out int points))
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
            
            var contentItem = await contentManager.GetAsync(id, VersionOptions.Latest)
                             ?? await contentManager.GetAsync(id, VersionOptions.Published);
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
        var contentItem = await contentManager.GetAsync(request.ContentItemId, VersionOptions.Latest)
                         ?? await contentManager.GetAsync(request.ContentItemId, VersionOptions.Published);
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
            var contentItem = await contentManager.GetAsync(contentItemId, VersionOptions.Latest)
                             ?? await contentManager.GetAsync(contentItemId, VersionOptions.Published);
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

    // Debug endpoint to create a test card
    [HttpGet]
    public async Task<IActionResult> CreateTestCard()
    {
        var cardItem = await contentManager.NewAsync(nameof(KanbanCard));
        cardItem.DisplayText = "Test Card - " + DateTime.Now.ToString("HH:mm:ss");
        
        var cardPart = cardItem.As<KanbanCardPart>();
        cardPart.WorkContextId = "test-context";
        cardPart.Status = KanbanStatus.Backlog;
        cardPart.OrderInColumn = 0;
        cardPart.AcceptanceCriteria = ["Test acceptance criteria 1", "Test acceptance criteria 2"];
        
        if (cardItem.Content.MarkdownBodyPart != null)
        {
            cardItem.Content.MarkdownBodyPart.Markdown = "This is a test card created for debugging purposes.";
        }
        
        await contentManager.CreateAsync(cardItem);
        await contentManager.PublishAsync(cardItem);

        logger.LogInformation("Created test Kanban card: {CardId}, Latest: {Latest}, Published: {Published}", 
            cardItem.ContentItemId, cardItem.Latest, cardItem.Published);

        return Json(new { 
            success = true, 
            cardId = cardItem.ContentItemId,
            title = cardItem.DisplayText,
            status = cardPart.Status
        });
    }

    // Debug endpoint to publish all unpublished cards
    [HttpGet]
    public async Task<IActionResult> PublishAllCards()
    {
        try
        {
            // Get all cards regardless of published status
            var allCards = await session.Query<ContentItem, ContentItemIndex>()
                .Where(x => x.ContentType == nameof(KanbanCard))
                .ListAsync();

            var publishedCount = 0;
            foreach (var card in allCards)
            {
                // Try to get the latest version and publish it
                var latestVersion = await contentManager.GetAsync(card.ContentItemId, VersionOptions.Latest);
                if (latestVersion != null && !latestVersion.Published)
                {
                    await contentManager.PublishAsync(latestVersion);
                    publishedCount++;
                    logger.LogInformation("Published latest version of card: {CardId}", card.ContentItemId);
                }
                else if (latestVersion == null)
                {
                    // If no latest version, try to publish the current version
                    if (!card.Published)
                    {
                        await contentManager.PublishAsync(card);
                        publishedCount++;
                        logger.LogInformation("Published card: {CardId}", card.ContentItemId);
                    }
                }
            }

            return Json(new { 
                success = true, 
                publishedCount = publishedCount,
                totalCards = allCards.Count(),
                message = $"Published {publishedCount} out of {allCards.Count()} cards"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing cards");
            return Json(new { success = false, error = ex.Message });
        }
    }

    // Debug endpoint to list all cards with their status
    [HttpGet]
    public async Task<IActionResult> ListAllCards()
    {
        try
        {
            var allRows = await session.Query<ContentItem, ContentItemIndex>()
                .Where(x => x.ContentType == nameof(KanbanCard))
                .ListAsync();

            // Group by ContentItemId to detect versioning duplicates
            var grouped = allRows
                .GroupBy(c => c.ContentItemId)
                .Select(g =>
                {
                    var latest = g.FirstOrDefault(c => c.Latest) ?? g.OrderByDescending(c => c.ModifiedUtc).First();
                    var part = latest.As<KanbanCardPart>();
                    return new
                    {
                        Id = latest.ContentItemId,
                        Title = latest.DisplayText,
                        VersionCount = g.Count(),
                        Latest = latest.Latest,
                        Published = latest.Published,
                        ShowsOnBoard = latest.Latest && latest.Published,
                        Status = part?.Status.ToString() ?? "N/A",
                        WorkContextId = part?.WorkContextId ?? "(empty)",
                        ExclusionReason = (!latest.Latest ? "Not Latest" :
                                          !latest.Published ? "Not Published" :
                                          string.IsNullOrEmpty(part?.WorkContextId) ? "No WorkContextId - orphan" : "OK")
                    };
                })
                .OrderBy(c => c.ExclusionReason)
                .ToList();

            var boardCount = grouped.Count(c => c.ShowsOnBoard);
            var notPublished = grouped.Count(c => !c.Published);
            var notLatest = grouped.Count(c => !c.Latest);
            var orphans = grouped.Count(c => string.IsNullOrEmpty(c.WorkContextId.Trim('(', ')')));

            return Json(new
            {
                success = true,
                totalCmsRows = allRows.Count(),
                distinctCards = grouped.Count,
                boardVisibleCount = boardCount,
                notPublishedCount = notPublished,
                notLatestCount = notLatest,
                orphanCount = orphans,
                cards = grouped
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing cards");
            return Json(new { success = false, error = ex.Message });
        }
    }
}

public class KanbanBoardViewModel
{
    public List<Project> Contexts { get; set; } = [];
    public string? SelectedContextId { get; set; }
    public List<ContentItem> Cards { get; set; } = [];
}

public class CardUpdateRequest
{
    public string ContentItemId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int? StoryPoints { get; set; }
    public string Priority { get; set; }
    public string DueDate { get; set; }
    public string Assignee { get; set; }
    public string Status { get; set; }
    public bool IsBlocked { get; set; }
    public string BlockedReason { get; set; }
    public string SourceTranscriptExcerpt { get; set; }
}
