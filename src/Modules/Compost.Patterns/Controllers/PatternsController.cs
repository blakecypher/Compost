using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compost.Patterns.Models;
using Compost.Patterns.ViewModels;
using Compost.Snippets.Models;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Title.Models;
using YesSql;

namespace Compost.Patterns.Controllers;

public class PatternsController(IContentManager contentManager, ISession session) : Controller
{
    private static readonly char[] separator = ['\n', '\r'];

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

    public async Task<IActionResult> Create()
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
            if (snippetPart != null && snippetPart.RelatedPatternId == id)
            {
                var preview = snippetPart.Code?.Length > 200 ? snippetPart.Code[..200] + "..." : snippetPart.Code ?? "";
                var item = new LinkedSnippetItem
                {
                    ContentItemId = snippetItem.ContentItemId,
                    Title = snippetItem.DisplayText ?? "Snippet",
                    Language = snippetPart.Language ?? "text",
                    CodePreview = preview
                };
                model.LinkedSnippets.Add(item);
            }
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

    private static List<string> ParseResourceUrls(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        return text.Split(separator, StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim())
            .Where(u => u.Length > 0)
            .ToList();
    }
}
