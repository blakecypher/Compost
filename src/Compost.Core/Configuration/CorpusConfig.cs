namespace Compost.Core.Configuration;

/// <summary>
/// Configuration for the NLP classification corpus.
/// Loaded from corpus.json at startup with hardcoded fallback.
/// </summary>
public class CorpusConfig
{
    /// <summary>
    /// Domain-specific keyword mappings to semantic types with weights.
    /// Top-level key is the semantic type name, inner dictionary maps keywords to weights (0.0-1.0).
    /// </summary>
    public Dictionary<string, Dictionary<string, double>> DomainKeywords { get; set; } = [];

    /// <summary>
    /// Common stop words to exclude from keyword extraction.
    /// </summary>
    public List<string> StopWords { get; set; } = [];
}
