using System.Text;
using System.Text.Json;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Compost.Core.Services;

public class AiIntegrationService(
    ILogger<AiIntegrationService> logger,
    IConfiguration configuration,
    HttpClient httpClient)
    : IAiIntegrationService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly string? _geminiApiKey = configuration["Compost:Gemini:ApiKey"];
    private readonly string? _geminiModel = configuration["Compost:Gemini:Model"] ?? "gemini-2.0-flash";
    private readonly ContextCorpusDictionary _corpus = new ContextCorpusDictionary();
    private static readonly char[] Separator = ['.', '!', '?'];

    public async Task<int> EstimateStoryPointsAsync(string requirement, string? context = null)
    {
        if (string.IsNullOrEmpty(_geminiApiKey))
        {
            return GetFallbackStoryPointEstimate(requirement);
        }

        try
        {
            var prompt = BuildStoryPointPrompt(requirement, context);
            var response = await CallGeminiAsync(prompt);
            
            // Parse the response to extract the story point estimate
            var storyPoints = ParseStoryPointsFromResponse(response);
            return storyPoints;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error estimating story points with Gemini AI");
            return GetFallbackStoryPointEstimate(requirement);
        }
    }

    public async Task<List<ArchitecturalPattern>> RecognizePatternsAsync(string code, string language)
    {
        if (string.IsNullOrEmpty(_geminiApiKey))
        {
            return GetFallbackPatternRecognition(code);
        }

        try
        {
            var prompt = BuildPatternRecognitionPrompt(code, language);
            var response = await CallGeminiAsync(prompt);
            
            var patterns = ParsePatternsFromResponse(response);
            return patterns;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recognizing patterns with Gemini AI");
            return GetFallbackPatternRecognition(code);
        }
    }

    public async Task<string> GenerateCodeSuggestionAsync(string requirement, string language, string? context = null)
    {
        if (string.IsNullOrEmpty(_geminiApiKey))
        {
            return GetFallbackCodeSuggestion(requirement, language);
        }

        try
        {
            var prompt = BuildCodeSuggestionPrompt(requirement, language, context);
            var response = await CallGeminiAsync(prompt);
            
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating code suggestion with Gemini AI");
            return GetFallbackCodeSuggestion(requirement, language);
        }
    }

    public async Task<List<ActionItem>> ExtractActionItemsFromTextAsync(string text, string? context = null)
    {
        if (string.IsNullOrEmpty(_geminiApiKey))
        {
            return GetFallbackActionItemExtraction(text);
        }

        try
        {
            var prompt = BuildActionItemExtractionPrompt(text, context);
            var response = await CallGeminiAsync(prompt);
            
            var actionItems = ParseActionItemsFromResponse(response);
            return actionItems;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting action items with Gemini AI");
            return GetFallbackActionItemExtraction(text);
        }
    }

    public async Task<List<MindMapNode>> ExtractMindMapNodesFromTextAsync(string text, string? context = null)
    {
        if (string.IsNullOrEmpty(_geminiApiKey))
        {
            return GetFallbackMindMapNodeExtraction(text);
        }

        try
        {
            var prompt = BuildMindMapNodeExtractionPrompt(text, context);
            var response = await CallGeminiAsync(prompt);
            
            var mindMapNodes = ParseMindMapNodesFromResponse(response);
            return mindMapNodes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting mind map nodes with Gemini AI");
            return GetFallbackMindMapNodeExtraction(text);
        }
    }

    private async Task<string> CallGeminiAsync(string prompt)
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
                        new { text = "You are an expert software architect and developer assistant. Provide concise, actionable responses.\n\n" + prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 1000
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiModel}:generateContent?key={_geminiApiKey}";
        
        var response = await httpClient.PostAsync(url, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API call failed: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
        
        return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
    }

    private string BuildStoryPointPrompt(string requirement, string? context)
    {
        var contextInfo = string.IsNullOrEmpty(context) ? "" : $"Project: {context}\n";
        return $"""
            {contextInfo}Estimate the story points for this requirement using Fibonacci sequence (1, 2, 3, 5, 8, 13, 21).
            
            Requirement: {requirement}
            
            Consider:
            - Complexity of implementation
            - Amount of work required
            - Uncertainty and risk
            - Dependencies on other components
            
            Respond with just the number (e.g., "5").
            """;
    }

    private string BuildPatternRecognitionPrompt(string code, string language)
    {
        return $"Analyze this {language} code and identify any architectural patterns present.\n\n" +
               $"Code:\n```{language}\n{code}\n```\n\n" +
               "Identify patterns like:\n" +
               "- Singleton\n" +
               "- Factory\n" +
               "- Repository\n" +
               "- Observer\n" +
               "- Strategy\n" +
               "- Decorator\n" +
               "- Command\n" +
               "- Adapter\n" +
               "- Facade\n" +
               "- Template Method\n\n" +
               "Respond in JSON format:\n" +
               "{\n" +
               "  \"patterns\": [\n" +
               "    {\n" +
               "      \"name\": \"Pattern Name\",\n" +
               "      \"description\": \"Brief description\",\n" +
               "      \"confidence\": 0.8\n" +
               "    }\n" +
               "  ]\n" +
               "}";
    }

    private string BuildCodeSuggestionPrompt(string requirement, string language, string? context)
    {
        var contextInfo = string.IsNullOrEmpty(context) ? "" : $"Project: {context}\n";
        return $"""
            {contextInfo}Generate a code suggestion in {language} for this requirement.
            
            Requirement: {requirement}
            
            Provide clean, well-commented code that follows best practices.
            Keep it concise but complete enough to be useful.
            """;
    }

    private string BuildActionItemExtractionPrompt(string text, string? context)
    {
        var contextInfo = string.IsNullOrEmpty(context) ? "" : $"Project: {context}\n";
        return $"{contextInfo}Extract action items from this transcript of a meeting. Look for tasks, commitments, responsibilities, or next steps.\n\n" +
               $"Text:\n{text}\n\n" +
               "Respond in JSON format:\n" +
               "{\n" +
               "  \"actionItems\": [\n" +
               "    {\n" +
               "      \"title\": \"Action item title\",\n" +
               "      \"description\": \"Brief context of why this was assigned\",\n" +
               "      \"priority\": \"High|Medium|Low\",\n" +
               "      \"sourceText\": \"The exact sentence or quote from the transcript that generated this action item\"\n" +
               "    }\n" +
               "  ]\n" +
               "}";
    }

    private string BuildMindMapNodeExtractionPrompt(string text, string? context)
    {
        var contextInfo = string.IsNullOrEmpty(context) ? "" : $"Project: {context}\n";
        return $"{contextInfo}Extract significant, cohesive statements and verbatim quotes from this meeting transcript. These will be used to build a mind map. Capture complete thoughts exactly as they appear.\n\n" +
               "IMPORTANT: Join fragmented spoken snippets into complete, humanly contextual thoughts. Consider the surrounding dialogue to ensure subsequent snippets are familiarized and meaningful.\n\n" +
               $"Text:\n{text}\n\n" +
               "Respond in JSON format:\n" +
               "{\n" +
               "  \"nodes\": [\n" +
               "    {\n" +
               "      \"title\": \"Short Title (max 60 chars)\",\n" +
               "      \"description\": \"The full verbatim sentence or statement\",\n" +
               "      \"type\": \"Idea|Decision|Requirement|Risk|Note|Action\",\n" +
               "      \"sourceText\": \"The exact text from the transcript that generated this node\"\n" +
               "    }\n" +
               "  ]\n" +
               "}";
    }

    private int ParseStoryPointsFromResponse(string response)
    {
        var cleanResponse = response.Trim();
        
        // Try to extract a number from the response
        var numbers = new[] { "1", "2", "3", "5", "8", "13", "21" };
        
        foreach (var number in numbers.OrderByDescending(n => n))
        {
            if (cleanResponse.Contains(number))
            {
                return int.Parse(number);
            }
        }
        
        // Default to 3 if no clear number found
        return 3;
    }

    private List<ArchitecturalPattern> ParsePatternsFromResponse(string response)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(response);
            var patterns = new List<ArchitecturalPattern>();
            
            if (jsonDoc.RootElement.TryGetProperty(nameof(patterns), out var patternsElement))
            {
                foreach (var patternElement in patternsElement.EnumerateArray())
                {
                    var pattern = new ArchitecturalPattern
                    {
                        Name = patternElement.GetProperty("name").GetString() ?? "Unknown",
                        WhenToUse = patternElement.GetProperty("description").GetString() ?? "",
                        HowItWorks = "Pattern detected in code analysis",
                        Gotchas = [],
                        Keywords = [],
                        SuccessScore = patternElement.TryGetProperty("confidence", out var confidence) ? confidence.GetSingle() : 0.5f
                    };
                    patterns.Add(pattern);
                }
            }
            
            return patterns;
        }
        catch
        {
            return [];
        }
    }

    private List<ActionItem> ParseActionItemsFromResponse(string response)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(response);
            var actionItems = new List<ActionItem>();
            
            if (jsonDoc.RootElement.TryGetProperty("actionItems", out var itemsElement))
            {
                foreach (var itemElement in itemsElement.EnumerateArray())
                {
                    var actionItem = new ActionItem
                    {
                        Title = itemElement.GetProperty("title").GetString() ?? "",
                        Description = itemElement.GetProperty("description").GetString() ?? "",
                        Priority = itemElement.GetProperty("priority").GetString() ?? "Medium",
                        OriginalTranscript = itemElement.TryGetProperty("sourceText", out var sourceText) ? sourceText.GetString() : null,
                        Status = "Open",
                        CreatedAt = DateTime.UtcNow
                    };
                    actionItems.Add(actionItem);
                }
            }
            
            return actionItems;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing action items from AI response");
            return [];
        }
    }

    private List<MindMapNode> ParseMindMapNodesFromResponse(string response)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(response);
            var nodes = new List<MindMapNode>();
            
            if (jsonDoc.RootElement.TryGetProperty("nodes", out var itemsElement))
            {
                foreach (var itemElement in itemsElement.EnumerateArray())
                {
                    var typeStr = itemElement.GetProperty("type").GetString() ?? "Note";
                    if (!Enum.TryParse<MindMapNodeType>(typeStr, true, out var nodeType))
                    {
                        nodeType = MindMapNodeType.Note;
                    }

                    var node = new MindMapNode
                    {
                        Title = itemElement.GetProperty("title").GetString() ?? "",
                        Description = itemElement.GetProperty("description").GetString() ?? "",
                        NodeType = nodeType.ToString(),
                        OriginalTranscript = itemElement.TryGetProperty("sourceText", out var sourceText) ? sourceText.GetString() : null,
                        CreatedAt = DateTime.UtcNow
                    };
                    nodes.Add(node);
                }
            }
            
            return nodes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing mind map nodes from AI response");
            return [];
        }
    }

    // Fallback methods when AI is not available
    private int GetFallbackStoryPointEstimate(string requirement)
    {
        // Simple keyword-based estimation
        var keywords = new Dictionary<string, int>
        {
            { "simple", 1 }, { "basic", 1 }, { "easy", 1 },
            { "small", 2 }, { "minor", 2 }, { "quick", 2 },
            { "medium", 3 }, { "moderate", 3 }, { "standard", 3 },
            { "complex", 5 }, { "difficult", 5 }, { "hard", 5 },
            { "large", 8 }, { "major", 8 }, { "significant", 8 },
            { "very complex", 13 }, { "extremely", 13 }, { "huge", 13 },
            { "epic", 21 }, { "massive", 21 }
        };

        var lowerRequirement = requirement.ToLower();
        foreach (var keyword in keywords.OrderByDescending(kvp => kvp.Key.Length))
        {
            if (lowerRequirement.Contains(keyword.Key))
            {
                return keyword.Value;
            }
        }

        return 3; // Default to medium
    }

    private static List<ArchitecturalPattern> GetFallbackPatternRecognition(string code)
    {
        var patterns = new List<ArchitecturalPattern>();
        
        // Simple pattern detection based on keywords
        var lowerCode = code.ToLower();
        
        if (lowerCode.Contains("singleton") || lowerCode.Contains("static instance"))
        {
            var item = new ArchitecturalPattern
            {
                Name = "Singleton",
                WhenToUse = "Ensure only one instance exists",
                HowItWorks = "Private constructor and static instance",
                SuccessScore = 0.7f
            };
            patterns.Add(item);
        }
        
        if (lowerCode.Contains("factory") || lowerCode.Contains("create"))
        {
            var item = new ArchitecturalPattern
            {
                Name = "Factory",
                WhenToUse = "Create objects without specifying exact class",
                HowItWorks = "Factory method or abstract factory",
                SuccessScore = 0.7f
            };
            patterns.Add(item);
        }
        
        if (lowerCode.Contains("repository") || lowerCode.Contains("save") && lowerCode.Contains("find"))
        {
            var item = new ArchitecturalPattern
            {
                Name = "Repository",
                WhenToUse = "Data access layer abstraction",
                HowItWorks = "Repository pattern for data operations",
                SuccessScore = 0.7f
            };
            patterns.Add(item);
        }

        return patterns;
    }

    private List<ActionItem> GetFallbackActionItemExtraction(string text)
    {
        var actionItems = new List<ActionItem>();
        var sentences = text.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        
        var actionKeywords = new[] { 
            "need to", "should", "must", "will", "task", "action item", "todo", "follow up", 
            "assigned to", "follow-up", "due by", "action:", "todo:", "please make sure" 
        };
        
        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > 250) continue;

            var lowerSentence = trimmed.ToLower();
            if (actionKeywords.Any(keyword => lowerSentence.Contains(keyword)) || 
                (lowerSentence.StartsWith("can you") && lowerSentence.Length < 100))
            {
                var item = new ActionItem
                {
                    Title = trimmed.Length > 80 ? trimmed[..77] + "..." : trimmed,
                    Description = trimmed,
                    Priority = lowerSentence.Contains("must") || lowerSentence.Contains("urgent") ? "High" : "Medium",
                    OriginalTranscript = trimmed,
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow
                };
                actionItems.Add(item);
            }
        }
        
        return actionItems;
    }

    private string GetFallbackCodeSuggestion(string requirement, string language)
    {
        return $"// Code suggestion for {requirement}\n// TODO: Implement {requirement} in {language}\n// This is a placeholder suggestion since AI is not configured.";
    }

    private List<MindMapNode> GetFallbackMindMapNodeExtraction(string text)
    {
        var nodes = new List<MindMapNode>();
        if (string.IsNullOrWhiteSpace(text)) return nodes;

        // More robust splitting: by punctuation or by common speaker prefixes if punctuation is missing
        var sentenceSource = System.Text.RegularExpressions.Regex.Split(text, @"[.!?]|\n|(?:Speaker\s+\d+:|Participant\s+\d+:)")
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 15);

        foreach (var sentence in sentenceSource)
        {
            var corpusScores = _corpus.ClassifyText(sentence);
            var primaryType = corpusScores.OrderByDescending(s => s.Value).FirstOrDefault();
            
            // Map SegmentSemanticType to MindMapNodeType
            var nodeType = MindMapNodeType.Idea; // Default
            if (primaryType.Value > 0.3)
            {
                nodeType = primaryType.Key switch
                {
                    SegmentSemanticType.Decision => MindMapNodeType.Decision,
                    SegmentSemanticType.Action => MindMapNodeType.Action,
                    SegmentSemanticType.Requirement => MindMapNodeType.Requirement,
                    SegmentSemanticType.Risk => MindMapNodeType.Risk,
                    SegmentSemanticType.Problem => MindMapNodeType.Risk,
                    SegmentSemanticType.Question => MindMapNodeType.Question,
                    SegmentSemanticType.Idea or SegmentSemanticType.Insight or SegmentSemanticType.Theory => MindMapNodeType.Idea,
                    _ => MindMapNodeType.Note
                };
            }
            // Fallback categorization for long significant sentences
            else if (sentence.Length > 50)
            {
                var lower = sentence.ToLowerInvariant();
                if (lower.Contains(" can ") || lower.Contains(" will "))
                {
                    nodeType = MindMapNodeType.Idea;
                }
                else
                {
                    nodeType = MindMapNodeType.Note; // Default long factual statements to Notes
                }
            }

            nodes.Add(new MindMapNode
            {
                Id = Guid.NewGuid().ToString(),
                Title = sentence.Length > 60 ? sentence[..57].Trim() + "..." : sentence,
                Description = sentence,
                NodeType = nodeType.ToString(),
                OriginalTranscript = sentence,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Final pass: Group short adjacent notes from the same speaker if they form a larger thought
        var finalNodes = new List<MindMapNode>();
        for (var i = 0; i < nodes.Count; i++)
        {
            var current = nodes[i];
            
            // Try to merge with next if they are both notes and from the same speaker segment
            if (i < nodes.Count - 1 && current.NodeType == MindMapNodeType.Note.ToString() && nodes[i+1].NodeType == MindMapNodeType.Note.ToString())
            {
                // Simple heuristic: if total length is reasonable, merge
                if (current.Description.Length + nodes[i+1].Description.Length < 400)
                {
                    current.Description += " " + nodes[i+1].Description;
                    current.OriginalTranscript += " " + nodes[i+1].OriginalTranscript;
                    current.Title = current.Description.Length > 60 ? current.Description[..57].Trim() + "..." : current.Description;
                    i++; // Skip next
                }
            }
            finalNodes.Add(current);
        }

        // Limit to top 40 most significant nodes to allow for more expansive capture
        return finalNodes.Take(40).ToList();
    }
}


