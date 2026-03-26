using Compost.Core.Models;

namespace Compost.Core.Interfaces;

/// <summary>
/// Manages the progressive refinement from mind map → tree → kanban
/// </summary>
public interface IDecompositionEngine
{
    // ========== Mind Map Operations ==========
    
    /// <summary>
    /// Create a new mind map node
    /// </summary>
    Task<MindMapNode> CreateMindMapNodeAsync(
        string projectId, 
        string title, 
        string content,
        string? parentNodeId = null);

    /// <summary>
    /// Update a mind map node
    /// </summary>
    Task UpdateMindMapNodeAsync(MindMapNode node);

    /// <summary>
    /// Delete a mind map node and its children
    /// </summary>
    Task DeleteMindMapNodeAsync(string nodeId);

    /// <summary>
    /// Get all mind map nodes for a context
    /// </summary>
    Task<List<MindMapNode>> GetMindMapNodesByContextAsync(string projectId);

    /// <summary>
    /// Promote a mind map node to a tree node
    /// </summary>
    Task<TreeNode> PromoteMindMapToTreeAsync(string mindMapNodeId);

    // ========== Tree Operations ==========

    /// <summary>
    /// Create a new tree node (can be from scratch or promoted from mind map)
    /// </summary>
    Task<TreeNode> CreateTreeNodeAsync(
        string projectId,
        string title,
        string description,
        string? sourceMindMapNodeId = null,
        string? sourceMeetingId = null,
        string? sourceTranscriptExcerpt = null);

    /// <summary>
    /// Update a tree node
    /// </summary>
    Task UpdateTreeNodeAsync(TreeNode node);

    /// <summary>
    /// Delete a tree node and its children
    /// </summary>
    Task DeleteTreeNodeAsync(string nodeId);

    /// <summary>
    /// Get all tree nodes for a context
    /// </summary>
    Task<List<TreeNode>> GetTreeNodesByContextAsync(string projectId);

    /// <summary>
    /// Add a refinement message to a tree node (interactive Q&A)
    /// </summary>
    Task AddRefinementMessageAsync(string treeNodeId, MessageRole role, string content);

    /// <summary>
    /// Promote a tree node to kanban cards
    /// </summary>
    Task<List<KanbanCard>> PromoteTreeToKanbanAsync(string treeNodeId);

    // ========== Structure Operations ==========

    /// <summary>
    /// Promote a tree node with children to a structure node
    /// </summary>
    Task<StructureNode> PromoteTreeToStructureAsync(string treeNodeId);

    /// <summary>
    /// Create a new structure node
    /// </summary>
    Task<StructureNode> CreateStructureNodeAsync(
        string projectId,
        string title,
        string description,
        StructureType structureType,
        string? sourceTreeNodeId = null);

    /// <summary>
    /// Update a structure node
    /// </summary>
    Task UpdateStructureNodeAsync(StructureNode node);

    /// <summary>
    /// Delete a structure node and reassign children
    /// </summary>
    Task DeleteStructureNodeAsync(string structureId);

    /// <summary>
    /// Get all structure nodes for a context
    /// </summary>
    Task<List<StructureNode>> GetStructureNodesByContextAsync(string projectId);

    /// <summary>
    /// Add a child structure to a parent structure
    /// </summary>
    Task AddChildStructureAsync(string parentStructureId, string childStructureId);

    /// <summary>
    /// Create a kanban board for a structure node
    /// </summary>
    Task<KanbanBoard> CreateKanbanBoardForStructureAsync(string structureId);

    /// <summary>
    /// Promote a structure node to kanban cards
    /// </summary>
    Task<List<KanbanCard>> PromoteStructureToKanbanAsync(string structureId);

    // ========== Kanban Board Operations ==========

    /// <summary>
    /// Get a kanban board by ID
    /// </summary>
    Task<KanbanBoard?> GetKanbanBoardAsync(string boardId);

    /// <summary>
    /// Update kanban board configuration
    /// </summary>
    Task UpdateKanbanBoardAsync(KanbanBoard board);

    /// <summary>
    /// Get kanban cards for a specific board
    /// </summary>
    Task<List<KanbanCard>> GetKanbanCardsByBoardAsync(string boardId);

    // ========== Kanban Operations ==========

    /// <summary>
    /// Create a new kanban card
    /// </summary>
    Task<KanbanCard> CreateKanbanCardAsync(
        string projectId,
        string title,
        string description,
        string? sourceTreeNodeId = null);

    /// <summary>
    /// Update a kanban card
    /// </summary>
    Task UpdateKanbanCardAsync(KanbanCard card);

    /// <summary>
    /// Delete a kanban card
    /// </summary>
    Task DeleteKanbanCardAsync(string cardId);

    /// <summary>
    /// Get all kanban cards for a context
    /// </summary>
    Task<List<KanbanCard>> GetKanbanCardsByContextAsync(string projectId);

    /// <summary>
    /// Move a card to a different status
    /// </summary>
    Task MoveCardToStatusAsync(string cardId, KanbanStatus newStatus);

    /// <summary>
    /// Reorder cards within a status column
    /// </summary>
    Task ReorderCardsAsync(string projectId, KanbanStatus status, List<string> cardIdsInOrder);

    /// <summary>
    /// Suggest story points for a kanban card based on complexity
    /// </summary>
    Task<int> SuggestStoryPointsAsync(string cardId);

    /// <summary>
    /// Promote an individual action item from a meeting to kanban
    /// </summary>
    Task<KanbanCard> PromoteActionItemToKanbanAsync(string meetingId, string actionItemId, string projectId);
}
