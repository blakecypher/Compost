using Compost.Core.Models;

namespace Compost.Core.Interfaces;

/// <summary>
/// Manages code snippets with cross-project search capabilities
/// </summary>
public interface ICodeSnippetService
{
    /// <summary>
    /// Create a new code snippet
    /// </summary>
    Task<CodeSnippet> CreateSnippetAsync(
        string title,
        string description,
        string code,
        string language = "csharp");

    /// <summary>
    /// Update an existing snippet
    /// </summary>
    Task UpdateSnippetAsync(CodeSnippet snippet);

    /// <summary>
    /// Delete a snippet
    /// </summary>
    Task DeleteSnippetAsync(string snippetId);

    /// <summary>
    /// Get a snippet by ID
    /// </summary>
    Task<CodeSnippet?> GetSnippetByIdAsync(string snippetId);

    /// <summary>
    /// Get all snippets
    /// </summary>
    Task<List<CodeSnippet>> GetAllSnippetsAsync();

    /// <summary>
    /// Search snippets by title, description, or code content
    /// </summary>
    Task<List<CodeSnippet>> SearchSnippetsAsync(string searchQuery);

    /// <summary>
    /// Semantic search using embeddings (find similar snippets)
    /// </summary>
    Task<List<CodeSnippet>> SemanticSearchAsync(string query, int maxResults = 10);

    /// <summary>
    /// Get snippets by project
    /// </summary>
    Task<List<CodeSnippet>> GetSnippetsByProjectAsync(string projectName);

    /// <summary>
    /// Get snippets by category
    /// </summary>
    Task<List<CodeSnippet>> GetSnippetsByCategoryAsync(string category);

    /// <summary>
    /// Get snippets by tag
    /// </summary>
    Task<List<CodeSnippet>> GetSnippetsByTagAsync(string tag);

    /// <summary>
    /// Get snippets associated with an architectural pattern
    /// </summary>
    Task<List<CodeSnippet>> GetSnippetsByPatternAsync(string patternId);

    /// <summary>
    /// Link a snippet to an architectural pattern
    /// </summary>
    Task LinkSnippetToPatternAsync(string snippetId, string patternId);

    /// <summary>
    /// Increment reference count when snippet is used
    /// </summary>
    Task IncrementReferenceCountAsync(string snippetId);

    /// <summary>
    /// Get most referenced snippets
    /// </summary>
    Task<List<CodeSnippet>> GetMostReferencedSnippetsAsync(int count = 10);
}
