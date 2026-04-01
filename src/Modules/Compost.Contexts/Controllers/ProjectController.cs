using Compost.Contexts.Services;
using Compost.Contexts.ViewModels;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Compost.Contexts.Controllers;

public class ProjectController(IProjectManager projectManager, ITimeTrackingService timeTrackingService)
    : Controller
{
    public async Task<IActionResult> Index()
    {
        var projects = await projectManager.GetAllProjectsAsync();
        var activeProject = await projectManager.GetActiveProjectAsync();
        
        var viewModel = new ContextListViewModel
        {
            Contexts = projects.Select(p => new Project
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
                TotalTimeSpentSeconds = p.TotalTimeSpentSeconds,
                TestingSteps = p.TestingSteps,
                OpenQuestions = p.OpenQuestions,
                CreatedAt = p.CreatedAt,
                LastAccessedAt = p.LastAccessedAt,
                CurrentSessionStartedAt = p.CurrentSessionStartedAt
            }).ToList(),
            ActiveContextId = activeProject?.Id
        };

        // Redirect to TreeView as the main interface
        return RedirectToAction(nameof(TreeView));
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

        var tags = model.Tags?.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList() ?? [];
        
        var project = await projectManager.CreateProjectAsync(
            model.Name, 
            model.Description,
            model.RepositoryName,
            model.RepositoryUrl,
            model.CurrentBranch,
            tags,
            model.Status,
            model.ParentContextId,
            model.DisplayOrder
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
            TotalTimeSpentSeconds = project.TotalTimeSpentSeconds,
            TestingSteps = project.TestingSteps,
            OpenQuestions = project.OpenQuestions
        };

        // Populate ViewBag for parent project dropdown
        ViewBag.AllContexts = await projectManager.GetAllProjectsAsync();

        Console.WriteLine($"[DEBUG] Edit GET - ViewModel: RepoName='{viewModel.RepositoryName}', Tags='{viewModel.Tags}'");

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

        // Debug logging
        Console.WriteLine($"[DEBUG] Edit POST - Model received: Name='{model.Name}', RepoName='{model.RepositoryName}', RepoUrl='{model.RepositoryUrl}', Branch='{model.CurrentBranch}', Tags='{model.Tags}'");

        context.Name = model.Name;
        context.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description;
        context.RepositoryName = string.IsNullOrWhiteSpace(model.RepositoryName) ? null : model.RepositoryName;
        context.RepositoryUrl = string.IsNullOrWhiteSpace(model.RepositoryUrl) ? null : model.RepositoryUrl;
        context.CurrentBranch = string.IsNullOrWhiteSpace(model.CurrentBranch) ? null : model.CurrentBranch;
        context.Tags = string.IsNullOrWhiteSpace(model.Tags) ? null : model.Tags.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
        context.ParentProjectId = string.IsNullOrWhiteSpace(model.ParentContextId) ? null : model.ParentContextId;
        context.DisplayOrder = model.DisplayOrder;
        context.Status = model.Status;
        context.IsActive = model.IsActive;
        
        Console.WriteLine($"[DEBUG] Edit POST - Project before save: RepoName='{context.RepositoryName}', Tags='{string.Join(",", context.Tags ?? [])}'");

        await projectManager.UpdateProjectAsync(context);
        
        TempData["SuccessMessage"] = $"Project '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(List));
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

}
