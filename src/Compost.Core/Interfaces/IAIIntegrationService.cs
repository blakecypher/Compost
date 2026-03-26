using Compost.Core.Models;

namespace Compost.Core.Interfaces;

public interface IAIIntegrationService
{
    /// <summary>
    /// Estimates story points for a requirement using AI
    /// </summary>
    Task<int> EstimateStoryPointsAsync(string requirement, string? context = null);
    
    /// <summary>
    /// Recognizes architectural patterns in code using AI
    /// </summary>
    Task<List<ArchitecturalPattern>> RecognizePatternsAsync(string code, string language);
    
    /// <summary>
    /// Generates code suggestions based on requirements using AI
    /// </summary>
    Task<string> GenerateCodeSuggestionAsync(string requirement, string language, string? context = null);
    
    /// <summary>
    /// Extracts action items from text using AI
    /// </summary>
    Task<List<ActionItem>> ExtractActionItemsFromTextAsync(string text, string? context = null);

    /// <summary>
    /// Extracts mind map nodes (concepts, ideas, decisions) from text using AI
    /// </summary>
    Task<List<MindMapNode>> ExtractMindMapNodesFromTextAsync(string text, string? context = null);
}
