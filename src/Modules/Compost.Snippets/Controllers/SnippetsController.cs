using System;
using System.Linq;
using System.Threading.Tasks;
using Compost.Core.Models;
using Compost.Core.Services;
using Compost.Snippets.ViewModels;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Title.Models;
using YesSql;

namespace Compost.Snippets.Controllers;

public class SnippetsController(IContentManager contentManager, ISession session, AiIntegrationService aiService)
    : Controller
{
    public async Task<IActionResult> Index(string query)
    {
        IQuery<ContentItem> queryParams = session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "CodeSnippet" && x.Published);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var search = query.ToLower();
            // In a real scenario, you'd use a Search Index (Lucene/Elastic), but for now we'll do in-memory filtering 
            // after basic retrieval or simple DB query modifications if YesSql supports it well enough.
            // Since YesSql with standard indices is limited for full-text, we'll fetch all and filter or add more specific indices.
            // For this scale, fetching all is acceptable, or we improve the query if we had a proper index.
            
            // Note: YesSQL 'Like' support depends on provider. We'll fetch relevant ones.
            // A meaningful real-world implementation would utilize Orchard Core's Search module (Lucene).
            // Here, for simplicity in this "Clean" architecture without setting up Lucene indices yet:
            
            var allSnippets = await queryParams.ListAsync();
            var filtered = allSnippets.Where(item => 
            {
                var part = item.As<CodeSnippetPart>();
                return (item.DisplayText != null && item.DisplayText.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                    || (part?.Language != null && part.Language.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                    || (part?.Category != null && part.Category.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                    || (part?.Documentation != null && part.Documentation.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                    || (part?.Tags != null && part.Tags.Any(t => t.Contains(search, StringComparison.CurrentCultureIgnoreCase)));
            });
            
            ViewBag.SearchQuery = query;
            return View(filtered);
        }

        var snippets = await queryParams.ListAsync();
        return View(snippets);
    }

    public async Task<IActionResult> Create()
    {
        var model = new SnippetViewModel();
        await PopulatePatternsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SnippetViewModel model)
    {
        if (ModelState.IsValid)
        {
            var contentItem = await contentManager.NewAsync("CodeSnippet");
            
            contentItem.Alter<TitlePart>(part => {
                part.Title = model.Title;
            });
            contentItem.DisplayText = model.Title;
            
            contentItem.Alter<CodeSnippetPart>(part => {
                part.Code = model.Code;
                part.Language = model.Language;
                part.Category = model.Category;
                part.ProjectName = model.ProjectName;
                part.RelatedPatternId = model.RelatedPatternId;
                part.Documentation = model.Description;
                
                if (!string.IsNullOrWhiteSpace(model.Tags))
                {
                    part.Tags = model.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(t => t.Trim())
                                        .ToList();
                }
            });

            await contentManager.CreateAsync(contentItem, VersionOptions.Draft);
            await contentManager.PublishAsync(contentItem);
            
            // Update pattern side - bidirectional
            if (string.IsNullOrEmpty(model.RelatedPatternId)) return RedirectToAction(nameof(Index));
            {
                var pattern = await contentManager.GetAsync(model.RelatedPatternId, VersionOptions.DraftRequired);
                if (pattern == null) return RedirectToAction(nameof(Index));
                pattern.Alter<ArchitecturalPatternPart>("ArchitecturalPatternPart", part => {
                    if (!part.RelatedSnippetIds.Contains(contentItem.ContentItemId))
                    {
                        part.RelatedSnippetIds.Add(contentItem.ContentItemId);
                    }
                });
                await contentManager.UpdateAsync(pattern);
                await contentManager.PublishAsync(pattern);
            }

            return RedirectToAction(nameof(Index));
        }
        
        await PopulatePatternsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Detail(string id)
    {
        var contentItem = await contentManager.GetAsync(id, VersionOptions.Published);
        if (contentItem == null)
            return NotFound();
        var part = contentItem.As<CodeSnippetPart>();
        if (part == null)
            return NotFound();
        ViewBag.Title = contentItem.DisplayText ?? "Snippet";
        return View(contentItem);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var contentItem = await contentManager.GetAsync(id, VersionOptions.Latest);
        if (contentItem == null)
        {
            return NotFound();
        }

        var part = contentItem.As<CodeSnippetPart>();
        var titlePart = contentItem.As<TitlePart>();

        var model = new SnippetViewModel
        {
            Id = contentItem.ContentItemId,
            Title = titlePart?.Title ?? contentItem.DisplayText,
            Code = part?.Code,
            Language = part?.Language,
            Category = part?.Category,
            ProjectName = part?.ProjectName,
            RelatedPatternId = part?.RelatedPatternId,
            Description = part?.Documentation,
            Tags = part?.Tags != null ? string.Join(", ", part.Tags) : string.Empty
        };

        await PopulatePatternsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SnippetViewModel model)
    {
        var contentItem = await contentManager.GetAsync(model.Id, VersionOptions.DraftRequired);
        if (contentItem == null)
        {
            return NotFound();
        }
        
        // Get current pattern ID before update
        var currentPart = contentItem.As<CodeSnippetPart>();
        var oldPatternId = currentPart?.RelatedPatternId;
        
        contentItem.Alter<TitlePart>(part => {
            part.Title = model.Title;
        });
        contentItem.DisplayText = model.Title;

        contentItem.Alter<CodeSnippetPart>(part => {
            part.Code = model.Code;
            part.Language = model.Language;
            part.Category = model.Category;
            part.ProjectName = model.ProjectName;
            part.RelatedPatternId = model.RelatedPatternId;
            part.Documentation = model.Description;
            
            if (!string.IsNullOrWhiteSpace(model.Tags))
            {
                part.Tags = model.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(t => t.Trim())
                                    .ToList();
            }
            else
            {
                part.Tags = [];
            }
        });

        await contentManager.UpdateAsync(contentItem);
        await contentManager.PublishAsync(contentItem);
        
        // Handle bidirectional updates
        if (oldPatternId == model.RelatedPatternId) return RedirectToAction(nameof(Index));
        {
            // Remove from old pattern if exists
            if (!string.IsNullOrEmpty(oldPatternId))
            {
                var oldPattern = await contentManager.GetAsync(oldPatternId, VersionOptions.DraftRequired);
                if (oldPattern != null)
                {
                    oldPattern.Alter<ArchitecturalPatternPart>("ArchitecturalPatternPart", part =>
                    {
                        part.RelatedSnippetIds.Remove(contentItem.ContentItemId);
                    });
                    await contentManager.UpdateAsync(oldPattern);
                    await contentManager.PublishAsync(oldPattern);
                }
            }
            
            // Add to new pattern if exists
            if (string.IsNullOrEmpty(model.RelatedPatternId)) return RedirectToAction(nameof(Index));
            {
                var newPattern = await contentManager.GetAsync(model.RelatedPatternId, VersionOptions.DraftRequired);
                if (newPattern == null) return RedirectToAction(nameof(Index));
                newPattern.Alter<ArchitecturalPatternPart>("ArchitecturalPatternPart", part => {
                    if (!part.RelatedSnippetIds.Contains(contentItem.ContentItemId))
                    {
                        part.RelatedSnippetIds.Add(contentItem.ContentItemId);
                    }
                });
                await contentManager.UpdateAsync(newPattern);
                await contentManager.PublishAsync(newPattern);
            }
        }

        return RedirectToAction(nameof(Index));
    }
    
    public async Task<IActionResult> Delete(string id)
    {
        var contentItem = await contentManager.GetAsync(id, VersionOptions.Latest);
        if (contentItem != null)
        {
            await contentManager.RemoveAsync(contentItem);
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>POST /Snippets/RecognizePatterns - AI pattern recognition for code</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RecognizePatterns(string id)
    {
        var contentItem = await contentManager.GetAsync(id);
        if (contentItem == null)
        {
            return NotFound();
        }

        var part = contentItem.As<CodeSnippetPart>();
        if (part?.Code == null)
        {
            return BadRequest("No code found in snippet");
        }

        try
        {
            var patterns = await aiService.RecognizePatternsAsync(part.Code, part.Language);
            
            // Store detected patterns in session or return as JSON
            return Json(new { 
                success = true, 
                patterns = patterns.Select(p => new {
                    name = p.Name,
                    description = p.WhenToUse,
                    confidence = p.SuccessScore
                })
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>POST /Snippets/GenerateSuggestion - AI code suggestion</summary>
    [HttpPost]
    public async Task<IActionResult> GenerateSuggestion(string requirement, string language)
    {
        if (string.IsNullOrEmpty(requirement))
        {
            return BadRequest("Requirement is required");
        }

        try
        {
            var suggestion = await aiService.GenerateCodeSuggestionAsync(requirement, language ?? "csharp");
            return Json(new { success = true, suggestion });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>POST /Snippets/AnalyzeCode - Analyze code for patterns without saving</summary>
    [HttpPost]
    public async Task<IActionResult> AnalyzeCode([FromBody] AnalyzeCodeRequest request)
    {
        if (string.IsNullOrEmpty(request?.Code))
        {
            return Json(new { success = false, error = "Code is required" });
        }

        try
        {
            var patterns = await aiService.RecognizePatternsAsync(request.Code, request.Language ?? "csharp");
            
            return Json(new { 
                success = true, 
                patterns = patterns.Select(p => new {
                    name = p.Name,
                    description = p.WhenToUse,
                    confidence = p.SuccessScore
                })
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    private async Task PopulatePatternsAsync(SnippetViewModel model)
    {
        var patterns = await session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "ArchitecturalPattern" && x.Published).ListAsync();
        model.AvailablePatterns = patterns.ToDictionary(p => p.ContentItemId, p => p.DisplayText ?? "Untitled Pattern");
    }
}

public class AnalyzeCodeRequest
{
    public string Code { get; set; }
    public string Language { get; set; }
}

