using Compost.Contexts.Models;
using OrchardCore.ContentManagement.Handlers;

namespace Compost.Contexts.Handlers;

/// <summary>
/// Content handler for WorkContextPart - handles lifecycle events
/// </summary>
public class ProjectPartHandler : ContentPartHandler<ProjectPart>
{
    public override Task ActivatedAsync(ActivatedContentContext context, ProjectPart instance)
    {
        // When a context is activated (switched to), start timing
        if (instance is { IsActive: true, CurrentSessionStartedAt: null })
        {
            instance.CurrentSessionStartedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public override Task UpdatedAsync(UpdateContentContext project, ProjectPart instance)
    {
        // When a project is deactivated, add session time to total
        if (instance is not { IsActive: false, CurrentSessionStartedAt: not null }) return Task.CompletedTask;
        var sessionDuration = DateTime.UtcNow - instance.CurrentSessionStartedAt.Value;
        instance.TotalTimeSpentSeconds += (long)sessionDuration.TotalSeconds;
        instance.CurrentSessionStartedAt = null;

        return Task.CompletedTask;
    }
}
