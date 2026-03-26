using Compost.Contexts.Models;
using Compost.Contexts.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using Microsoft.Extensions.Logging;

namespace Compost.Contexts.Drivers;

/// <summary>
/// Display driver for ProjectPart - handles rendering and updating
/// </summary>
public class WorkContextPartDisplayDriver(ILogger<WorkContextPartDisplayDriver> logger)
    : ContentPartDisplayDriver<ProjectPart>
{
    public override IDisplayResult Display(ProjectPart part, BuildPartDisplayContext context)
    {
        return Initialize<WorkContextPartViewModel>(nameof(ProjectPart), viewModel =>
        {
            viewModel.RepositoryName = part.RepositoryName;
            viewModel.RepositoryUrl = part.RepositoryUrl;
            viewModel.CurrentBranch = part.CurrentBranch;
            viewModel.TestingSteps = part.TestingSteps;
            viewModel.OpenQuestions = part.OpenQuestions;
            viewModel.TotalTimeSpent = TimeSpan.FromSeconds(part.TotalTimeSpentSeconds);
            viewModel.IsActive = part.IsActive;
            viewModel.Tags = part.Tags;
            viewModel.Notes = part.Notes;
            viewModel.ProjectPart = part;
        })
        .Location("Detail", "Content:5")
        .Location("Summary", "Meta:5");
    }

    public override IDisplayResult Edit(ProjectPart part, BuildPartEditorContext context)
    {
        return Initialize<WorkContextPartViewModel>("WorkContextPart_Edit", viewModel =>
        {
            viewModel.RepositoryName = part.RepositoryName;
            viewModel.RepositoryUrl = part.RepositoryUrl;
            viewModel.CurrentBranch = part.CurrentBranch;
            viewModel.TestingSteps = part.TestingSteps;
            viewModel.OpenQuestions = part.OpenQuestions;
            viewModel.Tags = part.Tags;
            viewModel.Notes = part.Notes;
            viewModel.ProjectPart = part;
        });
    }

    public override async Task<IDisplayResult> UpdateAsync(ProjectPart part, IUpdateModel updater, UpdatePartEditorContext context)
    {
        logger.LogInformation("=== DRIVER UpdateAsync CALLED ===");
        logger.LogInformation("BEFORE TryUpdateModel: RepoName='{RepoName}', RepoUrl='{RepoUrl}', Branch='{Branch}', Tags=[{Tags}]",
            part.RepositoryName, part.RepositoryUrl, part.CurrentBranch, string.Join(", ", part.Tags));
        
        var viewModel = new WorkContextPartViewModel();

        await updater.TryUpdateModelAsync(viewModel, Prefix);
        
        logger.LogInformation("ViewModel after TryUpdateModel: RepoName='{RepoName}', RepoUrl='{RepoUrl}', Branch='{Branch}', Tags=[{Tags}]",
            viewModel.RepositoryName, viewModel.RepositoryUrl, viewModel.CurrentBranch, string.Join(", ", viewModel.Tags));

        part.RepositoryName = viewModel.RepositoryName;
        part.RepositoryUrl = viewModel.RepositoryUrl;
        part.CurrentBranch = viewModel.CurrentBranch;
        part.TestingSteps = viewModel.TestingSteps;
        part.OpenQuestions = viewModel.OpenQuestions;
        part.Tags = viewModel.Tags;
        part.Notes = viewModel.Notes;
        
        logger.LogInformation("AFTER assignment: RepoName='{RepoName}', RepoUrl='{RepoUrl}', Branch='{Branch}', Tags=[{Tags}]",
            part.RepositoryName, part.RepositoryUrl, part.CurrentBranch, string.Join(", ", part.Tags));
        
        logger.LogInformation("=== DRIVER UpdateAsync END ===");

        return await EditAsync(part, context);
    }
}
