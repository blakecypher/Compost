using System.Threading.Tasks;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Compost.Kanban.Models;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
// Add this using

namespace Compost.Kanban.Controllers;

public class RefinementFormData
{
    public string id { get; set; }
    public string message { get; set; }
}

public class RefinementController(
    IContentManager contentManager,
    IDecompositionEngine decompositionEngine,
    IAiIntegrationService aiService) : Controller
{
    public async Task<IActionResult> Index(string id)
    {
        var contentItem = await contentManager.GetAsync(id);
        if (contentItem == null) return NotFound();

        var part = contentItem.As<TreeNodePart>();
        if (part == null) return BadRequest("Content item is not a Tree Node.");

        ViewBag.Title = contentItem.DisplayText ?? "Refinement";
        return View(contentItem);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AddMessage([FromForm] RefinementFormData formData)
    {
        // Store user message
        await decompositionEngine.AddRefinementMessageAsync(formData.id, MessageRole.User, formData.message);
        
        // Get the tree node for context
        var contentItem = await contentManager.GetAsync(formData.id);
        var part = contentItem?.As<TreeNodePart>();
        var context = part?.WorkContextId ?? "default";
        
        // Build a refinement prompt using the user's message and tree node context
        var refinementPrompt = BuildRefinementPrompt(formData.message, contentItem?.DisplayText, part);
        
        // Call Gemini AI for real response
        var aiResponse = await aiService.GenerateCodeSuggestionAsync(refinementPrompt, "markdown", context);
        
        // Store AI response
        await decompositionEngine.AddRefinementMessageAsync(formData.id, MessageRole.Assistant, aiResponse);

        return Json(new { response = aiResponse });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Promote(string id)
    {
        var cards = await decompositionEngine.PromoteTreeToKanbanAsync(id);
        return Json(new { success = true, count = cards.Count });
    }

    private string BuildRefinementPrompt(string userMessage, string? requirementTitle, TreeNodePart? part)
    {
        var context = "";
        if (part != null)
        {
            context += $"\nRequirement Title: {requirementTitle}\n";
            context += $"Complexity: {part.Complexity}\n";
            context += $"Priority: {part.Priority}\n";
            if (part.AcceptanceCriteria?.Count > 0)
            {
                context += "Existing Acceptance Criteria:\n";
                foreach (var criteria in part.AcceptanceCriteria)
                {
                    context += $"- {criteria}\n";
                }
            }
        }

        return $"""
            You are an expert software architect helping refine software requirements. 
            
            The user is asking about this requirement:{context}
            
            User's question/comment: {userMessage}
            
            Provide a helpful, actionable response that:
            1. Addresses their specific question or concern
            2. Suggests relevant acceptance criteria if they haven't been defined yet
            3. Identifies potential technical requirements or implementation details
            4. Asks clarifying questions if needed
            
            Keep your response concise and focused on helping them refine this requirement into actionable development tasks.
            """;
    }
}
