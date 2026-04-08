using Compost.Contexts.Services;
using Compost.Contexts.ViewModels;
using Compost.Core.Extensions;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Compost.Contexts.Controllers;

public class ProjectController(IProjectManager projectManager, ITimeTrackingService timeTrackingService, IGitService gitService)
    : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction(nameof(GitDashboard));
    }

    public async Task<IActionResult> GitDashboard()
    {
        var projects = await projectManager.GetAllProjectsAsync();
        var gitProjects = projects.Where(p => p.IsGitActive).ToList();
        
        var statusList = new List<(Project Project, GitStatus Status)>();
        foreach (var project in gitProjects)
        {
            if (!string.IsNullOrEmpty(project.GitLocalPath))
            {
                var status = gitService.GetRepositoryStatus(project.GitLocalPath);
                statusList.Add((project, status));
            }
        }
        
        return View("../Context/GitDashboard", statusList);
    }
    public IActionResult List()
    {
        // Redirect to TreeView as it's now the primary interface
        return RedirectToAction(nameof(TreeView));
    }

    public async Task<IActionResult> Create()
    {
        // Populate ViewBag for parent project dropdown
        ViewBag.AllContexts = await projectManager.GetAllProjectsAsync();
        return View("../Context/Create", new CreateContextViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateContextViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AllContexts = await projectManager.GetAllProjectsAsync();
            return View("../Context/Create", model);
        }

        var tags = model.Tags.ParseTags();
        
        var project = await projectManager.CreateProjectAsync(
            model.Name, 
            model.Description,
            model.RepositoryName,
            model.RepositoryUrl,
            model.CurrentBranch,
            tags,
            model.Status,
            model.ParentContextId,
            model.DisplayOrder,
            model.GitLocalPath,
            model.IsGitActive
        );
        
        TempData["SuccessMessage"] = $"Project '{model.Name}' created successfully.";
        return RedirectToAction(nameof(TreeView));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var project = await projectManager.GetProjectByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        // Debug logging
        Console.WriteLine($"[DEBUG] Edit GET - Loaded from DB: RepoName='{project.RepositoryName}', RepoUrl='{project.RepositoryUrl}', Branch='{project.CurrentBranch}', Tags='{string.Join(",", project.Tags ?? [])}'");

        var viewModel = new EditContextViewModel
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            RepositoryName = project.RepositoryName,
            RepositoryUrl = project.RepositoryUrl,
            CurrentBranch = project.CurrentBranch,
            Tags = string.Join(", ", project.Tags ?? []),
            ParentContextId = project.ParentProjectId,
            DisplayOrder = project.DisplayOrder,
            Status = project.Status,
            IsActive = project.IsActive,
            IsRepositoryPresent = !string.IsNullOrEmpty(project.GitLocalPath) && gitService.IsRepositoryValid(project.GitLocalPath),
            GitLocalPath = project.GitLocalPath,
            IsGitActive = project.IsGitActive,
            LastSyncAt = project.LastSyncAt,
            TotalTimeSpentSeconds = project.TotalTimeSpentSeconds,
            CurrentSessionStartedAt = project.CurrentSessionStartedAt,
            TestingSteps = project.TestingSteps,
            OpenQuestions = project.OpenQuestions
        };

        // Populate ViewBag for parent project dropdown
        ViewBag.AllContexts = await projectManager.GetAllProjectsAsync();

        return View("../Context/Edit", viewModel);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditContextViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AllContexts = await projectManager.GetAllProjectsAsync();
            return View("../Context/Edit", model);
        }

        var context = await projectManager.GetProjectByIdAsync(id);
        if (context == null)
        {
            return NotFound();
        }

        context.Name = model.Name;
        context.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description;
        context.RepositoryName = string.IsNullOrWhiteSpace(model.RepositoryName) ? null : model.RepositoryName;
        context.RepositoryUrl = string.IsNullOrWhiteSpace(model.RepositoryUrl) ? null : model.RepositoryUrl;
        context.CurrentBranch = string.IsNullOrWhiteSpace(model.CurrentBranch) ? null : model.CurrentBranch;
        context.Tags = model.Tags.ParseTags();
        context.ParentProjectId = string.IsNullOrWhiteSpace(model.ParentContextId) ? null : model.ParentContextId;
        context.DisplayOrder = model.DisplayOrder;
        context.Status = model.Status;
        context.IsActive = model.IsActive;
        context.GitLocalPath = model.GitLocalPath;
        context.IsGitActive = model.IsGitActive;
        
        await projectManager.UpdateProjectAsync(context);
        
        TempData["SuccessMessage"] = $"Project '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(TreeView));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var context = await projectManager.GetProjectByIdAsync(id);
            if (context == null)
            {
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToAction(nameof(List));
            }

            await projectManager.DeleteProjectAsync(id);
            
            TempData["SuccessMessage"] = $"Project '{context.Name}' deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to delete context: {ex.Message}";
        }
        
        return RedirectToAction(nameof(List));
    }


    [HttpPost]
    public async Task<IActionResult> Switch(string id)
    {
        await projectManager.SwitchProjectAsync(id);
        
        var context = await projectManager.GetProjectByIdAsync(id);
        TempData["SuccessMessage"] = $"Switched to context '{context?.Name}'.";
        
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> StartSession(string id)
    {
        await projectManager.StartSessionAsync(id);
        
        var context = await projectManager.GetProjectByIdAsync(id);
        TempData["SuccessMessage"] = $"Started tracking time for '{context?.Name}'.";
        
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> EndSession(string id)
    {
        await projectManager.EndSessionAsync(id);
        
        var context = await projectManager.GetProjectByIdAsync(id);
        var timeSpent = await timeTrackingService.GetTotalTimeSpentAsync(id);
        TempData["SuccessMessage"] = $"Ended session for '{context?.Name}'. Total time: {timeSpent:hh\\:mm\\:ss}";
        
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> AddTestingStep(string id, string step)
    {
        if (!string.IsNullOrWhiteSpace(step))
        {
            await projectManager.AddTestingStepAsync(id, step);
            TempData["SuccessMessage"] = "Testing step added.";
        }
        
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AddQuestion(string id, string question)
    {
        if (!string.IsNullOrWhiteSpace(question))
        {
            await projectManager.AddOpenQuestionAsync(id, question);
            TempData["SuccessMessage"] = "Question added.";
        }
        
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sync(string id)
    {
        var project = await projectManager.GetProjectByIdAsync(id);
        if (project == null || !project.IsGitActive || string.IsNullOrEmpty(project.GitLocalPath))
        {
            TempData["ErrorMessage"] = "Git sync is not active for this project.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        try
        {
            var result = await gitService.PullAsync(project.GitLocalPath);
            if (result)
            {
                project.LastSyncAt = DateTime.UtcNow;
                await projectManager.UpdateProjectAsync(project);
                TempData["SuccessMessage"] = "Successfully synced with remote repository.";
            }
            else
            {
                TempData["ErrorMessage"] = "Sync failed. Check if remote exists and credentials are valid.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Sync error: {ex.Message}";
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clone(string id)
    {
        var project = await projectManager.GetProjectByIdAsync(id);
        if (project == null || string.IsNullOrEmpty(project.RepositoryUrl) || string.IsNullOrEmpty(project.GitLocalPath))
        {
            TempData["ErrorMessage"] = "Repository URL or local path missing.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        try
        {
            var result = await gitService.CloneAsync(project.RepositoryUrl, project.GitLocalPath, project.CurrentBranch);
            if (result)
            {
                project.IsGitActive = true;
                project.LastSyncAt = DateTime.UtcNow;
                await projectManager.UpdateProjectAsync(project);
                TempData["SuccessMessage"] = "Successfully cloned repository.";
            }
            else
            {
                TempData["ErrorMessage"] = "Clone failed. Directory might not be empty or remote is unreachable.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Clone error: {ex.Message}";
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveQuestion(string projectId, string questionId, string answer)
    {
        if (!string.IsNullOrWhiteSpace(answer))
        {
            await projectManager.ResolveQuestionAsync(projectId, questionId, answer);
            TempData["SuccessMessage"] = "Question resolved.";
        }
        
        return RedirectToAction(nameof(Edit), new { id = projectId });
    }

    // GET: /Contexts/TreeView - Now the main interface
    public async Task<IActionResult> TreeView()
    {
        var projects = await projectManager.GetAllProjectsAsync();
        var activeProject = await projectManager.GetActiveProjectAsync();
        
        // Convert to ProjectContext for now to maintain compatibility with existing view
        var contexts = projects.Select(p => new Project() 
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            RepositoryName = p.RepositoryName,
            RepositoryUrl = p.RepositoryUrl,
            CurrentBranch = p.CurrentBranch,
            Tags = p.Tags,
            ParentProjectId = p.ParentProjectId,
            DisplayOrder = p.DisplayOrder,
            Status = p.Status,
            IsActive = p.IsActive,
            TestingSteps = p.TestingSteps,
            OpenQuestions = p.OpenQuestions,
            CreatedAt = p.CreatedAt,
            LastAccessedAt = p.LastAccessedAt,
            CurrentSessionStartedAt = p.CurrentSessionStartedAt
        }).ToList();
        
        ViewBag.ActiveProject = activeProject;
        
        return View("../Context/TreeView", contexts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateParent(string projectId, string? parentId)
    {
        try
        {
            var context = await projectManager.GetProjectByIdAsync(projectId);
            if (context == null)
            {
                return NotFound();
            }

            context.ParentProjectId = parentId;
            await projectManager.UpdateProjectAsync(context);

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncAll()
    {
        var projects = await projectManager.GetAllProjectsAsync();
        var gitProjects = projects.Where(p => p.IsGitActive && !string.IsNullOrEmpty(p.GitLocalPath)).ToList();
        
        int successCount = 0;
        int failCount = 0;
        
        foreach (var project in gitProjects)
        {
            try
            {
                var result = await gitService.PullAsync(project.GitLocalPath);
                if (result)
                {
                    project.LastSyncAt = DateTime.UtcNow;
                    await projectManager.UpdateProjectAsync(project);
                    successCount++;
                }
                else failCount++;
            }
            catch { failCount++; }
        }
        
        TempData["SuccessMessage"] = $"Sync complete. Success: {successCount}, Failed: {failCount}";
        return RedirectToAction(nameof(GitDashboard));
    }
}
