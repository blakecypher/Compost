using System.Text.Json.Serialization;
using OrchardCore.ContentManagement;

namespace Compost.Contexts.Models;

/// <summary>
/// Content part for global Git Settings
/// </summary>
public class GitSettingsPart : ContentPart
{
    [JsonInclude]
    public string? PersonalAccessToken { get; set; }
    
    [JsonInclude]
    public string AuthorName { get; set; } = "Compost Assistant";
    
    [JsonInclude]
    public string AuthorEmail { get; set; } = "assistant@compost.net";
}
