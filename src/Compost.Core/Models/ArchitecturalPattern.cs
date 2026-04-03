using Newtonsoft.Json;

namespace Compost.Core.Models;

/// <summary>
/// Represents a reusable architectural pattern with documentation and examples
/// </summary>
public class ArchitecturalPattern
{
    [JsonProperty("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the pattern (e.g., "CQRS with MediatR", "Event Sourcing")
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Brief description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// When to use this pattern
    /// </summary>
    public string WhenToUse { get; init; } = string.Empty;

    /// <summary>
    /// How it works - detailed explanation
    /// </summary>
    public string HowItWorks { get; init; } = string.Empty;

    /// <summary>
    /// Common gotchas and pitfalls
    /// </summary>
    public List<string> Gotchas { get; init; } = [];

    /// <summary>
    /// Code examples demonstrating the pattern
    /// </summary>
    public List<string> CodeSnippetIds { get; init; } = [];

    /// <summary>
    /// Projects where this pattern has been used
    /// </summary>
    public List<ProjectReference> UsedInProjects { get; init; } = [];

    /// <summary>
    /// Related patterns
    /// </summary>
    public List<string> RelatedPatternIds { get; init; } = [];

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>
    /// Category (e.g., Messaging, Data Access, API Design)
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// AI embedding vector for similarity matching
    /// </summary>
    public float[]? EmbeddingVector { get; init; }

    /// <summary>
    /// How many times this pattern has been used
    /// </summary>
    public int UsageCount { get; init; }

    /// <summary>
    /// When this pattern was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When this pattern was last modified
    /// </summary>
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Keywords for pattern matching (AI confidence score)
    /// </summary>
    public List<string> Keywords { get; init; } = [];

    /// <summary>
    /// Success score for AI pattern recognition (0.0 to 1.0)
    /// </summary>
    public float SuccessScore { get; init; } = 0.0f;

    /// <summary>
    /// Cosmos DB partition key
    /// </summary>
    [JsonProperty("partitionKey")]
    public string PartitionKey => "pattern";
}

public class ProjectReference
{
    public string ProjectName { get; init; } = string.Empty;
    public string? RepositoryUrl { get; init; }
    public string? SpecificImplementationPath { get; init; }
    public string? Notes { get; init; }
    public DateTime UsedAt { get; init; } = DateTime.UtcNow;
}
