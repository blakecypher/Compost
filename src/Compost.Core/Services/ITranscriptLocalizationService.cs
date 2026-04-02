using System.Collections.Generic;

namespace Compost.Core.Services;

/// <summary>
/// Provides localized strings for transcript segments and UI elements
/// Uses a dictionary-based approach for runtime localization without .resx files
/// </summary>
public interface ITranscriptLocalizationService
{
    string GetString(string key, string? culture = null);
    string GetString(string key, Dictionary<string, object> parameters, string? culture = null);
    void SetCulture(string culture);
    string CurrentCulture { get; }
}
