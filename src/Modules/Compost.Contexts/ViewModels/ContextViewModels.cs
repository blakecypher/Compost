using System.ComponentModel.DataAnnotations;
using Compost.Core.Models;

namespace Compost.Contexts.ViewModels;

public class ContextListViewModel
{
    public List<Project> Contexts { get; set; } = [];
    public string? ActiveContextId { get; set; }
}

public class CreateContextViewModel
{
    [Required]
    [Display(Name = "Project Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = nameof(Description))]
    public string? Description { get; set; }

    [Display(Name = "Repository Name")]
    public string? RepositoryName { get; set; }

    [Display(Name = "Repository URL")]
    [Url]
    public string? RepositoryUrl { get; set; }

    [Display(Name = "Current Branch")]
    public string? CurrentBranch { get; set; }

    [Display(Name = "Tags (comma-separated)")]
    public string? Tags { get; set; }

    [Display(Name = "Parent Project")]
    public string? ParentContextId { get; set; }

    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; } = 0;

    [Display(Name = nameof(Status))]
    public string Status { get; set; } = "To Do";

    [Display(Name = "Local Git Path")]
    public string? GitLocalPath { get; set; }

    [Display(Name = "Enable Git Sync")]
    public bool IsGitActive { get; set; }
}

public class EditContextViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Project Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = nameof(Description))]
    public string? Description { get; set; }

    [Display(Name = "Repository Name")]
    public string? RepositoryName { get; set; }

    [Display(Name = "Repository URL")]
    [Url]
    public string? RepositoryUrl { get; set; }

    [Display(Name = "Current Branch")]
    public string? CurrentBranch { get; set; }

    [Display(Name = "Tags (comma-separated)")]
    public string? Tags { get; set; }

    [Display(Name = "Parent Project")]
    public string? ParentContextId { get; set; }

    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    [Display(Name = nameof(Status))]
    public string Status { get; set; } = "To Do";

    [Display(Name = "Local Git Path")]
    public string? GitLocalPath { get; set; }

    [Display(Name = "Enable Git Sync")]
    public bool IsGitActive { get; set; }

    public DateTime? LastSyncAt { get; set; }

    public bool IsActive { get; set; }
    public bool IsRepositoryPresent { get; set; }
    public long TotalTimeSpentSeconds { get; set; }
    public DateTime? CurrentSessionStartedAt { get; set; }
    public List<string> TestingSteps { get; set; } = [];
    public List<OpenQuestion> OpenQuestions { get; set; } = [];
}
