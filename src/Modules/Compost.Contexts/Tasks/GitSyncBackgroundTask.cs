using Compost.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchardCore.BackgroundTasks;

namespace Compost.Contexts.Tasks;

/// <summary>
/// Background task that periodically synchronizes Git repositories for active projects.
/// Runs once per hour by default.
/// </summary>
[BackgroundTask(Schedule = "0 * * * *", Description = "Synchronizes active Git repositories.")]
public class GitSyncBackgroundTask : IBackgroundTask
{
    public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<GitSyncBackgroundTask>>();
        var projectManager = serviceProvider.GetRequiredService<IProjectManager>();
        var gitService = serviceProvider.GetRequiredService<IGitService>();

        logger.LogInformation("Starting Git synchronization background task.");

        var allProjects = await projectManager.GetAllProjectsAsync();
        var projectsWithGit = allProjects.Where(p => p.IsGitActive && !string.IsNullOrEmpty(p.GitLocalPath)).ToList();

        if (!projectsWithGit.Any())
        {
            logger.LogInformation("No active Git projects found to sync.");
            return;
        }

        foreach (var project in projectsWithGit)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                logger.LogInformation("Syncing project: {ProjectName} at {LocalPath}", project.Name, project.GitLocalPath);

                if (project.GitLocalPath != null && !gitService.IsRepositoryValid(project.GitLocalPath))
                {
                    if (!string.IsNullOrEmpty(project.RepositoryUrl))
                    {
                        logger.LogInformation("Repository not found in {LocalPath}, attempting clone from {RemoteUrl}", project.GitLocalPath, project.RepositoryUrl);
                        await gitService.CloneAsync(project.RepositoryUrl, project.GitLocalPath, project.CurrentBranch);
                    }
                    else
                    {
                        logger.LogWarning("Project {ProjectName} has Git active but no RepositoryUrl and local path is invalid.", project.Name);
                        continue;
                    }
                }
                else
                {
                    logger.LogInformation("Updating project: {ProjectName}", project.Name);
                    if (project.GitLocalPath != null) await gitService.PullAsync(project.GitLocalPath);
                }

                // Update last sync time
                project.LastSyncAt = DateTime.UtcNow;
                await projectManager.UpdateProjectAsync(project);
                
                logger.LogInformation("Successfully synced project: {ProjectName}", project.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error syncing project {ProjectName}", project.Name);
            }
        }

        logger.LogInformation("Git synchronization background task completed.");
    }
}
