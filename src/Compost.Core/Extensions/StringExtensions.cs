namespace Compost.Core.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Parses a comma-separated string of tags into a clean list of trimmed, non-empty strings.
    /// </summary>
    public static List<string> ParseTags(this string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return [];
        return tags.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
    }
}
