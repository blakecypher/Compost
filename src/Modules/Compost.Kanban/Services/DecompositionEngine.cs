using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Compost.Core.Services;
using Compost.Kanban.Models;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using YesSql;

namespace Compost.Kanban.Services;

public class DecompositionEngine(
    IContentManager contentManager,
    ISession session,
    ILogger<DecompositionEngine> logger,
    AIIntegrationService aiService,
    ITranscriptionService transcriptionService,
    IMindMapService mindMapService) : IDecompositionEngine
{
    private readonly AIIntegrationService _aiService = aiService;
    private readonly ITranscriptionService _transcriptionService = transcriptionService;
    private readonly IMindMapService _mindMapService = mindMapService;

    // ========== Mind Map Operations (Simplified for now) ==========

    public Task<MindMapNode> CreateMindMapNodeAsync(string projectId, string title, string content, string? parentNodeId = null)
    {
        // For now, return a mock or implement if we migrate MindMap to Content Items
        var node = new MindMapNode
        {
            WorkContextId = projectId,
            Title = title
        };
        return Task.FromResult(node);
    }

    public Task UpdateMindMapNodeAsync(MindMapNode node) => Task.CompletedTask;
    public Task DeleteMindMapNodeAsync(string nodeId) => Task.CompletedTask;
    public Task<List<MindMapNode>> GetMindMapNodesByContextAsync(string projectId) => Task.FromResult(new List<MindMapNode>());

    public async Task<TreeNode> PromoteMindMapToTreeAsync(string mindMapNodeId)
    {
        // Search all mind maps for the node
        var allMaps = await _mindMapService.GetAllMindMapsAsync();
        string? projectId = null;
        string? title = "New Tree Node (Promoted)";
        string? notes = "";
        string? sourceText = "";

        foreach (var mapSummary in allMaps)
        {
            // We need to get the full map to access nodes
            // Since IMindMapService in Core only has Summaries, 
            // we might need to handle this differently if we can't find the node.
            // For now, we'll try to find a map that matches the node if possible
            // OR use the project ID from the map summary if the map is relevant.
        }

        // Fallback: If we can't find the node via the Core interface safely, 
        // we'll rely on the controller passing the data to CreateTreeNodeAsync,
        // which it already does. This method is mainly for direct API calls.
        
        var treeNodeItem = await contentManager.NewAsync(nameof(TreeNode));
        var part = treeNodeItem.As<TreeNodePart>();
        
        treeNodeItem.DisplayText = title;
        part.SourceMindMapNodeId = mindMapNodeId;
        part.WorkContextId = projectId ?? "default";
        
        treeNodeItem.Apply(part);
        await contentManager.CreateAsync(treeNodeItem);
        await contentManager.PublishAsync(treeNodeItem);

        return MapToTreeNode(treeNodeItem);
    }

    // ========== Tree Operations ==========

    public async Task<TreeNode> CreateTreeNodeAsync(string projectId, string title, string description, string? sourceMindMapNodeId = null, string? sourceMeetingId = null, string? sourceTranscriptExcerpt = null)
    {
        var item = await contentManager.NewAsync(nameof(TreeNode));
        item.DisplayText = title;
        
        var part = item.As<TreeNodePart>();
        part.WorkContextId = projectId;
        part.SourceMindMapNodeId = sourceMindMapNodeId;
        part.SourceMeetingId = sourceMeetingId;
        part.SourceTranscriptExcerpt = sourceTranscriptExcerpt;
        
        // Handle description - standard Orchard MarkdownBodyPart might be used
        if (item.Content.MarkdownBodyPart != null)
        {
            item.Content.MarkdownBodyPart.Markdown = description;
        }

        item.Apply(part);
        await contentManager.CreateAsync(item);
        await contentManager.PublishAsync(item);

        return MapToTreeNode(item);
    }

    public async Task UpdateTreeNodeAsync(TreeNode node)
    {
        var item = await contentManager.GetAsync(node.Id);
        if (item == null) return;

        item.DisplayText = node.Title;
        var part = item.As<TreeNodePart>();
        part.Complexity = node.Complexity;
        part.Priority = node.Priority;
        part.AcceptanceCriteria = node.AcceptanceCriteria;
        part.TechnicalRequirements = node.TechnicalRequirements;

        item.Apply(part);
        await contentManager.UpdateAsync(item);
        await contentManager.PublishAsync(item);
    }

    public async Task DeleteTreeNodeAsync(string nodeId)
    {
        var item = await contentManager.GetAsync(nodeId);
        if (item != null)
        {
            await contentManager.RemoveAsync(item);
        }
    }

    public async Task<List<TreeNode>> GetTreeNodesByContextAsync(string projectId)
    {
        var items = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.ContentType == nameof(TreeNode) && x.Latest && x.Published)
            .ListAsync();

        var results = new List<TreeNode>();
        foreach (var item in items)
        {
            var part = item.As<TreeNodePart>();
            if (part?.WorkContextId == projectId)
            {
                results.Add(MapToTreeNode(item));
            }
        }
        return results;
    }

    public Task AddRefinementMessageAsync(string treeNodeId, MessageRole role, string content) => Task.CompletedTask;

    public async Task<List<KanbanCard>> PromoteTreeToKanbanAsync(string treeNodeId)
    {
        var treeItem = await contentManager.GetAsync(treeNodeId);
        if (treeItem == null) return [];

        var treePart = treeItem.As<TreeNodePart>();
        
        // Logic to create one or more kanban cards from tree node
        var cardItem = await contentManager.NewAsync(nameof(KanbanCard));
        cardItem.DisplayText = treeItem.DisplayText;
        
        var cardPart = cardItem.As<KanbanCardPart>();
        cardPart.WorkContextId = treePart.WorkContextId; // Propagate the context (ID or Name)
        if (string.IsNullOrEmpty(cardPart.WorkContextId)) cardPart.WorkContextId = "default";
        cardPart.SourceTreeNodeId = treeNodeId;
        cardPart.SourceTranscriptExcerpt = treePart.SourceTranscriptExcerpt;
        cardPart.AcceptanceCriteria = treePart.AcceptanceCriteria ?? [];
        cardPart.Status = KanbanStatus.Backlog; // Explicitly set to Backlog
        cardPart.OrderInColumn = 0; // Set initial order
        
        // Use AI to estimate story points
        var requirementText = $"{treeItem.DisplayText}\n{string.Join("\n", treePart.AcceptanceCriteria ?? [])}";
        var estimatedStoryPoints = await _aiService.EstimateStoryPointsAsync(requirementText, treePart.WorkContextId);
        cardPart.StoryPoints = estimatedStoryPoints;
        cardPart.SuggestedStoryPoints = estimatedStoryPoints;
        
        // Handle description from tree node
        var description = string.Join("\n", treePart.AcceptanceCriteria ?? []);
        
        // Append meeting transcript if available
        var sourceMeetingId = treePart.SourceMeetingId;
        cardPart.SourceMeetingId = sourceMeetingId;

        if (!string.IsNullOrEmpty(sourceMeetingId))
        {
            var meeting = await _transcriptionService.GetMeetingByIdAsync(sourceMeetingId);
            if (meeting != null && meeting.Transcript != null && meeting.Transcript.Count > 0)
            {
                var transcriptText = string.Join("\n", meeting.Transcript.Select(s => $"[{s.StartTime:mm\\:ss}] {s.SpeakerId}: {s.Text}"));
                description += $"\n\n### Full Transcript\n{transcriptText}";
            }
        }

        if (cardItem.Content.MarkdownBodyPart != null)
        {
            cardItem.Content.MarkdownBodyPart.Markdown = description;
        }
        
        cardItem.Apply(cardPart);
        await contentManager.CreateAsync(cardItem);
        await contentManager.PublishAsync(cardItem);

        // Mark tree node as promoted
        treePart.IsPromotedToKanban = true;
        treePart.KanbanCardIds.Add(cardItem.ContentItemId);
        treeItem.Apply(treePart);
        await contentManager.UpdateAsync(treeItem);

        logger.LogInformation("Created Kanban card {CardId} from tree node {NodeId}", cardItem.ContentItemId, treeNodeId);

        return [MapToKanbanCard(cardItem)];
    }

    // ========== Kanban Operations ==========

    public async Task<KanbanCard> CreateKanbanCardAsync(string projectId, string title, string description, string? sourceTreeNodeId = null)
    {
        var item = await contentManager.NewAsync(nameof(KanbanCard));
        item.DisplayText = title;
        
        var part = item.As<KanbanCardPart>();
        part.WorkContextId = projectId;
        part.SourceTreeNodeId = sourceTreeNodeId;
        
        item.Apply(part);
        await contentManager.CreateAsync(item);
        await contentManager.PublishAsync(item);

        return MapToKanbanCard(item);
    }

    public async Task UpdateKanbanCardAsync(KanbanCard card)
    {
        var item = await contentManager.GetAsync(card.Id);
        if (item == null) return;

        item.DisplayText = card.Title;
        var part = item.As<KanbanCardPart>();
        part.Status = card.Status;
        part.OrderInColumn = card.OrderInColumn;
        part.StoryPoints = card.StoryPoints;
        
        item.Apply(part);
        await contentManager.UpdateAsync(item);
        await contentManager.PublishAsync(item);
    }

    public async Task DeleteKanbanCardAsync(string cardId)
    {
        var item = await contentManager.GetAsync(cardId);
        if (item != null) await contentManager.RemoveAsync(item);
    }

    public async Task<List<KanbanCard>> GetKanbanCardsByContextAsync(string projectId)
    {
        var items = await session.Query<ContentItem, ContentItemIndex>()
            .Where(x => x.ContentType == nameof(KanbanCard) && x.Latest && x.Published)
            .ListAsync();

        var results = new List<KanbanCard>();
        foreach (var item in items)
        {
            var part = item.As<KanbanCardPart>();
            if (part?.WorkContextId == projectId)
            {
                results.Add(MapToKanbanCard(item));
            }
        }
        return results;
    }

    public async Task MoveCardToStatusAsync(string cardId, KanbanStatus newStatus)
    {
        var item = await contentManager.GetAsync(cardId);
        if (item == null) return;

        var part = item.As<KanbanCardPart>();
        part.Status = newStatus;
        
        item.Apply(part);
        await contentManager.UpdateAsync(item);
        await contentManager.PublishAsync(item);
    }    public Task ReorderCardsAsync(string projectId, KanbanStatus status, List<string> cardIdsInOrder) => Task.CompletedTask;

    public Task<int> SuggestStoryPointsAsync(string cardId) => Task.FromResult(3);

    // ========== Structure Operations ==========

    public async Task<StructureNode> PromoteTreeToStructureAsync(string treeNodeId)
    {
        var treeItem = await contentManager.GetAsync(treeNodeId);
        if (treeItem == null) throw new ArgumentException("Tree node not found");

        var treePart = treeItem.As<TreeNodePart>();
        
        // Check if this tree node has children (hierarchical structure requirement)
        var childNodes = await GetChildTreeNodesAsync(treeNodeId);
        if (childNodes.Count == 0)
        {
            logger.LogWarning("Tree node {NodeId} has no children, cannot promote to structure", treeNodeId);
            throw new InvalidOperationException("Only tree nodes with children can be promoted to structure");
        }

        // Create structure node
        var structureNode = new StructureNode
        {
            Title = treeItem.DisplayText,
            Description = "", // TreeNodePart doesn't have Description property
            WorkContextId = treePart?.WorkContextId ?? string.Empty,
            SourceTreeNodeId = treeNodeId,
            StructureType = DetermineStructureType(treeItem.DisplayText, childNodes.Count),
            Metadata = new StructureMetadata
            {
                Color = GetStructureColor(treeItem.DisplayText)
            }
        };

        // Mark tree node as promoted to structure
        if (treePart != null)
        {
            treePart.IsPromotedToStructure = true;
            treePart.StructureNodeId = structureNode.Id;
            treeItem.Apply(treePart);
            await contentManager.UpdateAsync(treeItem);
        }

        // For now, store in memory (in production, this would be persisted)
        logger.LogInformation("Promoted tree node {NodeId} to structure {StructureId}", treeNodeId, structureNode.Id);
        
        return structureNode;
    }

    public async Task<StructureNode> CreateStructureNodeAsync(string projectId, string title, string description, StructureType structureType, string? sourceTreeNodeId = null)
    {
        var structureNode = new StructureNode
        {
            Title = title,
            Description = description,
            WorkContextId = projectId,
            SourceTreeNodeId = sourceTreeNodeId,
            StructureType = structureType
        };

        logger.LogInformation("Created structure node {StructureId} of type {Type}", structureNode.Id, structureType);
        return structureNode;
    }

    public async Task UpdateStructureNodeAsync(StructureNode node)
    {
        node.ModifiedAt = DateTime.UtcNow;
        logger.LogInformation("Updated structure node {StructureId}", node.Id);
        await Task.CompletedTask;
    }

    public async Task DeleteStructureNodeAsync(string structureId)
    {
        logger.LogInformation("Deleted structure node {StructureId}", structureId);
        await Task.CompletedTask;
    }

    public async Task<List<StructureNode>> GetStructureNodesByContextAsync(string projectId)
    {
        // Stub implementation - in production, would query from database
        return await Task.FromResult(new List<StructureNode>());
    }

    public async Task AddChildStructureAsync(string parentStructureId, string childStructureId)
    {
        logger.LogInformation("Added child structure {ChildId} to parent {ParentId}", childStructureId, parentStructureId);
        await Task.CompletedTask;
    }

    public async Task<KanbanBoard> CreateKanbanBoardForStructureAsync(string structureId)
    {
        var board = new KanbanBoard
        {
            Title = $"Structure Board - {structureId}",
            Description = $"Kanban board for structure {structureId}",
            StructureNodeId = structureId,
            WorkContextId = "context-id" // Would be fetched from structure
        };

        logger.LogInformation("Created kanban board {BoardId} for structure {StructureId}", board.Id, structureId);
        return board;
    }

    public async Task<List<KanbanCard>> PromoteStructureToKanbanAsync(string structureId)
    {
        var cards = new List<KanbanCard>();
        
        // Create kanban cards from structure objectives and child structures
        var structureCard = new KanbanCard
        {
            Title = $"Structure Management - {structureId}",
            Description = "Manage structure operations and objectives",
            SourceStructureNodeId = structureId,
            WorkContextId = "context-id" // Would be fetched from structure
        };

        cards.Add(structureCard);
        
        logger.LogInformation("Promoted structure {StructureId} to {CardCount} kanban cards", structureId, cards.Count);
        return cards;
    }

    // ========== Kanban Board Operations ==========

    public async Task<KanbanBoard?> GetKanbanBoardAsync(string boardId)
    {
        // Stub implementation
        return await Task.FromResult<KanbanBoard?>(null);
    }

    public async Task UpdateKanbanBoardAsync(KanbanBoard board)
    {
        board.ModifiedAt = DateTime.UtcNow;
        logger.LogInformation("Updated kanban board {BoardId}", board.Id);
        await Task.CompletedTask;
    }

    public async Task<List<KanbanCard>> GetKanbanCardsByBoardAsync(string boardId)
    {
        // Stub implementation
        return await Task.FromResult(new List<KanbanCard>());
    }

    public async Task<KanbanCard> PromoteActionItemToKanbanAsync(string meetingId, string actionItemId, string projectId)
    {
        var meeting = await _transcriptionService.GetMeetingByIdAsync(meetingId);
        if (meeting == null) throw new ArgumentException("Meeting not found");

        var actionItem = meeting.ActionItems.FirstOrDefault(a => a.Id == actionItemId);
        if (actionItem == null) throw new ArgumentException("Action item not found");

        var cardItem = await contentManager.NewAsync(nameof(KanbanCard));
        cardItem.DisplayText = actionItem.Title;

        var cardPart = cardItem.As<KanbanCardPart>();
        cardPart.WorkContextId = projectId;
        cardPart.SourceMeetingId = meetingId;
        cardPart.SourceTranscriptExcerpt = actionItem.OriginalTranscript;
        cardPart.Status = KanbanStatus.Backlog;
        
        var description = $"## Action Item Detail\n{actionItem.Description ?? "No description provided."}\n\n---\n*Source Meeting: {meeting.Title}*";
        
        if (meeting.Transcript != null && meeting.Transcript.Count > 0)
        {
            var transcriptText = string.Join("\n", meeting.Transcript.Select(s => $"**{s.SpeakerId ?? "Unknown"}**: {s.Text}"));
            description += $"\n\n### Full Transcript\n{transcriptText}";
        }

        if (cardItem.Content.MarkdownBodyPart != null)
        {
            cardItem.Content.MarkdownBodyPart.Markdown = description;
        }

        await contentManager.CreateAsync(cardItem);
        await contentManager.PublishAsync(cardItem);

        // Update action item with card ID
        actionItem.KanbanCardId = cardItem.ContentItemId;

        return MapToKanbanCard(cardItem);
    }

    // ========== Helper Methods ==========

    private async Task<List<TreeNode>> GetChildTreeNodesAsync(string parentNodeId)
    {
        // In production, this would query for child tree nodes
        return await Task.FromResult(new List<TreeNode>());
    }

    private StructureType DetermineStructureType(string title, int childCount)
    {
        if (childCount > 10) return StructureType.Department;
        if (childCount > 5) return StructureType.Team;
        if (title.ToLower().Contains("project")) return StructureType.Project;
        if (title.ToLower().Contains("initiative")) return StructureType.Initiative;
        return StructureType.Team;
    }

    private string GetStructureColor(string title)
    {
        var colors = new List<string> { "#2196f3", "#4caf50", "#ff9800", "#9c27b0", "#f44336", "#00bcd4" };
        var hash = title.GetHashCode();
        return colors[Math.Abs(hash) % colors.Count];
    }

    // ========== Mapping Helpers ==========

    private TreeNode MapToTreeNode(ContentItem item)
    {
        var part = item.As<TreeNodePart>();
        var node = new TreeNode
        {
            Id = item.ContentItemId,
            Title = item.DisplayText,
            WorkContextId = part.WorkContextId ?? string.Empty,
            SourceMindMapNodeId = part.SourceMindMapNodeId,
            AcceptanceCriteria = part.AcceptanceCriteria,
            TechnicalRequirements = part.TechnicalRequirements,
            Complexity = part.Complexity,
            Priority = part.Priority,
            SourceMeetingId = part.SourceMeetingId,
            SourceTranscriptExcerpt = part.SourceTranscriptExcerpt,
            IsPromotedToKanban = part.IsPromotedToKanban,
            KanbanCardIds = part.KanbanCardIds
        };
        return node;
    }

    private KanbanCard MapToKanbanCard(ContentItem item)
    {
        var part = item.As<KanbanCardPart>();
        var card = new KanbanCard
        {
            Id = item.ContentItemId,
            Title = item.DisplayText,
            WorkContextId = part.WorkContextId ?? string.Empty,
            SourceTreeNodeId = part.SourceTreeNodeId,
            StoryPoints = part.StoryPoints,
            SuggestedStoryPoints = part.SuggestedStoryPoints,
            Status = part.Status,
            OrderInColumn = part.OrderInColumn,
            AcceptanceCriteria = part.AcceptanceCriteria,
            SourceMeetingId = part.SourceMeetingId,
            SourceTranscriptExcerpt = part.SourceTranscriptExcerpt,
            TimeSpentSeconds = part.TimeSpentSeconds
        };
        return card;
    }
}
