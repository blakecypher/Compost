using Compost.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Compost.Core.Services;

/// <summary>
/// Intelligent transcript context extractor using semantic analysis and corpus-based classification
/// Inspired by ChatRTX/NVIDIA's context extraction from corpus approach
/// </summary>
public interface ITranscriptContextExtractor
{
    Task<TranscriptContextResult> ExtractContextAsync(List<TranscriptSegment> segments, string? meetingTitle = null);
    Task<List<MindMapNode>> GenerateMindMapNodesAsync(TranscriptContextResult context);
}

public class TranscriptContextExtractor : ITranscriptContextExtractor
{
    private readonly ILogger<TranscriptContextExtractor> _logger;
    private readonly HttpClient _httpClient;
    private readonly ContextCorpusDictionary _corpus;
    private readonly string? _geminiApiKey;
    private readonly string? _geminiModel;
    private readonly string? _ollamaUrl;
    private readonly string? _ollamaModel;
    private readonly bool _useLocalLlm;

    // Semantic classification patterns with weights
    private readonly Dictionary<SegmentSemanticType, List<(Regex Pattern, double Weight)>> _semanticPatterns;

    public TranscriptContextExtractor(
        ILogger<TranscriptContextExtractor> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _corpus = new ContextCorpusDictionary(configuration, null);
        _geminiApiKey = configuration["Compost:Gemini:ApiKey"];
        _geminiModel = configuration["Compost:Gemini:Model"] ?? "gemini-2.0-flash";
        
        // Local LLM (Ollama) configuration
        _ollamaUrl = configuration["Compost:Ollama:Url"] ?? "http://localhost:11434";
        _ollamaModel = configuration["Compost:Ollama:Model"] ?? "llama3.2";
        _useLocalLlm = configuration.GetValue<bool>("Compost:Ollama:Enabled");

        _semanticPatterns = InitializeSemanticPatterns();
    }

    /// <summary>
    /// Main extraction pipeline: segments -> classification -> clustering -> themes -> nodes
    /// </summary>
    public async Task<TranscriptContextResult> ExtractContextAsync(List<TranscriptSegment> segments, string? meetingTitle = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new TranscriptContextResult();

        try
        {
            _logger.LogInformation("Starting context extraction for {SegmentCount} segments", segments.Count);

            // Step 1: Pre-process and merge fragmented segments
            var consolidatedSegments = ConsolidateSegments(segments);
            _logger.LogInformation("Consolidated into {Count} meaningful segments", consolidatedSegments.Count);

            // Step 2: Classify each segment semantically
            result.Segments = await ClassifySegmentsAsync(consolidatedSegments);
            result.Metadata.ClassifiedSegments = result.Segments.Count;
            _logger.LogInformation("Classified {Count} segments", result.Segments.Count);

            // Step 3: Extract themes through clustering
            result.Themes = ExtractThemes(result.Segments);
            result.Metadata.ThemesExtracted = result.Themes.Count;
            _logger.LogInformation("Extracted {Count} themes", result.Themes.Count);

            // Step 4: Identify key insights
            result.KeyInsights = ExtractKeyInsights(result.Segments);
            _logger.LogInformation("Identified {Count} key insights", result.KeyInsights.Count);

            // Step 5: Generate mind map nodes from themes and insights
            result.GeneratedNodes = await GenerateMindMapNodesAsync(result);
            result.Metadata.NodesGenerated = result.GeneratedNodes.Count;
            _logger.LogInformation("Generated {Count} mind map nodes", result.GeneratedNodes.Count);

            result.OverallConfidence = CalculateOverallConfidence(result.Segments);
            result.Metadata.TotalSegments = segments.Count;
            result.Metadata.ExtractionMethod = string.IsNullOrEmpty(_geminiApiKey) ? "LocalCorpus" : "Hybrid-AI";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in context extraction pipeline");
            // Return partial results if available
        }
        finally
        {
            stopwatch.Stop();
            result.Metadata.ProcessingDuration = stopwatch.Elapsed;
            _logger.LogInformation("Context extraction completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
        }

        return result;
    }

    /// <summary>
    /// Generate mind map nodes from extracted context
    /// Philosophy: EVERY meaningful segment becomes a node in the semantic content network
    /// </summary>
    public Task<List<MindMapNode>> GenerateMindMapNodesAsync(TranscriptContextResult context)
    {
        var nodes = new List<MindMapNode>();
        
        // Build semantic content network: each segment is a node
        // This creates a rich graph of ideas for innovation and expansion
        foreach (var segment in context.Segments.Where(s => s.Text.Length >= 15)) // Meaningful content threshold
        {
            var node = CreateNodeFromSegment(segment);
            nodes.Add(node);
        }
        
        // Build edges between semantically related nodes
        BuildSemanticEdges(nodes, context.Segments);
        
        _logger.LogInformation("Generated {Count} mind map nodes from {SegmentCount} segments - each segment is now a node in the content network", 
            nodes.Count, context.Segments.Count);

        return Task.FromResult(nodes);
    }
    
    private MindMapNode CreateNodeFromSegment(ContextualSegment segment)
    {
        return new MindMapNode
        {
            Id = Guid.NewGuid().ToString(),
            Title = segment.ProposedTitle ?? (segment.Text.Length > 60 ? segment.Text[..57].Trim() + "..." : segment.Text.Trim()),
            Description = $"[{segment.SemanticType}] {segment.Text}",
            NodeType = segment.SuggestedNodeType.ToString(),
            OriginalTranscript = segment.Text,
            CreatedAt = DateTime.UtcNow,
            Tags = segment.Keywords.Take(5).ToList(),
            // Store semantic metadata for network analysis
            SuggestedPatternIds =
            [
                segment.SemanticType.ToString(),
                $"confidence:{segment.ClassificationConfidence:F2}"
            ]
        };
    }
    
    private static void BuildSemanticEdges(List<MindMapNode> nodes, List<ContextualSegment> segments)
    {
        if (nodes.Count == 0) return;

        // 1) Build inverted indices for faster semantic matching
        var nodesByType = new Dictionary<SegmentSemanticType, List<string>>();
        var nodesByKeyword = new Dictionary<string, List<string>>();

        for (var i = 0; i < nodes.Count; i++)
        {
            var nodeId = nodes[i].Id;
            var seg = segments[i];

            // Type index
            if (!nodesByType.TryGetValue(seg.SemanticType, out var typeList))
            {
                typeList = new List<string>();
                nodesByType[seg.SemanticType] = typeList;
            }
            typeList.Add(nodeId);

            // Keyword index
            foreach (var kw in seg.Keywords)
            {
                if (!nodesByKeyword.TryGetValue(kw, out var kwList))
                {
                    kwList = new List<string>();
                    nodesByKeyword[kw] = kwList;
                }
                kwList.Add(nodeId);
            }
        }

        // 2) Connect nodes using indices to avoid O(n^2)
        var edgeSet = new HashSet<(string From, string To)>();

        for (var i = 0; i < nodes.Count; i++)
        {
            var currentNode = nodes[i];
            
            // Connect to temporally adjacent segment (dialogue flow)
            if (i > 0)
            {
                var prevNodeId = nodes[i - 1].Id;
                nodes[i - 1].Edges.Add(new NodeEdge
                {
                    FromNodeId = prevNodeId,
                    ToNodeId = currentNode.Id,
                    Label = "follows",
                    Type = "temporal"
                });
                edgeSet.Add((prevNodeId, currentNode.Id));
            }

            var currentSegment = segments[i];

            // Find semantic matches
            var semanticTargets = new HashSet<string>();

            if (nodesByType.TryGetValue(currentSegment.SemanticType, out var sameTypeNodes))
            {
                foreach (var targetId in sameTypeNodes)
                    semanticTargets.Add(targetId);
            }

            foreach (var kw in currentSegment.Keywords)
            {
                if (nodesByKeyword.TryGetValue(kw, out var matchNodes))
                {
                    foreach (var targetId in matchNodes)
                        semanticTargets.Add(targetId);
                }
            }

            // Generate edges (avoiding self-edges and duplicates)
            foreach (var targetId in semanticTargets)
            {
                if (targetId == currentNode.Id) continue;
                
                // Keep a consistent ordering for unordered semantic edges to avoid reverse-duplicate counting
                var a = string.Compare(currentNode.Id, targetId, StringComparison.Ordinal) < 0 ? currentNode.Id : targetId;
                var b = a == currentNode.Id ? targetId : currentNode.Id;

                if (edgeSet.Add((a, b)))
                {
                    currentNode.Edges.Add(new NodeEdge
                    {
                        FromNodeId = currentNode.Id,
                        ToNodeId = targetId,
                        Label = "related",
                        Type = "semantic"
                    });
                }
            }
        }
    }

    #region Private Methods

    private List<TranscriptSegment> ConsolidateSegments(List<TranscriptSegment> segments)
    {
        // Philosophy: Respect natural dialogue boundaries. 
        // Segments are naturally punctuated by intonation/pause from transcription.
        // We preserve individual segments as atomic units of thought.
        // Only filter out empty/whitespace segments, never merge.
        var consolidated = (from segment in segments.OrderBy(s => s.StartTime)
        where !string.IsNullOrWhiteSpace(segment.Text) && segment.Text.Trim().Length >= 3
        select new TranscriptSegment
        {
            Id = segment.Id,
            Text = segment.Text.Trim(),
            StartTime = segment.StartTime,
            EndTime = segment.EndTime,
            SpeakerId = segment.SpeakerId,
            Confidence = segment.Confidence,
            IsInterim = segment.IsInterim
        }).ToList();

        _logger.LogInformation("Preserved {Count} individual segments as atomic thought units", consolidated.Count);
        return consolidated;
    }

    private async Task<List<ContextualSegment>> ClassifySegmentsAsync(List<TranscriptSegment> segments)
    {
        // CPU-bound synchronous classification runs in parallel; corpus is read-only/thread-safe
        var contextualSegments = segments
            .AsParallel()
            .AsOrdered()
            .Select(ClassifySingleSegment)
            .ToList();

        // Enhance with AI if available
        if (!string.IsNullOrEmpty(_geminiApiKey) && segments.Count > 0)
        {
            await EnhanceClassificationsWithAiAsync(contextualSegments);
        }

        return contextualSegments;
    }

    private ContextualSegment ClassifySingleSegment(TranscriptSegment segment)
    {
        var text = segment.Text.ToLowerInvariant();
        var scores = new Dictionary<SegmentSemanticType, double>();

        // Pattern-based classification
        foreach (var (type, patterns) in _semanticPatterns)
        {
            var typeScore = 0.0;
            foreach (var (pattern, weight) in patterns)
            {
                var matches = pattern.Matches(text);
                typeScore += matches.Count * weight;
            }
            scores[type] = Math.Min(typeScore, 1.0);
        }

        // Corpus-based classification using dictionary lookup
        var corpusScores = _corpus.ClassifyText(segment.Text);
        foreach (var (type, score) in corpusScores)
        {
            if (!scores.TryAdd(type, score))
                scores[type] = Math.Max(scores[type], score);
        }

        // Determine primary type
        var primaryType = scores.OrderByDescending(s => s.Value).FirstOrDefault();
        var semanticType = primaryType.Value > 0.3 ? primaryType.Key : SegmentSemanticType.Informational;

        // Map to mind map node type
        var suggestedNodeType = MapSemanticTypeToNodeType(semanticType);

        // Extract keywords
        var keywords = ExtractKeywords(segment.Text);

        // Generate proposed title
        var proposedTitle = GenerateProposedTitle(segment.Text, semanticType);

        // Determine if key insight (threshold 0.8 matches ExtractKeyInsights filter)
        var isKeyInsight = primaryType.Value > 0.8 ||
                           semanticType == SegmentSemanticType.Decision ||
                           semanticType == SegmentSemanticType.Action ||
                           semanticType == SegmentSemanticType.Risk ||
                           semanticType == SegmentSemanticType.Idea ||
                           semanticType == SegmentSemanticType.Insight ||
                           semanticType == SegmentSemanticType.Theory ||
                           semanticType == SegmentSemanticType.Hypothesis ||
                           semanticType == SegmentSemanticType.Principle ||
                           semanticType == SegmentSemanticType.Synthesis ||
                           semanticType == SegmentSemanticType.Concept;

        return new ContextualSegment
        {
            Id = segment.Id,
            Text = segment.Text,
            StartTime = segment.StartTime,
            EndTime = segment.EndTime,
            SpeakerId = segment.SpeakerId,
            SemanticType = semanticType,
            ClassificationConfidence = primaryType.Value,
            Keywords = keywords,
            SuggestedNodeType = suggestedNodeType,
            ProposedTitle = proposedTitle,
            IsKeyInsight = isKeyInsight
        };
    }

    private async Task EnhanceClassificationsWithAiAsync(List<ContextualSegment> segments)
    {
        try
        {
            // Batch segments for AI enhancement
            var batchSize = 5;
            for (var i = 0; i < segments.Count; i += batchSize)
            {
                var batch = segments.Skip(i).Take(batchSize).ToList();
                await EnhanceBatchWithAiAsync(batch);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI enhancement failed, using local classification only");
        }
    }

    private async Task EnhanceBatchWithAiAsync(List<ContextualSegment> batch)
    {
        var prompt = BuildEnhancementPrompt(batch);
        // Use CallAiAsync to enable Ollama → Gemini fallback chain
        var response = await CallAiAsync(prompt);

        if (!string.IsNullOrEmpty(response))
        {
            ApplyAiEnhancements(batch, response);
        }
    }

    private string BuildEnhancementPrompt(List<ContextualSegment> segments)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze these transcript segments and provide semantic classification enhancements.");
        sb.AppendLine();
        sb.AppendLine("Segments:");
        
        for (var i = 0; i < segments.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {segments[i].Text}");
        }
        
        sb.AppendLine();
        sb.AppendLine("Respond in JSON format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"enhancements\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"segmentIndex\": 1,");
        sb.AppendLine("      \"semanticType\": \"Decision|Action|Requirement|Risk|Idea|Informational\",");
        sb.AppendLine("      \"confidence\": 0.85,");
        sb.AppendLine("      \"theme\": \"Brief theme description\",");
        sb.AppendLine("      \"isKeyInsight\": true,");
        sb.AppendLine("      \"proposedTitle\": \"Concise title (max 50 chars)\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private void ApplyAiEnhancements(List<ContextualSegment> segments, string response)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(response);
            if (jsonDoc.RootElement.TryGetProperty("enhancements", out var enhancements))
            {
                foreach (var enhancement in enhancements.EnumerateArray())
                {
                    var index = enhancement.GetProperty("segmentIndex").GetInt32() - 1;
                    if (index >= 0 && index < segments.Count)
                    {
                        var segment = segments[index];
                        
                        if (enhancement.TryGetProperty("semanticType", out var typeElement) &&
                            Enum.TryParse<SegmentSemanticType>(typeElement.GetString(), true, out var parsedType))
                        {
                            segment.SemanticType = parsedType;
                            segment.SuggestedNodeType = MapSemanticTypeToNodeType(parsedType);
                        }

                        if (enhancement.TryGetProperty("confidence", out var confElement))
                        {
                            segment.ClassificationConfidence = Math.Max(segment.ClassificationConfidence, confElement.GetDouble());
                        }

                        if (enhancement.TryGetProperty("theme", out var themeElement))
                        {
                            segment.Theme = themeElement.GetString();
                        }

                        if (enhancement.TryGetProperty("isKeyInsight", out var insightElement))
                        {
                            segment.IsKeyInsight = insightElement.GetBoolean();
                        }

                        if (enhancement.TryGetProperty("proposedTitle", out var titleElement))
                        {
                            segment.ProposedTitle = titleElement.GetString();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply AI enhancements");
        }
    }

    private List<ContextualTheme> ExtractThemes(List<ContextualSegment> segments)
    {
        var themes = new List<ContextualTheme>();
        
        // Group segments by semantic type and keyword overlap
        var groupedByType = segments.GroupBy(s => s.SemanticType);
        
        foreach (var group in groupedByType.Where(g => g.Count() >= 2))
        {
            // Find common keywords within this type
            var allKeywords = group.SelectMany(s => s.Keywords).ToList();
            var commonKeywords = allKeywords
                .GroupBy(k => k)
                .Where(g => g.Count() >= 2)
                .Select(g => g.Key)
                .ToList();

            if (commonKeywords.Count != 0)
            {
                var theme = new ContextualTheme
                {
                    Name = GenerateThemeName(group.Key, commonKeywords),
                    Keywords = commonKeywords,
                    SegmentIds = group.Select(s => s.Id).ToList(),
                    RelevanceScore = group.Average(s => s.ClassificationConfidence),
                    SuggestedNodeType = MapSemanticTypeToNodeType(group.Key),
                    IsTopLevelTheme = group.Key == SegmentSemanticType.Decision ||
                                     group.Key == SegmentSemanticType.Action ||
                                     group.Key == SegmentSemanticType.Requirement ||
                                     group.Key == SegmentSemanticType.Risk
                };
                themes.Add(theme);
            }
        }

        // Also cluster by temporal proximity for conversational themes
        var temporalThemes = ExtractTemporalThemes(segments);
        themes.AddRange(temporalThemes);

        return themes.OrderByDescending(t => t.RelevanceScore).ToList();
    }

    private List<ContextualTheme> ExtractTemporalThemes(List<ContextualSegment> segments)
    {
        var themes = new List<ContextualTheme>();
        var orderedSegments = segments.OrderBy(s => s.StartTime).ToList();
        
        // Find clusters of related segments within time windows
        var window = TimeSpan.FromMinutes(2);
        var currentCluster = new List<ContextualSegment>();
        ContextualSegment? lastSegment = null;

        foreach (var segment in orderedSegments)
        {
            if (lastSegment == null || (segment.StartTime - lastSegment.EndTime) < window)
            {
                currentCluster.Add(segment);
            }
            else
            {
                // Process current cluster
                if (currentCluster.Count >= 3)
                {
                    var theme = CreateThemeFromCluster(currentCluster);
                    if (theme != null) themes.Add(theme);
                }
                currentCluster = [segment];
            }
            lastSegment = segment;
        }

        // Process final cluster
        if (currentCluster.Count >= 3)
        {
            var theme = CreateThemeFromCluster(currentCluster);
            if (theme != null) themes.Add(theme);
        }

        return themes;
    }

    private ContextualTheme? CreateThemeFromCluster(List<ContextualSegment> cluster)
    {
        var keywords = cluster.SelectMany(s => s.Keywords).ToList();
        var commonKeywords = keywords
            .GroupBy(k => k)
            .Where(g => g.Count() >= 2)
            .Select(g => g.Key)
            .Take(3)
            .ToList();

        if (commonKeywords.Count == 0) return null;

        return new ContextualTheme
        {
            Name = string.Join(" ", commonKeywords),
            Keywords = commonKeywords,
            SegmentIds = cluster.Select(s => s.Id).ToList(),
            RelevanceScore = cluster.Average(s => s.ClassificationConfidence),
            SuggestedNodeType = cluster.OrderByDescending(s => s.ClassificationConfidence).First().SuggestedNodeType,
            IsTopLevelTheme = false
        };
    }

    private static string GenerateThemeName(SegmentSemanticType type, List<string> keywords)
    {
        var typeName = type.ToString();
        var keywordStr = string.Join(", ", keywords.Take(2));
        return $"{typeName}: {keywordStr}";
    }

    private static List<ContextualSegment> ExtractKeyInsights(List<ContextualSegment> segments)
    {
        return segments
            .Where(s => s.IsKeyInsight || s.ClassificationConfidence > 0.8)
            .OrderByDescending(s => s.ClassificationConfidence)
            .Take(10)
            .ToList();
    }


    private static MindMapNodeType MapSemanticTypeToNodeType(SegmentSemanticType semanticType)
    {
        return semanticType switch
        {
            SegmentSemanticType.Decision => MindMapNodeType.Decision,
            SegmentSemanticType.Action => MindMapNodeType.Action,
            SegmentSemanticType.Requirement => MindMapNodeType.Requirement,
            SegmentSemanticType.Constraint => MindMapNodeType.Requirement,
            SegmentSemanticType.Risk => MindMapNodeType.Risk,
            SegmentSemanticType.Problem => MindMapNodeType.Risk,
            SegmentSemanticType.Opportunity => MindMapNodeType.Idea,
            SegmentSemanticType.Idea => MindMapNodeType.Idea,
            SegmentSemanticType.Solution => MindMapNodeType.Idea,
            SegmentSemanticType.Goal => MindMapNodeType.Goal,
            SegmentSemanticType.Timeline => MindMapNodeType.Timeline,
            SegmentSemanticType.Resource => MindMapNodeType.Resource,
            SegmentSemanticType.Question => MindMapNodeType.Question,
            SegmentSemanticType.QuestionFundamental => MindMapNodeType.Question,
            
            // Intellectual/Philosophical types
            SegmentSemanticType.Theory => MindMapNodeType.Idea,
            SegmentSemanticType.Hypothesis => MindMapNodeType.Idea,
            SegmentSemanticType.Principle => MindMapNodeType.Requirement,
            SegmentSemanticType.Concept => MindMapNodeType.Idea,
            SegmentSemanticType.Paradigm => MindMapNodeType.Idea,
            SegmentSemanticType.Framework => MindMapNodeType.Idea,
            SegmentSemanticType.Analysis => MindMapNodeType.Note,
            SegmentSemanticType.Synthesis => MindMapNodeType.Idea,
            SegmentSemanticType.Insight => MindMapNodeType.Idea,
            SegmentSemanticType.Reflection => MindMapNodeType.Note,
            SegmentSemanticType.Argument => MindMapNodeType.Note,
            SegmentSemanticType.Evidence => MindMapNodeType.Note,
            SegmentSemanticType.Counterpoint => MindMapNodeType.Risk,
            SegmentSemanticType.Implication => MindMapNodeType.Note,
            SegmentSemanticType.Connection => MindMapNodeType.Idea,
            SegmentSemanticType.Pattern => MindMapNodeType.Idea,
            SegmentSemanticType.Abstraction => MindMapNodeType.Idea,
            
            SegmentSemanticType.Metric => MindMapNodeType.Note,
            SegmentSemanticType.Technical => MindMapNodeType.Note,
            SegmentSemanticType.Strategic => MindMapNodeType.Note,
            SegmentSemanticType.Informational => MindMapNodeType.Note,
            _ => MindMapNodeType.Note
        };
    }

    private List<string> ExtractKeywords(string text)
    {
        // Simple keyword extraction: nouns and important terms
        var words = text.ToLowerInvariant()
            .Split([' ', ',', '.', '!', '?', ';', ':'], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !_corpus.IsStopWord(w))
            .Distinct()
            .Take(5)
            .ToList();

        return words;
    }

    private static string GenerateProposedTitle(string text, SegmentSemanticType type)
    {
        var prefix = type switch
        {
            SegmentSemanticType.Decision => "[Decision] ",
            SegmentSemanticType.Action => "[Action] ",
            SegmentSemanticType.Requirement => "[Req] ",
            SegmentSemanticType.Risk => "[Risk] ",
            SegmentSemanticType.Goal => "[Goal] ",
            // Intellectual/Philosophical prefixes
            SegmentSemanticType.Theory => "[Theory] ",
            SegmentSemanticType.Hypothesis => "[Hypothesis] ",
            SegmentSemanticType.Principle => "[Principle] ",
            SegmentSemanticType.Insight => "[Insight] ",
            SegmentSemanticType.Analysis => "[Analysis] ",
            SegmentSemanticType.Synthesis => "[Synthesis] ",
            SegmentSemanticType.QuestionFundamental => "[Question] ",
            SegmentSemanticType.Argument => "[Argument] ",
            _ => ""
        };

        var cleanText = text.Length > 50 ? text[..47].Trim() + "..." : text.Trim();
        return prefix + cleanText;
    }

    private static double CalculateOverallConfidence(List<ContextualSegment> segments)
    {
        if (segments.Count == 0) return 0;
        return segments.Average(s => s.ClassificationConfidence);
    }

    private async Task<string?> CallGeminiAsync(string prompt)
    {
        if (string.IsNullOrEmpty(_geminiApiKey)) return null;

        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = 2000
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiModel}:generateContent?key={_geminiApiKey}";

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Gemini API error: {Error}", error);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

            return result.GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            return null;
        }
    }

    private async Task<string?> CallOllamaAsync(string prompt)
    {
        if (!_useLocalLlm) return null;

        try
        {
            var requestBody = new
            {
                model = _ollamaModel,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.2
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_ollamaUrl}/api/generate";

            _logger.LogInformation("Calling Ollama at {Url} with model {Model}", url, _ollamaModel);
            
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Ollama API error: {Error}", error);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

            return result.GetProperty("response").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling Ollama API at {Url}", _ollamaUrl);
            return null;
        }
    }

    private async Task<string?> CallAiAsync(string prompt)
    {
        // Try Ollama first if enabled
        if (_useLocalLlm)
        {
            var ollamaResult = await CallOllamaAsync(prompt);
            if (!string.IsNullOrEmpty(ollamaResult))
            {
                _logger.LogInformation("Successfully used Ollama local LLM for enhancement");
                return ollamaResult;
            }
        }

        // Fall back to Gemini if configured
        if (!string.IsNullOrEmpty(_geminiApiKey))
        {
            return await CallGeminiAsync(prompt);
        }

        return null;
    }

    private static Dictionary<SegmentSemanticType, List<(Regex Pattern, double Weight)>> InitializeSemanticPatterns()
    {
        var patterns = new Dictionary<SegmentSemanticType, List<(Regex Pattern, double Weight)>>
        {
            [SegmentSemanticType.Decision] = new List<(Regex, double)>
            {
                (new Regex(@"\b(decided|decision|agreed|agreement|consensus|finalized|concluded)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(settled on|approved|confirmed|voted|unanimous|signed off)\b", RegexOptions.IgnoreCase), 0.85),
                (new Regex(@"\b(we'll go with|let's go with|chosen|selected|picked)\b", RegexOptions.IgnoreCase), 0.8),
                (new Regex(@"\b(green light|go ahead|moving forward with|proceed with)\b", RegexOptions.IgnoreCase), 0.75)
            },

            [SegmentSemanticType.Action] = new List<(Regex, double)>
            {
                (new Regex(@"\b(will|shall|need to|needs to|must|should)\b", RegexOptions.IgnoreCase), 0.8),
                (new Regex(@"\b(going to|plan to| tasked |assigned to|responsible for)\b", RegexOptions.IgnoreCase), 0.85),
                (new Regex(@"\b(take care of|follow up|follow-up|get back to|look into)\b", RegexOptions.IgnoreCase), 0.8),
                (new Regex(@"\b(investigate|prepare|draft|create|develop|implement|deploy)\b", RegexOptions.IgnoreCase), 0.75)
            },

            [SegmentSemanticType.Requirement] = new List<(Regex, double)>
            {
                (new Regex(@"\b(requirement|requirements|require|requires|specification|spec)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(functional|non-functional|constraint|limitations|mandatory|essential)\b", RegexOptions.IgnoreCase), 0.8),
                (new Regex(@"\b(critical|necessary|expected to|supposed to|has to|have to)\b", RegexOptions.IgnoreCase), 0.75),
                (new Regex(@"\b(capability|feature|functionality|acceptance criteria|definition of done)\b", RegexOptions.IgnoreCase), 0.8)
            },

            [SegmentSemanticType.Risk] = new List<(Regex, double)>
            {
                (new Regex(@"\b(risk|risks|risky|issue|issues|problem|problems|concern|concerns)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(worried|worry|danger|threat|vulnerability|exposure|consequence)\b", RegexOptions.IgnoreCase), 0.8),
                (new Regex(@"\b(blocker|blocking|blocked|obstacle|bottleneck|challenge|difficult)\b", RegexOptions.IgnoreCase), 0.85),
                (new Regex(@"\b(uncertainty|unclear|ambiguous|untested|unknown|may fail|might break)\b", RegexOptions.IgnoreCase), 0.75)
            },

            [SegmentSemanticType.Goal] = new List<(Regex, double)>
            {
                (new Regex(@"\b(goal|goals|objective|objectives|target|aim|mission|vision)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(purpose|intent|aspiration|key result|okr|kpi|metric|outcome)\b", RegexOptions.IgnoreCase), 0.8),
                (new Regex(@"\b(deliverable|milestone|achievement|accomplish|reach|achieve|succeed)\b", RegexOptions.IgnoreCase), 0.75)
            },

            [SegmentSemanticType.Timeline] = new List<(Regex, double)>
            {
                (new Regex(@"\b(timeline|schedule|deadline|due date|milestone|phase|sprint)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(iteration|release|launch|deploy|rollout|go-live|start date|end date)\b", RegexOptions.IgnoreCase), 0.8),
                (new Regex(@"\b(duration|timeframe|period|quarter|q1|q2|q3|q4)\b", RegexOptions.IgnoreCase), 0.75),
                (new Regex(@"\b(this week|next week|this month|next month|by end of|no later than)\b", RegexOptions.IgnoreCase), 0.8)
            },

            [SegmentSemanticType.Question] = new List<(Regex, double)>
            {
                (new Regex(@"\?", RegexOptions.None), 0.8),
                (new Regex(@"\b(what is|what are|how do|how does|how can|how will|why is|why does)\b", RegexOptions.IgnoreCase), 0.75),
                (new Regex(@"\b(when will|where is|who will|who is|which|is there|are there|can we|could we)\b", RegexOptions.IgnoreCase), 0.7),
                (new Regex(@"\b(should we|would it|clarify|clarification|not sure|unclear|don't understand)\b", RegexOptions.IgnoreCase), 0.75)
            },

            [SegmentSemanticType.Idea] = new List<(Regex, double)>
            {
                (new Regex(@"\b(idea|ideas|suggestion|suggest|propose|proposal|consider|thinking)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(what if|how about|maybe|perhaps|possibly|alternative|option|approach)\b", RegexOptions.IgnoreCase), 0.8),
                (new Regex(@"\b(concept|design|architecture|solution|solve|address|improvement|enhancement)\b", RegexOptions.IgnoreCase), 0.75),
                (new Regex(@"\b(feature request|brainstorming|explore|experiment|pilot|proof of concept|poc|prototype|mvp)\b", RegexOptions.IgnoreCase), 0.8)
            },

            [SegmentSemanticType.Resource] = new List<(Regex, double)>
            {
                (new Regex(@"\b(resource|resources|budget|funding|cost|spend|investment|allocation)\b", RegexOptions.IgnoreCase), 0.85),
                (new Regex(@"\b(headcount|capacity|bandwidth|availability|manpower|staffing|team size)\b", RegexOptions.IgnoreCase), 0.85),
                (new Regex(@"\b(tool|tools|library|libraries|framework|platform|infrastructure|environment)\b", RegexOptions.IgnoreCase), 0.75),
                (new Regex(@"\b(time|timeline|schedule|deadline|effort|estimate|velocity)\b", RegexOptions.IgnoreCase), 0.7)
            },

            [SegmentSemanticType.Technical] = new List<(Regex, double)>
            {
                (new Regex(@"\b(technical|technology|architecture|design pattern|algorithm|data structure)\b", RegexOptions.IgnoreCase), 0.85),
                (new Regex(@"\b(implementation|code|coding|programming|function|method|class|interface)\b", RegexOptions.IgnoreCase), 0.8),
                (new Regex(@"\b(module|component|system|backend|frontend|database|server|client|api)\b", RegexOptions.IgnoreCase), 0.75),
                (new Regex(@"\b(endpoint|rest|graphql|microservice|container|docker|kubernetes|k8s|cloud)\b", RegexOptions.IgnoreCase), 0.8)
            },

            // Intellectual/Philosophical discourse patterns
            [SegmentSemanticType.Theory] = new List<(Regex, double)>
            {
                (new Regex(@"\b(theory|theories|theoretical|framework|model|paradigm|epistemology)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(conceptual|abstraction|abstract|notion|construct)\b", RegexOptions.IgnoreCase), 0.85),
                (new Regex(@"\b(philosophy|philosophical|ontology|metaphysics|axiology)\b", RegexOptions.IgnoreCase), 0.85)
            },

            [SegmentSemanticType.Hypothesis] = new List<(Regex, double)>
            {
                (new Regex(@"\b(hypothesis|hypotheses|conjecture|suppose|assumption|postulate)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(if|then|would|could|might|possibly|likely|probable)\b", RegexOptions.IgnoreCase), 0.75),
                (new Regex(@"\b(test|testing|validate|falsify|verify|experiment)\b", RegexOptions.IgnoreCase), 0.8)
            },

            [SegmentSemanticType.Principle] = new List<(Regex, double)>
            {
                (new Regex(@"\b(principle|principles|axiom|tenet|fundamental|foundation|core)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(guiding|rule|law|maxim|doctrine|canon|dictum)\b", RegexOptions.IgnoreCase), 0.85),
                (new Regex(@"\b(always|never|must|essential|inherent|intrinsic)\b", RegexOptions.IgnoreCase), 0.8)
            },

            [SegmentSemanticType.Synthesis] = new List<(Regex, double)>
            {
                (new Regex(@"\b(synthesis|synthesize|combine|integration|integrate|holistic)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(connect|connection|relate|relationship|interconnected)\b", RegexOptions.IgnoreCase), 0.85),
                (new Regex(@"\b(pattern|emerge|emergent|gestalt|unified|coherence)\b", RegexOptions.IgnoreCase), 0.8)
            },

            [SegmentSemanticType.Analysis] = new List<(Regex, double)>
            {
                (new Regex(@"\b(analysis|analyze|examine|break down|deconstruct|dissect)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(component|element|factor|aspect|dimension|facet)\b", RegexOptions.IgnoreCase), 0.8),
                (new Regex(@"\b(cause|effect|causal|correlation|relationship|dynamic)\b", RegexOptions.IgnoreCase), 0.8)
            },

            [SegmentSemanticType.Insight] = new List<(Regex, double)>
            {
                (new Regex(@"\b(insight|realization|epiphany|breakthrough|discovery|revelation)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(understand|understanding|grasp|comprehend|appreciate|see|recognize)\b", RegexOptions.IgnoreCase), 0.8),
                (new Regex(@"\b(deep|profound|nuanced|subtle|sophisticated|elegant)\b", RegexOptions.IgnoreCase), 0.75)
            },

            [SegmentSemanticType.Reflection] = new List<(Regex, double)>
            {
                (new Regex(@"\b(reflect|reflection|contemplate|contemplation|meditate|ponder)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(consider|consideration|think about|thinking|thought|cogitate)\b", RegexOptions.IgnoreCase), 0.8),
                (new Regex(@"\b(meaning|significance|implication|ramification|consequence)\b", RegexOptions.IgnoreCase), 0.75)
            },

            [SegmentSemanticType.Argument] = new List<(Regex, double)>
            {
                (new Regex(@"\b(argument|argue|reason|reasoning|logic|logical|premise|conclusion)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(because|therefore|thus|hence|since|consequently|as a result)\b", RegexOptions.IgnoreCase), 0.85),
                (new Regex(@"\b(evidence|support|justify|warrant|ground|foundation)\b", RegexOptions.IgnoreCase), 0.8)
            },

            [SegmentSemanticType.QuestionFundamental] = new List<(Regex, double)>
            {
                (new Regex(@"\b(what is|what does it mean|why|how come|fundamental|ultimate)\b", RegexOptions.IgnoreCase), 0.9),
                (new Regex(@"\b(existential|existence|being|nature|essence|purpose|meaning)\b", RegexOptions.IgnoreCase), 0.85),
                (new Regex(@"\b(purpose|telos|why are we|ultimate|final)\b", RegexOptions.IgnoreCase), 0.8)
            }
        };

        return patterns;
    }

    #endregion
}
