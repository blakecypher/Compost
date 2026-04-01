using Compost.Contexts.Models;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using YesSql;
using YesSql.Indexes;
using OpenQuestion = Compost.Core.Models.OpenQuestion;

namespace Compost.Contexts.Services;

/// <summary>
/// Implementation of IProjectManager using Orchard Core's content management
/// </summary>
public class ProjectManager(IContentManager contentManager, ISession session, ILogger<ProjectManager> logger) : IProjectManager
{
    public async Task<List<Project>> GetAllProjectsAsync()
    {
        var contentItems = await session.Query<ContentItem, ContentItemIndex>()
            .Where(ci => ci.ContentType == nameof(Project) && ci.Latest && ci.Published)
            .OrderByDescending(ci => ci.ModifiedUtc)
            .ListAsync();

        return contentItems.Select(MapToWorkProject).ToList();
    }

    public async Task<Project?> GetActiveProjectAsync()
    {
        var contentItems = await session.Query<ContentItem, WorkProjectPartIndex>()
            .Where(index => index.IsActive)
            .ListAsync();

        var activeItem = contentItems.FirstOrDefault();
        return activeItem != null ? MapToWorkProject(activeItem) : null;
    }

    public async Task<Project?> GetProjectByIdAsync(string projectId)
    {
        var contentItem = await contentManager.GetAsync(projectId, VersionOptions.Latest);
        if (contentItem == null) return null;
        
        // Ensure ProjectPart is loaded
        var part = contentItem.As<ProjectPart>();
        if (part == null)
        {
            // Attach ProjectPart if it doesn't exist (for legacy content)
            contentItem.Weld(new ProjectPart());
            part = contentItem.As<ProjectPart>();
            Console.WriteLine($"[DEBUG] GetProjectById - Attached ProjectPart to {projectId}");
        }
        
        Console.WriteLine($"[DEBUG] GetProjectById - Part loaded: {part != null}, RepoName='{part?.RepositoryName}'");
        
        return MapToWorkProject(contentItem);
    }

    public async Task<Project> CreateProjectAsync(string name, string? description = null, string? repositoryName = null, string? repositoryUrl = null, string? currentBranch = null, List<string>? tags = null, string status = "To Do", string? parentProjectId = null, int displayOrder = 0)
    {
        logger.LogInformation("=== CreateProjectAsync START ===");
        logger.LogInformation("Creating context: Name={Name}, RepoName={RepoName}, RepoUrl={RepoUrl}, Branch={Branch}, ParentId={ParentId}, DisplayOrder={DisplayOrder}, Tags=[{Tags}]", 
            name, repositoryName, repositoryUrl, currentBranch, parentProjectId, displayOrder, string.Join(", ", tags ??
                []));
        
        var contentItem = await contentManager.NewAsync(nameof(Project));
        contentItem.DisplayText = name;
        
        var part = contentItem.As<ProjectPart>();
        if (part != null)
        {
            logger.LogInformation("ProjectPart found, setting values");
            part.Notes = description;
            part.RepositoryName = repositoryName;
            part.RepositoryUrl = repositoryUrl;
            part.CurrentBranch = currentBranch;
            part.Tags = tags ?? [];
            part.Description = description;
            part.ParentProjectId = parentProjectId;
            part.DisplayOrder = displayOrder;
            part.Status = status;
            
            // CRITICAL: Apply the part to ensure it's serialized to Content
            contentItem.Apply(nameof(ProjectPart), part);
            
            logger.LogInformation("Part values set: RepoName='{RepoName}', RepoUrl='{RepoUrl}', Branch='{Branch}', ParentId='{ParentId}', DisplayOrder={DisplayOrder}, Tags=[{Tags}]",
                part.RepositoryName, part.RepositoryUrl, part.CurrentBranch, part.ParentProjectId, part.DisplayOrder, string.Join(", ", part.Tags));
        }
        else
        {
            logger.LogError("ProjectPart is NULL for new content item!");
        }

        logger.LogInformation("Calling contentManager.CreateAsync...");
        await contentManager.CreateAsync(contentItem);
        logger.LogInformation("CreateAsync completed");
        
        logger.LogInformation("Calling contentManager.PublishAsync...");
        await contentManager.PublishAsync(contentItem);
        logger.LogInformation("PublishAsync completed");
        
        logger.LogInformation("=== CreateProjectAsync END ===");

        return MapToWorkProject(contentItem);
    }

    public async Task SwitchProjectAsync(string projectId)
    {
        // Deactivate current active context
        var currentActive = await GetActiveProjectAsync();
        if (currentActive != null)
        {
            var currentItem = await contentManager.GetAsync(currentActive.Id);
            var currentPart = currentItem?.As<ProjectPart>();
            if (currentPart != null)
            {
                currentPart.IsActive = false;
                // CRITICAL: Apply the part to ensure it's serialized to Content
                currentItem.Apply(nameof(ProjectPart), currentPart);
                await contentManager.UpdateAsync(currentItem);
            }
        }

        // Activate new context
        var newItem = await contentManager.GetAsync(projectId);
        var newPart = newItem?.As<ProjectPart>();
        if (newPart != null)
        {
            newPart.IsActive = true;
            newPart.CurrentSessionStartedAt = DateTime.UtcNow;
            // CRITICAL: Apply the part to ensure it's serialized to Content
            newItem.Apply(nameof(ProjectPart), newPart);
            await contentManager.UpdateAsync(newItem);
        }
    }

    public async Task UpdateProjectAsync(Project context)
    {
        logger.LogInformation("=== UpdateProjectAsync START ===");
        logger.LogInformation("Project ID: {ProjectId}", context.Id);
        logger.LogInformation("Name: {Name}", context.Name);
        logger.LogInformation("RepositoryName: {RepoName}", context.RepositoryName);
        logger.LogInformation("RepositoryUrl: {RepoUrl}", context.RepositoryUrl);
        logger.LogInformation("CurrentBranch: {Branch}", context.CurrentBranch);
        logger.LogInformation("Tags: [{Tags}]", string.Join(", ", context.Tags ?? []));
        logger.LogInformation("Description: {Description}", context.Description);
        
        var contentItem = await contentManager.GetAsync(context.Id, VersionOptions.Latest);
        if (contentItem == null)
        {
            logger.LogError("ContentItem not found for ID: {ProjectId}", context.Id);
            return;
        }
        
        logger.LogInformation("ContentItem found: {ContentItemId}, ContentType: {ContentType}", 
            contentItem.ContentItemId, contentItem.ContentType);
        
        contentItem.DisplayText = context.Name;
        
        var part = contentItem.As<ProjectPart>();
        if (part == null)
        {
            logger.LogError("ProjectPart is NULL for content item {ContentItemId}", contentItem.ContentItemId);
            
            // Try to weld the part
            contentItem.Weld(new ProjectPart());
            part = contentItem.As<ProjectPart>();
            logger.LogInformation("After welding - ProjectPart is {NotNull}", part != null ? "NOT NULL" : "STILL NULL");
        }
        
        if (part != null)
        {
            logger.LogInformation("BEFORE UPDATE - Part values:");
            logger.LogInformation("  RepositoryName: '{RepoName}'", part.RepositoryName);
            logger.LogInformation("  RepositoryUrl: '{RepoUrl}'", part.RepositoryUrl);
            logger.LogInformation("  CurrentBranch: '{Branch}'", part.CurrentBranch);
            logger.LogInformation("  Tags: [{Tags}]", string.Join(", ", part.Tags));
            logger.LogInformation("  Description: '{Description}'", part.Description);
            
            // Update all fields
            part.RepositoryName = context.RepositoryName;
            part.RepositoryUrl = context.RepositoryUrl;
            part.CurrentBranch = context.CurrentBranch;
            part.Description = context.Description;
            part.Tags = context.Tags ?? [];
            part.ParentProjectId = context.ParentProjectId;
            part.DisplayOrder = context.DisplayOrder;
            part.TestingSteps = context.TestingSteps;
            part.IsActive = context.IsActive;
            part.Status = context.Status;
            if (context.Notes != null)
                part.Notes = context.Notes;
            
            // CRITICAL: Apply the part to ensure it's serialized to Content
            contentItem.Apply(nameof(ProjectPart), part);
            
            logger.LogInformation("AFTER UPDATE - Part values:");
            logger.LogInformation("  RepositoryName: '{RepoName}'", part.RepositoryName);
            logger.LogInformation("  RepositoryUrl: '{RepoUrl}'", part.RepositoryUrl);
            logger.LogInformation("  CurrentBranch: '{Branch}'", part.CurrentBranch);
            logger.LogInformation("  Tags: [{Tags}]", string.Join(", ", part.Tags));
            
            // Debug: Check Content dictionary
            if (contentItem.Content != null)
            {
                var containsKey = contentItem.Content.ContainsKey(nameof(ProjectPart));
                logger.LogInformation($"Content dictionary ProjectPart: {containsKey}");
            }
        }
        else
        {
            logger.LogError("ProjectPart still null after welding for {ContentItemId}", contentItem.ContentItemId);
            return;
        }

        logger.LogInformation("Calling contentManager.UpdateAsync...");
        await contentManager.UpdateAsync(contentItem);
        logger.LogInformation("UpdateAsync completed successfully");
        
        logger.LogInformation("Calling contentManager.PublishAsync...");
        await contentManager.PublishAsync(contentItem);
        logger.LogInformation("PublishAsync completed successfully");
        
        logger.LogInformation("=== UpdateProjectAsync END ===");
    }

    public async Task DeleteProjectAsync(string projectId)
    {
        logger.LogInformation("Attempting to delete context with ID: {ProjectId}", projectId);
        
        var contentItem = await contentManager.GetAsync(projectId, VersionOptions.Latest);
        if (contentItem != null)
        {
            logger.LogInformation("Found content item '{ContentItemId}' with display text '{DisplayText}', removing...", 
                contentItem.ContentItemId, contentItem.DisplayText);
            await contentManager.RemoveAsync(contentItem);
            logger.LogInformation("Successfully removed content item '{ContentItemId}'", contentItem.ContentItemId);
        }
        else
        {
            logger.LogWarning("Content item with ID {ProjectId} not found for deletion", projectId);
        }
    }

    public async Task StartSessionAsync(string projectId)
    {
        var contentItem = await contentManager.GetAsync(projectId, VersionOptions.Latest);
        var part = contentItem?.As<ProjectPart>();
        if (part != null)
        {
            part.CurrentSessionStartedAt = DateTime.UtcNow;
            // CRITICAL: Apply the part to ensure it's serialized to Content
            contentItem.Apply(nameof(ProjectPart), part);
            await contentManager.UpdateAsync(contentItem);
        }
    }

    public async Task EndSessionAsync(string projectId)
    {
        var contentItem = await contentManager.GetAsync(projectId, VersionOptions.Latest);
        var part = contentItem?.As<ProjectPart>();
        if (part is { CurrentSessionStartedAt: not null })
        {
            var duration = DateTime.UtcNow - part.CurrentSessionStartedAt.Value;
            part.TotalTimeSpentSeconds += (long)duration.TotalSeconds;
            part.CurrentSessionStartedAt = null;
            // CRITICAL: Apply the part to ensure it's serialized to Content
            contentItem.Apply(nameof(ProjectPart), part);
            await contentManager.UpdateAsync(contentItem);
        }
    }

    public async Task<Dictionary<string, TimeSpan>> GetTimeSpentByProjectAsync()
    {
        var contexts = await GetAllProjectsAsync();
        return contexts.ToDictionary(
            c => c.Name,
            c => TimeSpan.FromSeconds(c.TotalTimeSpentSeconds)
        );
    }

    public async Task AddTestingStepAsync(string projectId, string step)
    {
        var contentItem = await contentManager.GetAsync(projectId, VersionOptions.Latest);
        var part = contentItem?.As<ProjectPart>();
        if (part != null)
        {
            part.TestingSteps.Add(step);
            await contentManager.UpdateAsync(contentItem);
        }
    }

    public async Task AddOpenQuestionAsync(string projectId, string question)
    {
        var contentItem = await contentManager.GetAsync(projectId, VersionOptions.Latest);
        var part = contentItem?.As<ProjectPart>();
        if (part != null)
        {
            var item = new OpenQuestion();
            item.Question = question;
            item.CreatedAt = DateTime.UtcNow;
            part.OpenQuestions.Add(item);
            await contentManager.UpdateAsync(contentItem);
        }
    }

    public async Task ResolveQuestionAsync(string projectId, string questionId, string answer)
    {
        var contentItem = await contentManager.GetAsync(projectId, VersionOptions.Latest);
        var part = contentItem?.As<ProjectPart>();
        var question = part?.OpenQuestions.FirstOrDefault(q => q.Id == questionId);
        if (question != null)
        {
            question.Answer = answer;
            question.IsResolved = true;
            question.ResolvedAt = DateTime.UtcNow;
            await contentManager.UpdateAsync(contentItem);
        }
    }

    private Project MapToWorkProject(ContentItem contentItem)
    {
        var part = contentItem.As<ProjectPart>();

        var context = new Project
        {
            Id = contentItem.ContentItemId,
            Name = contentItem.DisplayText ?? "Unnamed Project",
            Description = part?.Description ?? part?.Notes,
            RepositoryName = part?.RepositoryName,
            RepositoryUrl = part?.RepositoryUrl,
            CurrentBranch = part?.CurrentBranch,
            TestingSteps = part?.TestingSteps ?? [],
            OpenQuestions = part?.OpenQuestions.Select(q => new OpenQuestion
            {
                Id = q.Id,
                Question = q.Question,
                Answer = q.Answer,
                IsResolved = q.IsResolved,
                CreatedAt = q.CreatedAt,
                ResolvedAt = q.ResolvedAt
            }).ToList() ?? [],
            TotalTimeSpentSeconds = part?.TotalTimeSpentSeconds ?? 0,
            CurrentSessionStartedAt = part?.CurrentSessionStartedAt,
            IsActive = part?.IsActive ?? false,
            Tags = part?.Tags ?? [],
            ParentProjectId = part?.ParentProjectId,
            DisplayOrder = part?.DisplayOrder ?? 0,
            Status = part?.Status ?? "To Do",
            CreatedAt = contentItem.CreatedUtc ?? DateTime.UtcNow,
            LastAccessedAt = contentItem.ModifiedUtc ?? DateTime.UtcNow
        };
        return context;
    }
}

// YesSql index for querying active contexts
public class WorkProjectPartIndex : MapIndex
{
    public bool IsActive { get; init; }
    public long TotalTimeSpentSeconds { get; set; }
}

public class WorkProjectPartIndexProvider : IndexProvider<ContentItem>
{
    public override void Describe(DescribeContext<ContentItem> context)
    {
        context.For<WorkProjectPartIndex>()
            .Map(contentItem =>
            {
                var part = contentItem.As<ProjectPart>();
                if (part == null) return null;

                var index = new WorkProjectPartIndex
                {
                    IsActive = part.IsActive,
                    TotalTimeSpentSeconds = part.TotalTimeSpentSeconds
                };
                return index;
            });
    }
}
