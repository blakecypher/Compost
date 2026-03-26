using Newtonsoft.Json;

namespace Compost.Core.Models;

/// <summary>
/// Represents a reusable architectural pattern with documentation and examples
/// </summary>
public class ArchitecturalPattern
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the pattern (e.g., "CQRS with MediatR", "Event Sourcing")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brief description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When to use this pattern
    /// </summary>
    public string WhenToUse { get; set; } = string.Empty;

    /// <summary>
    /// How it works - detailed explanation
    /// </summary>
    public string HowItWorks { get; set; } = string.Empty;

    /// <summary>
    /// Common gotchas and pitfalls
    /// </summary>
    public List<string> Gotchas { get; set; } = [];

    /// <summary>
    /// Code examples demonstrating the pattern
    /// </summary>
    public List<string> CodeSnippetIds { get; set; } = [];

    /// <summary>
    /// Projects where this pattern has been used
    /// </summary>
    public List<ProjectReference> UsedInProjects { get; set; } = [];

    /// <summary>
    /// Related patterns
    /// </summary>
    public List<string> RelatedPatternIds { get; set; } = [];

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Category (e.g., Messaging, Data Access, API Design)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// AI embedding vector for similarity matching
    /// </summary>
    public float[]? EmbeddingVector { get; set; }

    /// <summary>
    /// How many times this pattern has been used
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// When this pattern was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this pattern was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Keywords for pattern matching (AI confidence score)
    /// </summary>
    public List<string> Keywords { get; set; } = [];

    /// <summary>
    /// Success score for AI pattern recognition (0.0 to 1.0)
    /// </summary>
    public float SuccessScore { get; set; } = 0.0f;

    /// <summary>
    /// Cosmos DB partition key
    /// </summary>
    [JsonProperty("partitionKey")]
    public string PartitionKey => "pattern";
}

public class ProjectReference
{
    public string ProjectName { get; set; } = string.Empty;
    public string? RepositoryUrl { get; set; }
    public string? SpecificImplementationPath { get; set; }
    public string? Notes { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}
