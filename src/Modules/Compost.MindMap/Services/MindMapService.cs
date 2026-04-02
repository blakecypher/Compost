using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Compost.Core.Interfaces;
using Compost.MindMap.Models;
using Microsoft.Extensions.Logging;

namespace Compost.MindMap.Services;

public interface IMindMapService
{
    Task<MindMapCollection> CreateMindMapFromTextAsync(string text, string name, string? workContextId = null);
    Task<MindMapCollection> CreateMindMapFromTranscriptAsync(string transcript, string name, string? workContextId = null);
    Task<MindMapCollection> CreateMindMapFromMeetingNodesAsync(List<Compost.Core.Models.MindMapNode> meetingNodes, string name, string? workContextId = null, string? meetingId = null);
    Task<MindMapCollection> CreateMindMapFromRequirementsAsync(string requirements, string name, string? workContextId = null);
    Task<MindMapCollection> GetMindMapAsync(string id);
    Task<List<MindMapCollection>> GetMindMapsByContextAsync(string workContextId);
    Task SaveMindMapAsync(MindMapCollection mindMap);
    Task UpdateMindMapAsync(MindMapCollection mindMap);
    Task DeleteMindMapAsync(string id);
    Task UpdateNodeAsync(string mapId, MindMapNode node);
    Task<MindMapNode?> AddOrUpdateNodeAsync(string mapId, MindMapNode node);
    Task RemoveNodeAsync(string mapId, string nodeId);
    Task AddNodeAsync(string mapId, MindMapNode node);
    Task DeleteNodeAsync(string mapId, string nodeId);
    Task BulkDeleteNodesAsync(string mapId, string[] nodeIds);
    Task<string> PromoteNodeAsync(string mapId, string nodeId);
    Task<List<MindMapCollection>> GetAllMindMapsAsync();
    Task<string> ExportToJsonAsync(string id);
    Task<string> ExportToMarkdownAsync(string id);
    Task<MindMapCollection> ImportFromJsonAsync(string json);
    Task<MindMapCollection> CloneMindMapAsync(string id);
    Task<MindMapCollection> CreateFromTemplateAsync(string templateName, string name, string? workContextId = null);
}

public class MindMapService : Compost.Core.Interfaces.IMindMapService, IMindMapService
{
    private static readonly Dictionary<string, MindMapCollection> _mindMaps = new();
    private readonly IProjectManager _projectManager;
    private readonly ILogger<MindMapService> _logger;
    private const string DataFilePath = "mindmaps.json";

    public MindMapService(IProjectManager projectManager, ILogger<MindMapService> logger)
    {
        _projectManager = projectManager;
        _logger = logger;
        
        LoadFromJson();
    }

    private void LoadFromJson()
    {
        try {
            if (File.Exists(DataFilePath))
            {
                var json = File.ReadAllText(DataFilePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, MindMapCollection>>(json);
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        _mindMaps[item.Key] = item.Value;
                    }
                    _logger.LogInformation("Loaded {Count} mind maps from persistent storage.", _mindMaps.Count);
                }
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to load mind maps from JSON.");
        }
    }

    private void SaveToJson()
    {
        try {
            var json = JsonSerializer.Serialize(_mindMaps);
            File.WriteAllText(DataFilePath, json);
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to save mind maps to JSON.");
        }
    }

    public async Task UpdateNodeAsync(string mapId, MindMapNode node)
    {
        _logger.LogInformation("UpdateNodeAsync called for Map: {MapId}, Node: {NodeId}", mapId, node.Id);
        if (_mindMaps.TryGetValue(mapId, out var mindMap))
        {
            var existingNode = mindMap.Nodes.FirstOrDefault(n => n.Id == node.Id);
            if (existingNode != null)
            {
                _logger.LogInformation("Updating node: {NodeText}", node.Text);
                existingNode.Text = node.Text;
                existingNode.Tags = node.Tags ?? existingNode.Tags;
                existingNode.Status = node.Status;
                existingNode.Notes = node.Notes;
                existingNode.NodeType = node.NodeType;
                existingNode.Color = node.Color;
                existingNode.PositionX = node.PositionX;
                existingNode.PositionY = node.PositionY;
                mindMap.UpdatedAt = DateTime.UtcNow;
                SaveToJson();
            }
            else
            {
                _logger.LogWarning("Node {NodeId} NOT found in map {MapId}", node.Id, mapId);
            }
        }
        else
        {
            _logger.LogWarning("Map {MapId} NOT found in dictionary", mapId);
        }
        await Task.CompletedTask;
    }

    public async Task<MindMapNode?> AddOrUpdateNodeAsync(string mapId, MindMapNode node)
    {
        if (!_mindMaps.TryGetValue(mapId, out var mindMap))
            return await Task.FromResult<MindMapNode?>(null);
        var existing = mindMap.Nodes.FirstOrDefault(n => n.Id == node.Id);
        if (existing != null)
        {
            existing.Text = node.Text;
            existing.NodeType = node.NodeType ?? existing.NodeType;
            existing.Color = node.Color ?? existing.Color;
            existing.PositionX = node.PositionX;
            existing.PositionY = node.PositionY;
            existing.Notes = node.Notes;
            existing.Tags = node.Tags ?? existing.Tags;
        }
        else
        {
            mindMap.Nodes.Add(node);
        }
        mindMap.UpdatedAt = DateTime.UtcNow;
        SaveToJson();
        return await Task.FromResult<MindMapNode?>(mindMap.Nodes.FirstOrDefault(n => n.Id == node.Id));
    }

    public async Task RemoveNodeAsync(string mapId, string nodeId)
    {
        if (_mindMaps.TryGetValue(mapId, out var mindMap))
        {
            var node = mindMap.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                mindMap.Nodes.Remove(node);
                foreach (var n in mindMap.Nodes.Where(n => n.ParentId == nodeId))
                    n.ParentId = node.ParentId;
                mindMap.UpdatedAt = DateTime.UtcNow;
                SaveToJson();
            }
        }
        await Task.CompletedTask;
    }

    public async Task<string> PromoteNodeAsync(string mapId, string nodeId)
    {
        _logger.LogInformation("PromoteNodeAsync called for Map: {MapId}, Node: {NodeId}. Current known maps: {TotalMaps}", mapId, nodeId, _mindMaps.Count);
        if (_mindMaps.TryGetValue(mapId, out var mindMap))
        {
            var node = mindMap.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                _logger.LogInformation("Node found: {NodeText}. Promoting...", node.Text);
                // Create a new ProjectContext from this node using IProjectManager
                var newContext = await _projectManager.CreateProjectAsync(
                    node.Text, 
                    node.Notes ?? $"Promoted from mind map node: {node.Text}");

                _logger.LogInformation("Project created: {ContextId}", newContext.Id);
                // Update node state
                node.IsPromoted = true;
                node.PromotedToId = newContext.Id;
                node.Status = "Approved";
                mindMap.UpdatedAt = DateTime.UtcNow;
                SaveToJson();

                return newContext.Id;
            }
            _logger.LogWarning("Node {NodeId} NOT found in map {MapId}", nodeId, mapId);
        }
        else
        {
            _logger.LogWarning("Map {MapId} NOT found in dictionary", mapId);
        }
        return string.Empty;
    }
    public async Task<MindMapCollection> CreateMindMapFromTextAsync(string text, string name, string? workContextId = null)
    {
        var mindMap = new MindMapCollection
        {
            Name = name,
            WorkContextId = workContextId,
            Description = "Generated from text input",
            Nodes = ParseTextToNodes(text)
        };

        _mindMaps[mindMap.Id] = mindMap;
        SaveToJson();
        _logger.LogInformation("Created mind map {MapId} with {NodeCount} nodes. Total maps: {TotalMaps}", 
            mindMap.Id, mindMap.Nodes.Count, _mindMaps.Count);
        return await Task.FromResult(mindMap);
    }
    
    public async Task<MindMapCollection> CreateMindMapFromTranscriptAsync(string transcript, string name, string? workContextId = null)
    {
        var mindMap = new MindMapCollection
        {
            Name = name,
            WorkContextId = workContextId,
            Description = "Generated from meeting transcript",
            Nodes = ParseTranscriptToNodes(transcript)
        };

        _mindMaps[mindMap.Id] = mindMap;
        return await Task.FromResult(mindMap);
    }
    
    public async Task<MindMapCollection> CreateMindMapFromMeetingNodesAsync(List<Compost.Core.Models.MindMapNode> meetingNodes, string name, string? workContextId = null, string? meetingId = null)
    {
        var mindMap = new MindMapCollection
        {
            Name = name,
            WorkContextId = workContextId,
            Description = $"Generated from meeting transcript with {meetingNodes?.Count ?? 0} extracted nodes.",
            Nodes = ConvertMeetingNodesToMindMapNodes(meetingNodes, meetingId),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mindMaps[mindMap.Id] = mindMap;
        SaveToJson();
        
        _logger.LogInformation("Created mind map {MapId} from meeting {MeetingId} with {NodeCount} nodes", 
            mindMap.Id, meetingId, mindMap.Nodes.Count);
        
        return await Task.FromResult(mindMap);
    }
    
    public async Task<MindMapCollection> CreateMindMapFromRequirementsAsync(string requirements, string name, string? workContextId = null)
    {
        var mindMap = new MindMapCollection
        {
            Name = name,
            WorkContextId = workContextId,
            Description = "Generated from requirements",
            Nodes = ParseRequirementsToNodes(requirements)
        };

        _mindMaps[mindMap.Id] = mindMap;
        return await Task.FromResult(mindMap);
    }
    
    public async Task<MindMapCollection> GetMindMapAsync(string id)
    {
        _mindMaps.TryGetValue(id, out var mindMap);
        return await Task.FromResult(mindMap ?? new MindMapCollection { Name = "Not Found" });
    }
    
    public async Task<List<MindMapCollection>> GetMindMapsByContextAsync(string workContextId)
    {
        var maps = _mindMaps.Values.Where(m => m.WorkContextId == workContextId).ToList();
        return await Task.FromResult(maps);
    }
    
    public async Task SaveMindMapAsync(MindMapCollection mindMap)
    {
        mindMap.UpdatedAt = DateTime.UtcNow;
        _mindMaps[mindMap.Id] = mindMap;
        SaveToJson();
        await Task.CompletedTask;
    }
    
    public async Task UpdateMindMapAsync(MindMapCollection mindMap)
    {
        await SaveMindMapAsync(mindMap);
    }
    
    public async Task DeleteMindMapAsync(string id)
    {
        _mindMaps.Remove(id);
        SaveToJson();
        await Task.CompletedTask;
    }

    public async Task<List<MindMapCollection>> GetAllMindMapsAsync()
    {
        return await Task.FromResult(_mindMaps.Values.ToList());
    }
    
    // ========== Parsing Logic ==========
    
    private List<MindMapNode> ParseTextToNodes(string text)
    {
        var nodes = new List<MindMapNode>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // Create root node at center
        var root = new MindMapNode
        {
            Text = "Main Topic",
            NodeType = "Root",
            PositionX = 400,
            PositionY = 300,
            Color = "#2c5f2d"
        };
        nodes.Add(root);
        
        var currentParent = root;
        var level = 1;
        var angleStep = (2 * Math.PI) / (lines.Count() > 8 ? lines.Count() : 8.0); // Distribute nodes in a circle
        
        foreach (var line in lines.Select((line, index) => (Text: line.Trim(), Index: index)))
        {
            if (string.IsNullOrWhiteSpace(line.Text)) continue;
            
            var (nodeText, nodeType, detectedLevel) = AnalyzeLine(line.Text);
            
            // Calculate radial position
            var angle = line.Index * angleStep;
            var radius = 150 + (detectedLevel * 60); // Further out for deeper levels
            var x = 400 + radius * Math.Cos(angle);
            var y = 300 + radius * Math.Sin(angle);
            
            var node = new MindMapNode
            {
                Text = nodeText,
                NodeType = nodeType,
                ParentId = currentParent.Id,
                Level = detectedLevel,
                PositionX = (float)x,
                PositionY = (float)y,
                Color = GetColorForNodeType(nodeType),
                Icon = GetIconForNodeType(nodeType),
                SourceType = "TextParse",
                SourceText = line.Text
            };

            nodes.Add(node);
            currentParent.ChildIds.Add(node.Id);
            
            // Update parent based on level
            if (detectedLevel > level)
            {
                currentParent = node;
                level = detectedLevel;
            }
            else if (detectedLevel < level)
            {
                // Find parent at the previous level
                var parentCandidates = nodes.Where(n => n.Level == detectedLevel - 1).ToList();
                if (parentCandidates.Count != 0)
                {
                    currentParent = parentCandidates.First();
                }
                level = detectedLevel;
            }
        }
        
        return nodes;
    }
    
    private List<MindMapNode> ParseTranscriptToNodes(string transcript)
    {
        var nodes = new List<MindMapNode>();
        
        // Extract key segments from transcript
        var segments = ExtractSegments(transcript);
        
        var root = new MindMapNode
        {
            Text = "Meeting Summary",
            NodeType = "Root",
            PositionX = 400,
            PositionY = 300,
            Color = "#2c5f2d",
            SourceType = "Transcript"
        };
        nodes.Add(root);
        
        var angleStep = 2 * Math.PI / Math.Max(segments.Count, 1);
        var radius = 200;
        
        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var angle = i * angleStep;
            
            var node = new MindMapNode
            {
                Text = segment.Text.Length > 50 ? segment.Text[..50] + "..." : segment.Text,
                NodeType = segment.SegmentType,
                ParentId = root.Id,
                Level = 1,
                PositionX = 400 + radius * Math.Cos(angle),
                PositionY = 300 + radius * Math.Sin(angle),
                Color = GetColorForNodeType(segment.SegmentType),
                Icon = GetIconForNodeType(segment.SegmentType),
                SourceType = "Transcript",
                SourceText = segment.Text,
                Tags = [segment.SegmentType.ToLower()]
            };

            nodes.Add(node);
            root.ChildIds.Add(node.Id);
        }
        
        return nodes;
    }
    
    private List<MindMapNode> ParseRequirementsToNodes(string requirements)
    {
        var nodes = new List<MindMapNode>();
        
        // Parse numbered or bulleted requirements
        var pattern = @"(?:^|\n)(?:\d+[.)]\s*|[-•]\s*|\[\s*(\d+)\s*\]\s*)(.+?)(?=\n(?:\d+[.)]\s*|[-•]\s*|\[\s*\d+\s*\]\s*)|$)";
        var matches = Regex.Matches(requirements, pattern, RegexOptions.Singleline);
        
        var root = new MindMapNode
        {
            Text = "Requirements",
            NodeType = "Root",
            PositionX = 400,
            PositionY = 300,
            Color = "#2c5f2d",
            SourceType = "Requirement"
        };
        nodes.Add(root);
        
        var angleStep = 2 * Math.PI / Math.Max(matches.Count, 1);
        var radius = 200;
        
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var reqText = match.Groups[2].Value.Trim();
            if (string.IsNullOrWhiteSpace(reqText)) continue;
            
            // Use enhanced node type recognition
            var (nodeText, nodeType, detectedLevel) = AnalyzeLine(reqText);
            
            var angle = i * angleStep;
            
            var node = new MindMapNode
            {
                Text = nodeText.Length > 60 ? nodeText[..60] + "..." : nodeText,
                NodeType = nodeType,
                ParentId = root.Id,
                Level = 1,
                PositionX = 400 + radius * Math.Cos(angle),
                PositionY = 300 + radius * Math.Sin(angle),
                Color = GetColorForNodeType(nodeType),
                Icon = GetIconForNodeType(nodeType),
                SourceType = "Requirement",
                SourceText = reqText
            };

            nodes.Add(node);
            root.ChildIds.Add(node.Id);
        }
        
        return nodes;
    }
    
    private (string text, string type, int level) AnalyzeLine(string line)
    {
        int level = 1;
        string type = "Idea";
        
        // Detect level by indentation or markers
        if (line.StartsWith("    ") || line.StartsWith("\t")) level = 2;
        if (line.StartsWith("        ") || line.StartsWith("\t\t")) level = 3;
        
        // Detect type by keywords with enhanced recognition - prioritize exact matches
        var lowerLine = line.ToLower().Trim();
        
        // Questions (highest priority - check first)
        if (lowerLine.Contains("?") || lowerLine.StartsWith("what") || lowerLine.StartsWith("how") || 
            lowerLine.StartsWith("why") || lowerLine.StartsWith("when") || lowerLine.StartsWith("where") ||
            lowerLine.StartsWith("is there") || lowerLine.StartsWith("are there") || lowerLine.Contains("unclear") ||
            lowerLine.Contains("not sure") || lowerLine.Contains("need clarification"))
            type = "Question";
        // Actions/Tasks (high priority)
        else if (lowerLine.Contains("action item") || lowerLine.Contains("task:") || lowerLine.Contains("todo:") ||
                 lowerLine.StartsWith("implement") || lowerLine.StartsWith("develop") || lowerLine.StartsWith("create") ||
                 lowerLine.StartsWith("build") || lowerLine.StartsWith("fix") || lowerLine.StartsWith("add") ||
                 lowerLine.Contains("will need to") || lowerLine.Contains("should implement"))
            type = nameof(Action);
        // Decisions
        else if (lowerLine.Contains("decision:") || lowerLine.Contains("decided to") || lowerLine.Contains("chosen:") ||
                 lowerLine.Contains("selected:") || lowerLine.Contains("agreed to") || lowerLine.Contains("concluded"))
            type = "Decision";
        // Risks/Issues
        else if (lowerLine.Contains("risk:") || lowerLine.Contains("issue:") || lowerLine.Contains("concern:") ||
                 lowerLine.Contains("problem:") || lowerLine.Contains("challenge:") || lowerLine.Contains("obstacle") ||
                 lowerLine.Contains("potential issue") || lowerLine.Contains("might fail"))
            type = "Risk";
        // Goals/Objectives
        else if (lowerLine.Contains("goal:") || lowerLine.Contains("objective:") || lowerLine.Contains("target:") ||
                 lowerLine.StartsWith("goal is") || lowerLine.StartsWith("objective is") || lowerLine.Contains("aim:"))
            type = "Goal";
        // Timeline/Milestones
        else if (lowerLine.Contains("timeline:") || lowerLine.Contains("milestone:") || lowerLine.Contains("deadline:") ||
                 lowerLine.Contains("schedule:") || lowerLine.Contains("by ") && lowerLine.Contains("date"))
            type = "Timeline";
        // Resources
        else if (lowerLine.Contains("resource:") || lowerLine.Contains("tool:") || lowerLine.Contains("library:") ||
                 lowerLine.Contains("framework:") || lowerLine.Contains("dependency:"))
            type = "Resource";
        // Requirements (most specific patterns last)
        else if (lowerLine.Contains("requirement:") || lowerLine.Contains("shall ") || 
                 (lowerLine.Contains("must ") && !lowerLine.Contains("question")) ||
                 lowerLine.Contains("the system must") || lowerLine.Contains("user must") ||
                 lowerLine.Contains("feature:") || lowerLine.Contains("functionality:"))
            type = "Requirement";
        // Notes/Information
        else if (lowerLine.Contains("note:") || lowerLine.Contains("remember:") || lowerLine.Contains("info:") ||
                 lowerLine.Contains("information:") || lowerLine.Contains("detail:"))
            type = "Note";
        // Ideas/Concepts (fallback)
        else if (lowerLine.Contains("idea:") || lowerLine.Contains("concept:") || lowerLine.Contains("thought:") ||
                 lowerLine.Contains("suggestion:") || lowerLine.Contains("proposal:"))
            type = "Idea";
        
        // Clean up the text
        var cleanText = line.TrimStart('-', '*', '•', ' ', '\t', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', ')');
        
        // If no specific type matched but the sentence is significant, categorize as Note/Information
        if (type == "Idea" && cleanText.Length > 40 && !lowerLine.Contains("idea") && !lowerLine.Contains("think"))
        {
            type = "Note";
        }
        
        return (cleanText.Trim(), type, level);
    }
    
    private List<ParsedSegment> ExtractSegments(string text)
    {
        var segments = new List<ParsedSegment>();
        
        // Split by sentences
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
        
        foreach (var sentence in sentences.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            // Use enhanced node type recognition
            var (nodeText, nodeType, detectedLevel) = AnalyzeLine(sentence);

            var item = new ParsedSegment
            {
                Text = nodeText.Trim(),
                SegmentType = nodeType,
                Confidence = 0.8
            };
            segments.Add(item);
        }
        
        // Limit to top 30 most significant segments (was 8)
        return segments.Take(30).ToList();
    }
    
    private string GetColorForNodeType(string nodeType)
    {
        return nodeType switch
        {
            "Root" => "#2c5f2d",
            "Idea" => "#4a7c4b",
            "Requirement" => "#2196f3",
            "Question" => "#ff9800",
            nameof(Action) => "#9c27b0",
            "Decision" => "#4caf50",
            "Risk" => "#f44336",
            "Note" => "#757575",
            "Recommendation" => "#00bcd4",
            "Optional" => "#8bc34a",
            "Goal" => "#673ab7",
            "Resource" => "#00acc1",
            "Timeline" => "#ff5722",
            _ => "#97bc62"
        };
    }
    
    private string GetIconForNodeType(string nodeType)
    {
        return nodeType switch
        {
            "Root" => "fas fa-sitemap",
            "Idea" => "fas fa-lightbulb",
            "Requirement" => "fas fa-clipboard-check",
            "Question" => "fas fa-question-circle",
            nameof(Action) => "fas fa-tasks",
            "Decision" => "fas fa-balance-scale",
            "Risk" => "fas fa-exclamation-triangle",
            "Note" => "fas fa-sticky-note",
            "Recommendation" => "fas fa-thumbs-up",
            "Optional" => "fas fa-circle",
            "Goal" => "fas fa-bullseye",
            "Resource" => "fas fa-toolbox",
            "Timeline" => "fas fa-clock",
            _ => "fas fa-circle"
        };
    }

    public async Task<string> ExportToJsonAsync(string id)
    {
        var mindMap = await GetMindMapAsync(id);
        if (mindMap == null)
            throw new ArgumentException("MindMap not found");
        
        return JsonSerializer.Serialize(mindMap, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<string> ExportToMarkdownAsync(string id)
    {
        var mindMap = await GetMindMapAsync(id);
        if (mindMap == null)
            throw new ArgumentException("MindMap not found");
        
        var markdown = new StringBuilder();
        markdown.AppendLine($"# {mindMap.Name}");
        markdown.AppendLine();
        
        if (!string.IsNullOrEmpty(mindMap.Description))
        {
            markdown.AppendLine(mindMap.Description);
            markdown.AppendLine();
        }
        
        markdown.AppendLine("## Mind Map Structure");
        markdown.AppendLine();
        
        var rootNodes = mindMap.Nodes.Where(n => n.NodeType == "Root").ToList();
        foreach (var root in rootNodes)
        {
            await ExportNodeToMarkdown(markdown, root, mindMap.Nodes, 0);
        }
        
        return markdown.ToString();
    }

    private async Task ExportNodeToMarkdown(StringBuilder markdown, MindMapNode node, List<MindMapNode> allNodes, int level)
    {
        var indent = new string(' ', level * 2);
        var marker = level == 0 ? "#" : "-";
        
        markdown.AppendLine($"{indent}{marker} **{node.Text}** ({node.NodeType})");
        
        if (!string.IsNullOrEmpty(node.SourceText))
        {
            markdown.AppendLine($"{indent}  *Source: {node.SourceText}*");
        }
        
        if (node.Tags?.Any() == true)
        {
            markdown.AppendLine($"{indent}  Tags: {string.Join(", ", node.Tags)}");
        }
        
        markdown.AppendLine();
        
        var children = allNodes.Where(n => n.ParentId == node.Id).ToList();
        foreach (var child in children)
        {
            await ExportNodeToMarkdown(markdown, child, allNodes, level + 1);
        }
    }

    public async Task<MindMapCollection> ImportFromJsonAsync(string json)
    {
        try
        {
            var mindMap = JsonSerializer.Deserialize<MindMapCollection>(json);
            if (mindMap == null)
                throw new ArgumentException("Invalid JSON format");
            
            // Generate new ID to avoid conflicts
            mindMap.Id = Guid.NewGuid().ToString();
            
            // Generate new IDs for all nodes
            var nodeMapping = new Dictionary<string, string>();
            foreach (var node in mindMap.Nodes)
            {
                var oldId = node.Id;
                var newId = Guid.NewGuid().ToString();
                nodeMapping[oldId] = newId;
                node.Id = newId;
            }
            
            // Update parent references
            foreach (var node in mindMap.Nodes)
            {
                if (!string.IsNullOrEmpty(node.ParentId) && nodeMapping.TryGetValue(node.ParentId, out var value))
                {
                    node.ParentId = value;
                }
            }
            
            await SaveMindMapAsync(mindMap);
            return mindMap;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import mind map from JSON");
            throw;
        }
    }

    public async Task<MindMapCollection> CloneMindMapAsync(string id)
    {
        var sourceMap = await GetMindMapAsync(id);
        if (sourceMap == null)
            throw new ArgumentException("Source mind map not found");

        // Serialize and deserialize to create a deep copy
        var json = JsonSerializer.Serialize(sourceMap);
        var clonedMap = JsonSerializer.Deserialize<MindMapCollection>(json);
        
        if (clonedMap == null)
            throw new InvalidOperationException("Failed to clone mind map");

        // Generate new IDs
        clonedMap.Id = Guid.NewGuid().ToString();
        clonedMap.Name = $"{sourceMap.Name} (Copy)";
        clonedMap.CreatedAt = DateTime.UtcNow;
        clonedMap.UpdatedAt = DateTime.UtcNow;

        var nodeMapping = new Dictionary<string, string>();
        foreach (var node in clonedMap.Nodes)
        {
            var oldId = node.Id;
            var newId = Guid.NewGuid().ToString();
            nodeMapping[oldId] = newId;
            node.Id = newId;
        }

        // Update parent references and child IDs
        foreach (var node in clonedMap.Nodes)
        {
            if (!string.IsNullOrEmpty(node.ParentId) && nodeMapping.TryGetValue(node.ParentId, out var value))
            {
                node.ParentId = value;
            }
            
            // Update child IDs
            for (int i = 0; i < node.ChildIds.Count; i++)
            {
                if (nodeMapping.ContainsKey(node.ChildIds[i]))
                {
                    node.ChildIds[i] = nodeMapping[node.ChildIds[i]];
                }
            }
        }

        await SaveMindMapAsync(clonedMap);
        return clonedMap;
    }

    // Wrapper methods for controller compatibility
    public async Task AddNodeAsync(string mapId, MindMapNode node)
    {
        await AddOrUpdateNodeAsync(mapId, node);
    }

    public async Task DeleteNodeAsync(string mapId, string nodeId)
    {
        await RemoveNodeAsync(mapId, nodeId);
    }

    public async Task BulkDeleteNodesAsync(string mapId, string[] nodeIds)
    {
        var mindMap = await GetMindMapAsync(mapId);
        if (mindMap == null) return;

        foreach (var nodeId in nodeIds)
        {
            var node = mindMap.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                // Remove from parent's child list
                if (!string.IsNullOrEmpty(node.ParentId))
                {
                    var parent = mindMap.Nodes.FirstOrDefault(n => n.Id == node.ParentId);
                    parent?.ChildIds.Remove(nodeId);
                }

                // Reassign children to parent or make them root
                var children = mindMap.Nodes.Where(n => n.ParentId == nodeId).ToList();
                foreach (var child in children)
                {
                    if (string.IsNullOrEmpty(node.ParentId))
                    {
                        child.ParentId = null;
                    }
                    else
                    {
                        child.ParentId = node.ParentId;
                        var grandparent = mindMap.Nodes.FirstOrDefault(n => n.Id == node.ParentId);
                        grandparent?.ChildIds.Add(child.Id);
                    }
                }

                mindMap.Nodes.Remove(node);
            }
        }

        await SaveMindMapAsync(mindMap);
    }

    public async Task<MindMapCollection> CreateFromTemplateAsync(string templateName, string name, string? workContextId = null)
    {
        var mindMap = new MindMapCollection
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = $"Created from {templateName} template",
            WorkContextId = workContextId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        switch (templateName.ToLower())
        {
            case "project":
                CreateProjectPlanningTemplate(mindMap);
                break;
            case "brainstorming":
                CreateBrainstormingTemplate(mindMap);
                break;
            case "decision":
                CreateDecisionTreeTemplate(mindMap);
                break;
            case "meeting":
                CreateMeetingNotesTemplate(mindMap);
                break;
            case "research":
                CreateResearchTemplate(mindMap);
                break;
            default:
                CreateProjectPlanningTemplate(mindMap);
                break;
        }

        await SaveMindMapAsync(mindMap);
        return mindMap;
    }

    private void CreateProjectPlanningTemplate(MindMapCollection mindMap)
    {
        var root = new MindMapNode
        {
            Id = Guid.NewGuid().ToString(),
            Text = mindMap.Name,
            NodeType = "Root",
            PositionX = 400,
            PositionY = 300,
            Color = "#2c5f2d"
        };
        mindMap.Nodes.Add(root);

        var branches = new[]
        {
            ("Requirements", "Requirement"),
            ("Tasks", nameof(Action)),
            ("Timeline", "Decision"),
            ("Risks", "Risk")
        };

        var angleStep = 2 * Math.PI / branches.Length;
        var radius = 150;

        for (int i = 0; i < branches.Length; i++)
        {
            var (text, type) = branches[i];
            var angle = i * angleStep;
            
            var node = new MindMapNode
            {
                Id = Guid.NewGuid().ToString(),
                Text = text,
                NodeType = type,
                ParentId = root.Id,
                PositionX = 400 + radius * Math.Cos(angle),
                PositionY = 300 + radius * Math.Sin(angle),
                Color = GetColorForNodeType(type),
                Icon = GetIconForNodeType(type)
            };
            mindMap.Nodes.Add(node);
            root.ChildIds.Add(node.Id);
        }
    }

    private void CreateBrainstormingTemplate(MindMapCollection mindMap)
    {
        var root = new MindMapNode
        {
            Id = Guid.NewGuid().ToString(),
            Text = mindMap.Name,
            NodeType = "Root",
            PositionX = 400,
            PositionY = 300,
            Color = "#2c5f2d"
        };
        mindMap.Nodes.Add(root);

        var ideas = new[] { "Idea 1", "Idea 2", "Idea 3", "Idea 4" };
        var angleStep = 2 * Math.PI / ideas.Length;

        for (int i = 0; i < ideas.Length; i++)
        {
            var angle = i * angleStep;
            var x = 400 + (int)(200 * Math.Cos(angle));
            var y = 300 + (int)(200 * Math.Sin(angle));

            var node = new MindMapNode
            {
                Id = Guid.NewGuid().ToString(),
                Text = ideas[i],
                NodeType = "Idea",
                ParentId = root.Id,
                PositionX = x,
                PositionY = y,
                Color = "#4a7c4b"
            };
            mindMap.Nodes.Add(node);
            root.ChildIds.Add(node.Id);
        }
    }

    private void CreateDecisionTreeTemplate(MindMapCollection mindMap)
    {
        var root = new MindMapNode
        {
            Id = Guid.NewGuid().ToString(),
            Text = "Decision: " + mindMap.Name,
            NodeType = "Decision",
            PositionX = 400,
            PositionY = 100,
            Color = "#4caf50"
        };
        mindMap.Nodes.Add(root);

        var options = new[] { "Option A", "Option B", "Option C" };
        var yOffset = 250;

        foreach (var option in options)
        {
            var node = new MindMapNode
            {
                Id = Guid.NewGuid().ToString(),
                Text = option,
                NodeType = "Idea",
                ParentId = root.Id,
                PositionX = 400,
                PositionY = yOffset,
                Color = "#2196f3"
            };
            mindMap.Nodes.Add(node);
            root.ChildIds.Add(node.Id);

            // Add pros and cons
            var pros = new MindMapNode
            {
                Id = Guid.NewGuid().ToString(),
                Text = "Pros",
                NodeType = "Note",
                ParentId = node.Id,
                PositionX = 300,
                PositionY = yOffset + 100,
                Color = "#4caf50"
            };
            mindMap.Nodes.Add(pros);
            node.ChildIds.Add(pros.Id);

            var cons = new MindMapNode
            {
                Id = Guid.NewGuid().ToString(),
                Text = "Cons",
                NodeType = "Risk",
                ParentId = node.Id,
                PositionX = 500,
                PositionY = yOffset + 100,
                Color = "#f44336"
            };
            mindMap.Nodes.Add(cons);
            node.ChildIds.Add(cons.Id);

            yOffset += 150;
        }
    }

    private void CreateMeetingNotesTemplate(MindMapCollection mindMap)
    {
        var root = new MindMapNode
        {
            Id = Guid.NewGuid().ToString(),
            Text = mindMap.Name,
            NodeType = "Root",
            PositionX = 400,
            PositionY = 150,
            Color = "#2c5f2d"
        };
        mindMap.Nodes.Add(root);

        var sections = new[]
        {
            ("Attendees", "Note", 200, 300),
            ("Agenda", "Requirement", 400, 300),
            ("Action Items", nameof(Action), 600, 300),
            ("Decisions", "Decision", 300, 450),
            ("Questions", "Question", 500, 450)
        };

        foreach (var (text, type, x, y) in sections)
        {
            var node = new MindMapNode
            {
                Id = Guid.NewGuid().ToString(),
                Text = text,
                NodeType = type,
                ParentId = root.Id,
                PositionX = x,
                PositionY = y,
                Color = GetColorForNodeType(type)
            };
            mindMap.Nodes.Add(node);
            root.ChildIds.Add(node.Id);
        }
    }

    private void CreateResearchTemplate(MindMapCollection mindMap)
    {
        var root = new MindMapNode
        {
            Id = Guid.NewGuid().ToString(),
            Text = "Research: " + mindMap.Name,
            NodeType = "Root",
            PositionX = 400,
            PositionY = 300,
            Color = "#2c5f2d"
        };
        mindMap.Nodes.Add(root);

        var branches = new[]
        {
            ("Hypothesis", "Idea", 200, 150),
            ("Sources", "Requirement", 600, 150),
            ("Methodology", "Decision", 200, 450),
            ("Findings", "Note", 600, 450),
            ("Conclusions", "Decision", 400, 550)
        };

        foreach (var (text, type, x, y) in branches)
        {
            var node = new MindMapNode
            {
                Id = Guid.NewGuid().ToString(),
                Text = text,
                NodeType = type,
                ParentId = root.Id,
                PositionX = x,
                PositionY = y,
                Color = GetColorForNodeType(type)
            };
            mindMap.Nodes.Add(node);
            root.ChildIds.Add(node.Id);
        }
    }
    
    private List<MindMapNode> ConvertMeetingNodesToMindMapNodes(List<Compost.Core.Models.MindMapNode> meetingNodes, string? meetingId)
    {
        var mindMapNodes = new List<MindMapNode>();
        
        if (meetingNodes is not { Count: not 0 })
        {
            return mindMapNodes;
        }
        
        // Create root node
        var root = new MindMapNode
        {
            Id = Guid.NewGuid().ToString(),
            Text = "Meeting Insights",
            NodeType = "Root",
            PositionX = 400,
            PositionY = 300,
            Color = "#2c5f2d",
            SourceType = "Meeting",
            SourceReference = meetingId,
            ChildIds = []
        };
        mindMapNodes.Add(root);
        
        // Group nodes by type for better layout
        var nodeGroups = meetingNodes.Where(n => n != null).GroupBy(n => n.NodeType);
        var angleStep = nodeGroups.Any() ? 360.0 / nodeGroups.Count() : 360.0;
        var currentAngle = 0.0;
        
        foreach (var group in nodeGroups)
        {
            // Create category node
            var categoryNode = new MindMapNode
            {
                Id = Guid.NewGuid().ToString(),
                Text = GetCategoryName(group.Key),
                NodeType = "Category",
                PositionX = 400 + (int)(200 * Math.Cos(currentAngle * Math.PI / 180)),
                PositionY = 300 + (int)(200 * Math.Sin(currentAngle * Math.PI / 180)),
                Color = GetColorForNodeType(GetMindMapNodeTypeName(group.Key)),
                ParentId = root.Id,
                SourceType = "Meeting",
                SourceReference = meetingId,
                ChildIds = []
            };
            mindMapNodes.Add(categoryNode);
            root.ChildIds.Add(categoryNode.Id);
            
            // Add individual nodes from this category
            var groupList = group.Where(n => n != null && !string.IsNullOrEmpty(n.Title)).ToList();
            if (groupList.Count != 0)
            {
                var itemAngleStep = 60.0 / groupList.Count();
                var itemAngle = currentAngle - 30;
                
                foreach (var meetingNode in groupList)
                {
                    var node = new MindMapNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        Text = meetingNode.Title,
                        NodeType = GetMindMapNodeTypeName(meetingNode.NodeType),
                        PositionX = categoryNode.PositionX + (int)(120 * Math.Cos(itemAngle * Math.PI / 180)),
                        PositionY = categoryNode.PositionY + (int)(120 * Math.Sin(itemAngle * Math.PI / 180)),
                        Color = GetColorForNodeType(GetMindMapNodeTypeName(meetingNode.NodeType)),
                        ParentId = categoryNode.Id,
                        SourceType = "Meeting",
                        SourceReference = meetingId,
                        SourceText = meetingNode.OriginalTranscript ?? meetingNode.Description,
                        ChildIds = []
                    };
                    mindMapNodes.Add(node);
                    categoryNode.ChildIds.Add(node.Id);
                    
                    itemAngle += itemAngleStep;
                }
            }
            
            currentAngle += angleStep;
        }
        
        return mindMapNodes;
    }
    
    private string GetCategoryName(Compost.Core.Models.MindMapNodeType nodeType)
    {
        return nodeType switch
        {
            Core.Models.MindMapNodeType.Idea => "Ideas & Concepts",
            Core.Models.MindMapNodeType.Requirement => "Requirements",
            Core.Models.MindMapNodeType.Action => "Action Items",
            Core.Models.MindMapNodeType.Question => "Questions",
            Core.Models.MindMapNodeType.Decision => "Decisions",
            Core.Models.MindMapNodeType.Risk => "Risks & Issues",
            Core.Models.MindMapNodeType.Note => "Notes",
            _ => "General"
        };
    }
    
    private string GetMindMapNodeTypeName(Compost.Core.Models.MindMapNodeType nodeType)
    {
        return nodeType switch
        {
            Core.Models.MindMapNodeType.Idea => "Idea",
            Core.Models.MindMapNodeType.Requirement => "Requirement",
            Core.Models.MindMapNodeType.Action => nameof(Action),
            Core.Models.MindMapNodeType.Question => "Question",
            Core.Models.MindMapNodeType.Decision => "Decision",
            Core.Models.MindMapNodeType.Risk => "Risk",
            Core.Models.MindMapNodeType.Note => "Note",
            _ => "General"
        };
    }
    
    // ========== Core Interface Implementations ==========
    
    async Task<List<MindMapSummary>> Compost.Core.Interfaces.IMindMapService.GetAllMindMapsAsync()
    {
        var mindMaps = await GetAllMindMapsAsync();
        return mindMaps.Select(m => new MindMapSummary
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            WorkContextId = m.WorkContextId,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt,
            NodeCount = m.Nodes?.Count ?? 0
        }).ToList();
    }
    
    async Task<MindMapSummary?> Compost.Core.Interfaces.IMindMapService.GetMindMapSummaryAsync(string id)
    {
        var mindMap = await GetMindMapAsync(id);
        if (string.IsNullOrEmpty(mindMap.Name))
            return null;
            
        return new MindMapSummary
        {
            Id = mindMap.Id,
            Name = mindMap.Name,
            Description = mindMap.Description,
            WorkContextId = mindMap.WorkContextId,
            CreatedAt = mindMap.CreatedAt,
            UpdatedAt = mindMap.UpdatedAt,
            NodeCount = mindMap.Nodes?.Count ?? 0
        };
    }
    
    async Task<List<MindMapSummary>> Compost.Core.Interfaces.IMindMapService.GetMindMapsByContextAsync(string workContextId)
    {
        var mindMaps = await GetMindMapsByContextAsync(workContextId);
        return mindMaps.Select(m => new MindMapSummary
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            WorkContextId = m.WorkContextId,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt,
            NodeCount = m.Nodes?.Count ?? 0
        }).ToList();
    }
}
