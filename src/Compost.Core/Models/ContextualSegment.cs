using System.Text.Json.Serialization;

namespace Compost.Core.Models;

/// <summary>
/// Represents a classified transcript segment with semantic context
/// </summary>
public class ContextualSegment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// The original transcript text
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Start time in the recording
    /// </summary>
    public TimeSpan StartTime { get; set; }
    
    /// <summary>
    /// End time in the recording
    /// </summary>
    public TimeSpan EndTime { get; set; }
    
    /// <summary>
    /// Speaker identifier
    /// </summary>
    public string? SpeakerId { get; set; }
    
    /// <summary>
    /// Semantic classification of the segment
    /// </summary>
    public SegmentSemanticType SemanticType { get; set; } = SegmentSemanticType.Informational;
    
    /// <summary>
    /// Confidence score of the classification (0.0 - 1.0)
    /// </summary>
    public double ClassificationConfidence { get; set; }
    
    /// <summary>
    /// Contextual theme/topic this segment belongs to
    /// </summary>
    public string? Theme { get; set; }
    
    /// <summary>
    /// Keywords extracted from this segment
    /// </summary>
    public List<string> Keywords { get; set; } = [];
    
    /// <summary>
    /// Related segment IDs (contextual proximity)
    /// </summary>
    public List<string> RelatedSegmentIds { get; set; } = [];
    
    /// <summary>
    /// Whether this segment is a key insight/important
    /// </summary>
    public bool IsKeyInsight { get; set; }
    
    /// <summary>
    /// Suggested mind map node type based on content
    /// </summary>
    public MindMapNodeType SuggestedNodeType { get; set; } = MindMapNodeType.Note;
    
    /// <summary>
    /// Proposed title for a mind map node
    /// </summary>
    public string? ProposedTitle { get; set; }
}

/// <summary>
/// Semantic types for transcript segments
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SegmentSemanticType
{
    Decision,           // Decisions made, agreements reached
    Action,             // Action items, tasks assigned
    Requirement,        // Requirements, needs, specifications
    Constraint,         // Constraints, limitations, restrictions
    Risk,               // Risks, issues, concerns, blockers
    Opportunity,        // Opportunities, potential improvements
    Informational,      // General information, context, background
    Question,           // Questions raised, clarifications needed
    Idea,               // Ideas, suggestions, proposals
    Commitment,         // Commitments, promises, deadlines
    Problem,            // Problems identified
    Solution,           // Solutions proposed
    Resource,           // Resources, tools, references mentioned
    Timeline,           // Timeline, dates, milestones
    Goal,               // Goals, objectives, targets
    Metric,             // Metrics, KPIs, measurements
    Assumption,         // Assumptions made
    Dependency,         // Dependencies identified
    Stakeholder,        // Stakeholder mentions
    Technical,          // Technical details, implementation notes
    Process,            // Process descriptions, workflows
    Policy,             // Policies, rules, guidelines
    Financial,          // Budget, cost, financial matters
    Legal,              // Legal, compliance, regulatory
    Strategic,          // Strategic considerations
    Operational,        // Operational details
    
    // Intellectual/Philosophical discourse types
    Theory,             // Theoretical frameworks, conceptual models
    Hypothesis,         // Hypotheses, testable propositions
    Principle,          // Guiding principles, axioms, tenets
    Concept,            // Abstract concepts, notions
    Paradigm,           // Paradigms, worldviews, mental models
    Framework,          // Conceptual frameworks, architectures
    Analysis,           // Analytical observations, breakdowns
    Synthesis,          // Synthesis, combining ideas
    Insight,            // Deep insights, realizations
    Reflection,         // Reflective thoughts, contemplations
    Argument,           // Arguments, reasoning, logic
    Evidence,           // Supporting evidence, data points
    Counterpoint,       // Counter-arguments, opposing views
    Implication,        // Implications, consequences
    Connection,         // Connections between concepts
    Pattern,            // Recognized patterns
    Abstraction,        // Abstract thinking, generalization
    QuestionFundamental, // Fundamental questions, existential queries
    Unknown             // Unclassified
}

/// <summary>
/// Represents a contextual theme/topic cluster extracted from transcript
/// </summary>
public class ContextualTheme
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Theme name/title
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Theme description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Keywords associated with this theme
    /// </summary>
    public List<string> Keywords { get; set; } = [];
    
    /// <summary>
    /// Segment IDs belonging to this theme
    /// </summary>
    public List<string> SegmentIds { get; set; } = [];
    
    /// <summary>
    /// Importance/relevance score
    /// </summary>
    public double RelevanceScore { get; set; }
    
    /// <summary>
    /// Suggested node type for this theme
    /// </summary>
    public MindMapNodeType SuggestedNodeType { get; set; } = MindMapNodeType.Idea;
    
    /// <summary>
    /// Whether this theme should be a top-level node
    /// </summary>
    public bool IsTopLevelTheme { get; set; }
}

/// <summary>
/// Result of transcript context extraction
/// </summary>
public class TranscriptContextResult
{
    /// <summary>
    /// All classified segments
    /// </summary>
    public List<ContextualSegment> Segments { get; set; } = [];
    
    /// <summary>
    /// Extracted themes/clusters
    /// </summary>
    public List<ContextualTheme> Themes { get; set; } = [];
    
    /// <summary>
    /// Key insights extracted
    /// </summary>
    public List<ContextualSegment> KeyInsights { get; set; } = [];
    
    /// <summary>
    /// Generated mind map nodes from context
    /// </summary>
    public List<MindMapNode> GeneratedNodes { get; set; } = [];
    
    /// <summary>
    /// Extraction confidence
    /// </summary>
    public double OverallConfidence { get; set; }
    
    /// <summary>
    /// Processing metadata
    /// </summary>
    public ExtractionMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Metadata about the extraction process
/// </summary>
public class ExtractionMetadata
{
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public int TotalSegments { get; set; }
    public int ClassifiedSegments { get; set; }
    public int ThemesExtracted { get; set; }
    public int NodesGenerated { get; set; }
    public string? ExtractionMethod { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
}
