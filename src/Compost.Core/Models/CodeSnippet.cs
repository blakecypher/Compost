using Newtonsoft.Json;

namespace Compost.Core.Models;

/// <summary>
/// Represents a searchable code snippet with metadata
/// </summary>
public class CodeSnippet
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Title or name of the snippet
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this snippet does
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The actual code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Programming language
    /// </summary>
    public string Language { get; set; } = "csharp";

    /// <summary>
    /// Project this snippet is from
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>
    /// Repository URL
    /// </summary>
    public string? RepositoryUrl { get; set; }

    /// <summary>
    /// File path in the repository
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Project context this was captured in
    /// </summary>
    public string? WorkContextId { get; set; }

    /// <summary>
    /// Related architectural patterns
    /// </summary>
    public List<string> ArchitecturalPatternIds { get; set; } = [];

    /// <summary>
    /// Tags for categorization and search
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Category (e.g., "Retry Logic", "Authentication", "Error Handling")
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Dependencies/NuGet packages required
    /// </summary>
    public List<string> Dependencies { get; set; } = [];

    /// <summary>
    /// Usage notes or best practices
    /// </summary>
    public string? UsageNotes { get; set; }

    /// <summary>
    /// AI embedding vector for semantic search
    /// </summary>
    public float[]? EmbeddingVector { get; set; }

    /// <summary>
    /// How many times this snippet has been referenced
    /// </summary>
    public int ReferenceCount { get; set; }

    /// <summary>
    /// When this snippet was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this snippet was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}
