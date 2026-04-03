using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compost.Core.Models;
using Compost.Patterns.ViewModels;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Title.Models;
using YesSql;

namespace Compost.Patterns.Controllers;

public class PatternsController(IContentManager contentManager, ISession session) : Controller
{
    private static readonly char[] Separator = ['\n', '\r'];

    public async Task<IActionResult> Index(string query)
    {
        IQuery<ContentItem> queryParams = session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "ArchitecturalPattern" && x.Published);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var search = query.ToLower();
            var allPatterns = await queryParams.ListAsync();
            var filtered = allPatterns.Where(item => 
            {
                var part = item.As<ArchitecturalPatternPart>();
                return (item.DisplayText != null && item.DisplayText.ToLower().Contains(search))
                    || (part?.WhenToUse != null && part.WhenToUse.ToLower().Contains(search))
                    || (part?.HowItWorks != null && part.HowItWorks.ToLower().Contains(search))
                    || (part?.Keywords != null && part.Keywords.Any(k => k.ToLower().Contains(search)));
            });
            
            ViewBag.SearchQuery = query;
            return View(filtered);
        }

        var patterns = await queryParams.ListAsync();
        return View(patterns);
    }

    public IActionResult Create()
    {
        return View(new PatternViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PatternViewModel model)
    {
        if (ModelState.IsValid)
        {
            var contentItem = await contentManager.NewAsync("ArchitecturalPattern");
            
            contentItem.Alter<TitlePart>(part => {
                part.Title = model.Title;
            });
            contentItem.DisplayText = model.Title;
            
            contentItem.Alter<ArchitecturalPatternPart>(part => {
                part.WhenToUse = model.WhenToUse;
                part.HowItWorks = model.HowItWorks;
                part.Gotchas = model.Gotchas;
                part.ResourceUrls = ParseResourceUrls(model.ResourceUrlsText);
                if (!string.IsNullOrWhiteSpace(model.Keywords))
                {
                    part.Keywords = model.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(t => t.Trim())
                                        .ToList();
                }
            });

            await contentManager.CreateAsync(contentItem, VersionOptions.Draft);
            await contentManager.PublishAsync(contentItem);
            
            return RedirectToAction(nameof(Index));
        }
        
        return View(model);
    }

    public async Task<IActionResult> Detail(string id)
    {
        var contentItem = await contentManager.GetAsync(id, VersionOptions.Published);
        if (contentItem == null)
        {
            return NotFound();
        }

        var part = contentItem.As<ArchitecturalPatternPart>();
        var titlePart = contentItem.As<TitlePart>();

        var model = new PatternViewModel
        {
            Id = contentItem.ContentItemId,
            Title = titlePart?.Title ?? contentItem.DisplayText,
            WhenToUse = part?.WhenToUse,
            HowItWorks = part?.HowItWorks,
            Gotchas = part?.Gotchas,
            Keywords = part?.Keywords != null ? string.Join(", ", part.Keywords) : string.Empty,
            ResourceUrls = part?.ResourceUrls ?? []
        };

        // Find linked snippets (with id for edit link)
        var snippets = await session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "CodeSnippet" && x.Published).ListAsync();
        foreach (var snippetItem in snippets)
        {
            var snippetPart = snippetItem.As<CodeSnippetPart>();
            var relatedPatternId = snippetPart?.RelatedPatternId;
            if (snippetPart == null || relatedPatternId != id) continue;
            var code = snippetPart.Code;
            var language = snippetPart.Language;
            var preview = code.Length > 200 ? code[..200] + "..." : code;
            var item = new LinkedSnippetItem
            {
                ContentItemId = snippetItem.ContentItemId,
                Title = snippetItem.DisplayText ?? "Snippet",
                Language = language,
                CodePreview = preview
            };
            model.LinkedSnippets.Add(item);
        }

        return View(model);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var contentItem = await contentManager.GetAsync(id, VersionOptions.Latest);
        if (contentItem == null)
        {
            return NotFound();
        }

        var part = contentItem.As<ArchitecturalPatternPart>();
        var titlePart = contentItem.As<TitlePart>();

        var model = new PatternViewModel
        {
            Id = contentItem.ContentItemId,
            Title = titlePart?.Title ?? contentItem.DisplayText,
            WhenToUse = part?.WhenToUse,
            HowItWorks = part?.HowItWorks,
            Gotchas = part?.Gotchas,
            Keywords = part?.Keywords != null ? string.Join(", ", part.Keywords) : string.Empty,
            ResourceUrlsText = part?.ResourceUrls != null ? string.Join("\n", part.ResourceUrls) : ""
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PatternViewModel model)
    {
        var contentItem = await contentManager.GetAsync(model.Id, VersionOptions.DraftRequired);
        if (contentItem == null)
        {
            return NotFound();
        }
        
        contentItem.Alter<TitlePart>(part => {
            part.Title = model.Title;
        });
        contentItem.DisplayText = model.Title;

        contentItem.Alter<ArchitecturalPatternPart>(part => {
            part.WhenToUse = model.WhenToUse;
            part.HowItWorks = model.HowItWorks;
            part.Gotchas = model.Gotchas;
            part.ResourceUrls = ParseResourceUrls(model.ResourceUrlsText);
            if (!string.IsNullOrWhiteSpace(model.Keywords))
            {
                part.Keywords = model.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(t => t.Trim())
                                    .ToList();
            }
            else
            {
                part.Keywords = [];
            }
        });

        await contentManager.UpdateAsync(contentItem);
        await contentManager.PublishAsync(contentItem);

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
    
    /// <summary>GET /Patterns/CheckPatternExists - Check if pattern exists by name</summary>
    [HttpGet]
    public async Task<IActionResult> CheckPatternExists(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return Json(new { exists = false });
        }
        
        var patterns = await session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "ArchitecturalPattern" && x.Published).ListAsync();
        var existingPattern = patterns.FirstOrDefault(p => p.DisplayText?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
        
        return Json(new { 
            exists = existingPattern != null,
            patternId = existingPattern?.ContentItemId
        });
    }
    
    /// <summary>POST /Patterns/AssociatePattern - Associate pattern with snippet</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AssociatePattern([FromBody] AssociatePatternRequest request)
    {
        if (string.IsNullOrEmpty(request?.PatternId) || string.IsNullOrEmpty(request.SnippetId))
        {
            return Json(new { success = false, error = "Pattern ID and Snippet ID are required" });
        }
        
        try
        {
            // Get both snippet and pattern
            var snippet = await contentManager.GetAsync(request.SnippetId, VersionOptions.DraftRequired);
            var pattern = await contentManager.GetAsync(request.PatternId, VersionOptions.DraftRequired);
            
            if (snippet == null)
            {
                return Json(new { success = false, error = "Snippet not found" });
            }
            
            if (pattern == null)
            {
                return Json(new { success = false, error = "Pattern not found" });
            }
            
            // Update snippet side (RelatedPatternId)
            snippet.Alter<CodeSnippetPart>("CodeSnippetPart", part => {
                part.RelatedPatternId = request.PatternId;
            });
            
            await contentManager.UpdateAsync(snippet);
            await contentManager.PublishAsync(snippet);
            
            // Update pattern side (RelatedSnippetIds) - bidirectional
            pattern.Alter<ArchitecturalPatternPart>(part => {
                if (!part.RelatedSnippetIds.Contains(request.SnippetId))
                {
                    part.RelatedSnippetIds.Add(request.SnippetId);
                }
            });
            
            await contentManager.UpdateAsync(pattern);
            await contentManager.PublishAsync(pattern);
            
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
    
    /// <summary>POST /Patterns/CreateFromSnippet - Create pattern from snippet analysis</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreateFromSnippet([FromBody] CreatePatternFromSnippetRequest request)
    {
        if (string.IsNullOrEmpty(request?.Name) || string.IsNullOrEmpty(request.SnippetId))
        {
            return Json(new { success = false, error = "Pattern name and snippet ID are required" });
        }
        
        try
        {
            // Create new pattern
            var contentItem = await contentManager.NewAsync("ArchitecturalPattern");
            
            contentItem.Alter<TitlePart>(part => {
                part.Title = request.Name;
            });
            contentItem.DisplayText = request.Name;
            
            contentItem.Alter<ArchitecturalPatternPart>(part => {
                part.WhenToUse = request.WhenToUse ?? request.Description;
                part.HowItWorks = request.Description;
                
                // Store code example in Gotchas field temporarily (or we could add it to HowItWorks)
                if (!string.IsNullOrEmpty(request.CodeExample))
                {
                    part.Gotchas = $"Code Example:\n{request.CodeExample}";
                }
                
                // Store category in Keywords as first item
                if (!string.IsNullOrEmpty(request.Category))
                {
                    part.Keywords = [request.Category];
                }
                
                if (!string.IsNullOrWhiteSpace(request.Tags))
                {
                    var tagList = request.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(t => t.Trim())
                                            .ToList();
                    
                    // Add category to keywords if not already there
                    if (!string.IsNullOrEmpty(request.Category) && !tagList.Contains(request.Category))
                    {
                        tagList.Insert(0, request.Category);
                    }
                    
                    // Add tags to keywords
                    part.Keywords.AddRange(tagList.Where(tag => !part.Keywords.Contains(tag)));
                }
            });
            
            await contentManager.CreateAsync(contentItem, VersionOptions.Draft);
            await contentManager.PublishAsync(contentItem);
            
            // Associate with snippet
            var snippet = await contentManager.GetAsync(request.SnippetId, VersionOptions.DraftRequired);
            if (snippet != null)
            {
                snippet.Alter<CodeSnippetPart>("CodeSnippetPart", part => {
                    part.RelatedPatternId = contentItem.ContentItemId;
                });
                
                await contentManager.UpdateAsync(snippet);
                await contentManager.PublishAsync(snippet);
                
                // Update pattern side - bidirectional
                contentItem.Alter<ArchitecturalPatternPart>(part => {
                    if (!part.RelatedSnippetIds.Contains(request.SnippetId))
                    {
                        part.RelatedSnippetIds.Add(request.SnippetId);
                    }
                });
                await contentManager.UpdateAsync(contentItem);
                await contentManager.PublishAsync(contentItem);
            }
            
            return Json(new { 
                success = true, 
                patternId = contentItem.ContentItemId,
                navigateToPattern = true
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    private static List<string> ParseResourceUrls(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        return text.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim())
            .Where(u => u.Length > 0)
            .ToList();
    }
}

public class AssociatePatternRequest
{
    public string PatternId { get; set; }
    public string SnippetId { get; set; }
}

public class CreatePatternFromSnippetRequest
{
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public string WhenToUse { get; set; }
    public string CodeExample { get; set; }
    public string Tags { get; set; }
    public string SnippetId { get; set; }
}
