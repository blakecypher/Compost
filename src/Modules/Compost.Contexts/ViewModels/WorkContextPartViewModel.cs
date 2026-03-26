using Compost.Contexts.Models;
using OpenQuestion = Compost.Core.Models.OpenQuestion;

namespace Compost.Contexts.ViewModels;

public class WorkContextPartViewModel
{
    public string? RepositoryName { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? CurrentBranch { get; set; }
    public List<string> TestingSteps { get; set; } = [];
    public List<OpenQuestion> OpenQuestions { get; set; } = [];
    public TimeSpan TotalTimeSpent { get; set; }
    public bool IsActive { get; set; }
    public List<string> Tags { get; set; } = [];
    public string? Notes { get; set; }

    // Reference to the content part
    public ProjectPart? ProjectPart { get; set; }
}
