using Compost.Core.Models;

namespace Compost.Core.Interfaces;

/// <summary>
/// Learns architectural patterns and suggests them based on context
/// </summary>
public interface IPatternRecognitionService
{
    /// <summary>
    /// Create a new architectural pattern
    /// </summary>
    Task<ArchitecturalPattern> CreatePatternAsync(
        string name,
        string description,
        string whenToUse,
        string howItWorks);

    /// <summary>
    /// Update an existing pattern
    /// </summary>
    Task UpdatePatternAsync(ArchitecturalPattern pattern);

    /// <summary>
    /// Delete a pattern
    /// </summary>
    Task DeletePatternAsync(string patternId);

    /// <summary>
    /// Get all patterns
    /// </summary>
    Task<List<ArchitecturalPattern>> GetAllPatternsAsync();

    /// <summary>
    /// Get a pattern by ID
    /// </summary>
    Task<ArchitecturalPattern?> GetPatternByIdAsync(string patternId);

    /// <summary>
    /// Suggest patterns relevant to a mind map node based on content
    /// </summary>
    Task<List<ArchitecturalPattern>> SuggestPatternsForNodeAsync(string mindMapNodeId);

    /// <summary>
    /// Suggest patterns for a tree node based on requirements
    /// </summary>
    Task<List<ArchitecturalPattern>> SuggestPatternsForTreeNodeAsync(string treeNodeId);

    /// <summary>
    /// Learn from user's choice of pattern (reinforcement learning)
    /// </summary>
    Task RecordPatternUsageAsync(string patternId, string projectId, bool wasUseful);

    /// <summary>
    /// Search patterns by keyword or description
    /// </summary>
    Task<List<ArchitecturalPattern>> SearchPatternsAsync(string searchQuery);

    /// <summary>
    /// Get patterns by category
    /// </summary>
    Task<List<ArchitecturalPattern>> GetPatternsByCategoryAsync(string category);

    /// <summary>
    /// Add a project reference to a pattern (track where it's been used)
    /// </summary>
    Task AddProjectReferenceAsync(string patternId, ProjectReference projectRef);
}
