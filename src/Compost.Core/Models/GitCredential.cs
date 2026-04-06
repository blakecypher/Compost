namespace Compost.Core.Models;

public class GitCredential
{
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string AuthorName { get; set; } = "Compost Assistant";
    public string AuthorEmail { get; set; } = "assistant@compost.net";
}
