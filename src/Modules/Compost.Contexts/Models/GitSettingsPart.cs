using OrchardCore.ContentManagement;

namespace Compost.Contexts.Models;

/// <summary>
/// Content part for global Git Settings.
/// NOTE: PersonalAccessToken is stored securely via IDataProtector, not in this part.
/// </summary>
public class GitSettingsPart : ContentPart
{
    /// <summary>
    /// Key used to store the encrypted PersonalAccessToken in the content part (as protected data).
    /// </summary>
    public const string ProtectedTokenKey = "Compost.GitSettings.ProtectedToken";

    public string AuthorName { get; set; } = "Compost Assistant";

    public string AuthorEmail { get; set; } = "assistant@compost.net";
}
