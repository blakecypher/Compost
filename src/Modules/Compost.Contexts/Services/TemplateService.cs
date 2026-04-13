using Compost.Contexts.Models;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using YesSql;

namespace Compost.Contexts.Services;

/// <summary>
/// Implementation of template service using Orchard Core content management
/// </summary>
public class TemplateService(
    IContentManager contentManager,
    ISession session,
    IProjectManager projectManager,
    ILogger<TemplateService> logger)
    : ITemplateService
{
    private readonly ILogger<TemplateService> _logger = logger;

    public async Task<List<ContextTemplate>> GetAllTemplatesAsync()
    {
        var templates = new List<ContextTemplate>();
        
        // Get built-in templates first
        templates.AddRange(GetBuiltInTemplates());
        
        // Get custom templates from database
        var customTemplates = await session.Query<ContentItem, ContentItemIndex>()
            .Where(ci => ci.ContentType == nameof(ContextTemplate) && ci.Published)
            .ListAsync();
            
        templates.AddRange(customTemplates.Select(MapToContextTemplate));
        
        return templates;
    }

    public async Task<ContextTemplate?> GetTemplateByIdAsync(string templateId)
    {
        // Check built-in templates first
        var builtIn = GetBuiltInTemplates().FirstOrDefault(t => t.Id == templateId);
        if (builtIn != null) return builtIn;
        
        // Check database
        var contentItem = await contentManager.GetAsync(templateId, VersionOptions.Published);
        if (contentItem?.ContentType != nameof(ContextTemplate)) return null;
        
        return MapToContextTemplate(contentItem);
    }

    public async Task<ContextTemplate> CreateTemplateFromContextAsync(
        string projectId, string templateName, string? description = null)
    {
        var context = await projectManager.GetProjectByIdAsync(projectId);
        if (context == null) throw new InvalidOperationException("Project not found");
        
        var contentItem = await contentManager.NewAsync(nameof(ContextTemplate));
        contentItem.DisplayText = templateName;
        
        var part = contentItem.As<ProjectTemplatePart>();
        if (part != null)
        {
            part.TemplateName = templateName;
            part.Description = description ?? context.Description;
            part.DefaultRepositoryName = context.RepositoryName;
            part.DefaultRepositoryUrl = context.RepositoryUrl;
            part.DefaultBranch = context.CurrentBranch;
            part.DefaultTags = context.Tags?.ToList() ?? [];
            part.TestingSteps = context.TestingSteps?.ToList() ?? [];
            part.Notes = context.Notes;
            part.IsBuiltIn = false;
            
            contentItem.Apply(nameof(ProjectTemplatePart), part);
        }
        
        await contentManager.CreateAsync(contentItem);
        await contentManager.PublishAsync(contentItem);
        
        return MapToContextTemplate(contentItem);
    }

    public async Task<ContextTemplate> CreateTemplateAsync(
        string name, string? description, string? repositoryName,
        string? repositoryUrl, string? branch, List<string>? tags, 
        List<string>? testingSteps, string? category)
    {
        var contentItem = await contentManager.NewAsync(nameof(ContextTemplate));
        contentItem.DisplayText = name;
        
        var part = contentItem.As<ProjectTemplatePart>();
        if (part != null)
        {
            part.TemplateName = name;
            part.Description = description;
            part.DefaultRepositoryName = repositoryName;
            part.DefaultRepositoryUrl = repositoryUrl;
            part.DefaultBranch = branch;
            part.DefaultTags = tags ?? [];
            part.TestingSteps = testingSteps ?? [];
            part.Category = category;
            part.IsBuiltIn = false;
            
            contentItem.Apply(nameof(ProjectTemplatePart), part);
        }
        
        await contentManager.CreateAsync(contentItem);
        await contentManager.PublishAsync(contentItem);
        
        return MapToContextTemplate(contentItem);
    }

    public async Task DeleteTemplateAsync(string templateId)
    {
        var template = await GetTemplateByIdAsync(templateId);
        if (template == null) return;
        
        if (template.IsBuiltIn)
            throw new InvalidOperationException("Cannot delete built-in templates");
            
        await contentManager.RemoveAsync(await contentManager.GetAsync(templateId, VersionOptions.Latest));
    }

    public async Task<Project> CreateContextFromTemplateAsync(string templateId, string contextName)
    {
        var template = await GetTemplateByIdAsync(templateId);
        if (template == null) throw new InvalidOperationException("Template not found");
        
        return await projectManager.CreateProjectAsync(
            contextName,
            template.Description,
            template.DefaultRepositoryName,
            template.DefaultRepositoryUrl,
            template.DefaultBranch,
            template.DefaultTags
        );
    }

    public async Task<List<ContextTemplate>> GetTemplatesByCategoryAsync(string category)
    {
        var allTemplates = await GetAllTemplatesAsync();
        return allTemplates.Where(t => t.Category == category).ToList();
    }

    public async Task<List<string>> GetTemplateCategoriesAsync()
    {
        var allTemplates = await GetAllTemplatesAsync();
        return allTemplates
            .Where(t => !string.IsNullOrEmpty(t.Category))
            .Select(t => t.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    private static List<ContextTemplate> GetBuiltInTemplates()
    {
        var template = new ContextTemplate
        {
            Id = "builtin-web-dev",
            TemplateName = "Web Development",
            Description = "Standard web development project template",
            DefaultTags = ["web", "frontend", "backend"],
            TestingSteps = ["Run unit tests", "Check browser console", "Test responsive design"],
            Category = "Development",
            IsBuiltIn = true,
            CreatedAt = DateTime.UtcNow
        };
        return
        [
            template,

            new ContextTemplate
            {
                Id = "builtin-api-dev",
                TemplateName = "API Development",
                Description = "API and backend service development",
                DefaultTags = ["api", "backend", "service"],
                TestingSteps = ["Run integration tests", "Test endpoints", "Check logs"],
                Category = "Development",
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow
            },

            new ContextTemplate
            {
                Id = "builtin-bugfix",
                TemplateName = "Bug Fix",
                Description = "Template for investigating and fixing bugs",
                DefaultTags = ["bug", "fix", "investigation"],
                TestingSteps = ["Reproduce bug", "Identify root cause", "Apply fix", "Verify fix"],
                Category = "Maintenance",
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow
            }
        ];
    }

    private ContextTemplate MapToContextTemplate(ContentItem contentItem)
    {
        var part = contentItem.As<ProjectTemplatePart>();

        var template = new ContextTemplate
        {
            Id = contentItem.ContentItemId,
            TemplateName = part?.TemplateName ?? contentItem.DisplayText ?? "Unnamed Template",
            Description = part?.Description,
            DefaultRepositoryName = part?.DefaultRepositoryName,
            DefaultRepositoryUrl = part?.DefaultRepositoryUrl,
            DefaultBranch = part?.DefaultBranch,
            DefaultTags = part?.DefaultTags ?? [],
            TestingSteps = part?.TestingSteps ?? [],
            Notes = part?.Notes,
            Category = part?.Category,
            IsBuiltIn = part?.IsBuiltIn ?? false,
            CreatedAt = contentItem.CreatedUtc ?? DateTime.UtcNow
        };
        return template;
    }
}
