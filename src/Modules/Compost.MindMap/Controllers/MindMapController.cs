using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Compost.Core.Extensions;
using Compost.Kanban.Models;
using Compost.MindMap.Models;
using Compost.MindMap.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using IMindMapService = Compost.MindMap.Services.IMindMapService;
using MindMapNodeModel = Compost.Core.Models.MindMapNode;

namespace Compost.MindMap.Controllers;

public class MindMapController(
    IMindMapService mindMapService,
    IProjectManager projectManager,
    IDecompositionEngine decompositionEngine,
    IContentManager contentManager,
    ITranscriptionService transcriptionService,
    ILogger<MindMapController> logger)
    : Controller
{
    // GET: /MindMap
    public async Task<IActionResult> Index()
    {
        var contexts = await projectManager.GetAllProjectsAsync();
        var mindMaps = new List<MindMapCollection>();

        // Load mind maps associated with contexts
        foreach (var context in contexts)
        {
            var maps = await mindMapService.GetMindMapsByContextAsync(context.Id);
            mindMaps.AddRange(maps);
        }

        // Also load all mind maps to ensure we don't miss any (including those without contexts)
        var allMindMaps = await mindMapService.GetAllMindMapsAsync();

        // Add any mind maps that aren't already in the list (avoid duplicates)
        foreach (var map in allMindMaps)
            if (mindMaps.All(m => m.Id != map.Id))
                mindMaps.Add(map);

        // Sort by creation date (newest first)
        mindMaps = mindMaps.OrderByDescending(m => m.CreatedAt).ToList();

        return View(mindMaps);
    }

    // GET: /MindMap/Latest
    public async Task<IActionResult> Latest()
    {
        var allMindMaps = await mindMapService.GetAllMindMapsAsync();
        var latestMindMap = allMindMaps.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

        if (latestMindMap == null) return RedirectToAction(nameof(Index));

        return RedirectToAction(nameof(ViewMap), new { id = latestMindMap.Id });
    }

    // GET: /MindMap/View/abc123
    public async Task<IActionResult> ViewMap(string id)
    {
        var mindMap = await mindMapService.GetMindMapAsync(id);
        if (string.IsNullOrEmpty(mindMap.Name)) return NotFound();

        return View(mindMap);
    }

    // GET: /MindMap/FromMeeting/abc123
    [HttpGet("MindMap/FromMeeting/{meetingId}")]
    public async Task<IActionResult> FromMeeting(string meetingId)
    {
        try
        {
            if (string.IsNullOrEmpty(meetingId))
                return BadRequest("Meeting ID is required");

            var meeting = await transcriptionService.GetMeetingByIdAsync(meetingId);
            if (meeting == null) return NotFound("Meeting not found");

            var mindMapNodes = await transcriptionService.ExtractMindMapNodesAsync(meetingId);
            if (mindMapNodes.Count == 0) return BadRequest("No mind map nodes found for this meeting");

            var mindMap = await mindMapService.CreateMindMapFromMeetingNodesAsync(
                mindMapNodes, $"Meeting: {meeting.Title}", meeting.WorkContextId, meetingId);

            return RedirectToAction(nameof(ViewMap), new { id = mindMap.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error creating mind map from meeting: {ex.Message}");
        }
    }

    // POST: /MindMap/FromMeeting/abc123
    [HttpPost("MindMap/FromMeeting/{meetingId}")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> FromMeeting(string meetingId, [FromBody] MindMapRequestModel model)
    {
        try
        {
            if (string.IsNullOrEmpty(meetingId)) return BadRequest("Meeting ID is required");

            var meeting = await transcriptionService.GetMeetingByIdAsync(meetingId);
            if (meeting == null) return NotFound("Meeting not found");

            var mindMapNodes = await transcriptionService.ExtractMindMapNodesAsync(meetingId);
            if (mindMapNodes is not { Count: not 0 })
                return BadRequest("No mind map nodes found for this meeting");

            var mindMap = await mindMapService.CreateMindMapFromMeetingNodesAsync(
                mindMapNodes, model.Name, model.WorkContextId, meetingId);

            mindMap.Description = model.Description;
            await mindMapService.SaveMindMapAsync(mindMap);

            return RedirectToAction(nameof(ViewMap), new { id = mindMap.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error creating mind map from meeting: {ex.Message}");
        }
    }

    // GET: /MindMap/GetNodes/abc123
    [HttpGet]
    public async Task<IActionResult> GetNodes(string id)
    {
        try
        {
            var mindMap = await mindMapService.GetMindMapAsync(id);
            if (string.IsNullOrEmpty(mindMap.Name))
                return Json(new { success = false, error = "MindMap not found" });

            return Json(new
            {
                success = true,
                nodes = mindMap.Nodes?.ToList() ?? []
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: /MindMap/Create
    public async Task<IActionResult> Create(string? workContextId = null)
    {
        var contexts = await projectManager.GetAllProjectsAsync();
        ViewBag.WorkContexts = contexts;
        ViewBag.SelectedContextId = workContextId;

        return View(new CreateMindMapViewModel());
    }

    // POST: /MindMap/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMindMapViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var contexts = await projectManager.GetAllProjectsAsync();
            ViewBag.WorkContexts = contexts;
            return View(model);
        }

        MindMapCollection mindMap;

        switch (model.SourceType)
        {
            case "Transcript":
                mindMap = await mindMapService.CreateMindMapFromTranscriptAsync(
                    model.SourceText, model.Name, model.WorkContextId);
                break;
            case "Requirements":
                mindMap = await mindMapService.CreateMindMapFromRequirementsAsync(
                    model.SourceText, model.Name, model.WorkContextId);
                break;
            default:
                mindMap = await mindMapService.CreateMindMapFromTextAsync(
                    model.SourceText, model.Name, model.WorkContextId);
                break;
        }

        TempData["SuccessMessage"] = $"Mind map '{model.Name}' created with {mindMap.Nodes.Count} nodes.";
        return RedirectToAction(nameof(ViewMap), new { id = mindMap.Id });
    }

    // POST: /MindMap/Delete/abc123
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await mindMapService.DeleteMindMapAsync(id);
        TempData["SuccessMessage"] = "Mind map deleted.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /MindMap/FromContext/abc123
    public async Task<IActionResult> FromContext(string projectId)
    {
        var context = await projectManager.GetProjectByIdAsync(projectId);
        if (context == null) return NotFound();

        // Create mind map from context data
        var sourceText = $@"
Project: {context.Name}
Description: {context.Description}
Repository: {context.RepositoryName}
Branch: {context.CurrentBranch}
Notes: {context.Notes}
Testing Steps: {string.Join(", ", context.TestingSteps)}
Open Questions: {string.Join(", ", context.OpenQuestions.Select(q => q.Question))}
";

        var mindMap = await mindMapService.CreateMindMapFromTextAsync(
            sourceText, $"Mind Map: {context.Name}", projectId);

        TempData["SuccessMessage"] = $"Mind map created from context '{context.Name}'.";
        return RedirectToAction(nameof(ViewMap), new { id = mindMap.Id });
    }

    // GET: /MindMap/Export/abc123/json
    public async Task<IActionResult> Export(string id, string format)
    {
        try
        {
            var mindMap = await mindMapService.GetMindMapAsync(id);

            switch (format.ToLower())
            {
                case "json":
                    var json = await mindMapService.ExportToJsonAsync(id);
                    return File(Encoding.UTF8.GetBytes(json),
                        "application/json", $"{mindMap.Name}.json");

                case "markdown":
                case "md":
                    var markdown = await mindMapService.ExportToMarkdownAsync(id);
                    return File(Encoding.UTF8.GetBytes(markdown),
                        "text/markdown", $"{mindMap.Name}.md");

                default:
                    return BadRequest("Unsupported export format");
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Export failed: {ex.Message}";
            return RedirectToAction(nameof(ViewMap), new { id });
        }
    }

    // GET: /MindMap/Import
    public IActionResult Import()
    {
        return View();
    }

    // POST: /MindMap/Import
    [HttpPost]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file to import.";
            return View();
        }

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var json = await reader.ReadToEndAsync();

            var mindMap = await mindMapService.ImportFromJsonAsync(json);
            TempData["SuccessMessage"] = $"Mind map '{mindMap.Name}' imported successfully.";
            return RedirectToAction(nameof(ViewMap), new { id = mindMap.Id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Import failed: {ex.Message}";
            return View();
        }
    }

    // GET: /MindMap/AddNode/abc123
    public async Task<IActionResult> AddNode(string id, string? parentNodeId = null)
    {
        var mindMap = await mindMapService.GetMindMapAsync(id);

        ViewBag.MindMapId = id;
        ViewBag.MindMapName = mindMap.Name;
        ViewBag.ParentNodeId = parentNodeId;
        ViewBag.ExistingNodes = mindMap.Nodes;

        var model = new MindMapNodeViewModel
        {
            MindMapId = id,
            ParentNodeId = parentNodeId
        };
        return View(model);
    }

    // POST: /MindMap/AddNode/abc123
    [HttpPost]
    public async Task<IActionResult> AddNode(string id, MindMapNodeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var mindMap = await mindMapService.GetMindMapAsync(id);
            ViewBag.MindMapId = id;
            ViewBag.MindMapName = mindMap?.Name;
            ViewBag.ExistingNodes = mindMap?.Nodes;
            return View(model);
        }

        var node = new MindMapNodeModel
        {
            Text = model.Text,
            NodeType = model.NodeType,
            ParentId = model.ParentNodeId,
            Color = GetNodeColor(model.NodeType),
            PositionX = model.PositionX,
            PositionY = model.PositionY,
            Tags = model.Tags.ParseTags(),
            SourceText = model.SourceText
        };

        await mindMapService.AddNodeAsync(id, node);
        TempData["SuccessMessage"] = "Node added successfully.";
        return RedirectToAction(nameof(ViewMap), new { id });
    }

    // GET: /MindMap/EditNode/abc123?nodeId=xyz789
    public async Task<IActionResult> EditNode(string id, string nodeId)
    {
        var mindMap = await mindMapService.GetMindMapAsync(id);

        var node = mindMap.Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node == null)
            return NotFound();

        var model = new MindMapNodeViewModel
        {
            MindMapId = id,
            NodeId = nodeId,
            Text = node.Text,
            NodeType = node.NodeType,
            ParentNodeId = node.ParentId,
            PositionX = node.PositionX,
            PositionY = node.PositionY,
            Tags = string.Join(", ", node.Tags ?? []),
            SourceText = node.SourceText
        };

        ViewBag.MindMapName = mindMap.Name;
        ViewBag.ExistingNodes = mindMap.Nodes.Where(n => n.Id != nodeId).ToList();
        return View(model);
    }

    // POST: /MindMap/EditNode/abc123
    [HttpPost]
    public async Task<IActionResult> EditNode(string id, MindMapNodeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.MindMapName = (await mindMapService.GetMindMapAsync(id))?.Name;
            return View(model);
        }

        var mindMap = await mindMapService.GetMindMapAsync(id);


        var node = mindMap.Nodes.FirstOrDefault(n => n.Id == model.NodeId);
        if (node == null)
            return NotFound();

        node.Text = model.Text;
        node.NodeType = model.NodeType;
        node.Color = GetNodeColor(model.NodeType);
        node.ParentId = model.ParentNodeId;
        node.PositionX = model.PositionX;
        node.PositionY = model.PositionY;
        node.Tags = model.Tags.ParseTags();
        node.SourceText = model.SourceText;

        await mindMapService.SaveMindMapAsync(mindMap);
        TempData["SuccessMessage"] = "Node updated successfully.";
        return RedirectToAction(nameof(ViewMap), new { id });
    }

    // POST: /MindMap/DeleteNode/abc123
    [HttpPost]
    public async Task<IActionResult> DeleteNode(string id, string nodeId)
    {
        await mindMapService.DeleteNodeAsync(id, nodeId);
        TempData["SuccessMessage"] = "Node deleted successfully.";
        return RedirectToAction(nameof(ViewMap), new { id });
    }

    // POST: /MindMap/BulkDeleteNodes/abc123
    [HttpPost]
    public async Task<IActionResult> BulkDeleteNodes(string id, string[] nodeIds)
    {
        if (nodeIds is not { Length: not 0 })
        {
            TempData["ErrorMessage"] = "No nodes selected for deletion.";
            return RedirectToAction(nameof(ViewMap), new { id });
        }

        try
        {
            await mindMapService.BulkDeleteNodesAsync(id, nodeIds);
            TempData["SuccessMessage"] = $"Deleted {nodeIds.Length} node(s) successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Bulk delete failed: {ex.Message}";
        }

        return RedirectToAction(nameof(ViewMap), new { id });
    }

    // POST: /MindMap/Clone/abc123
    public async Task<IActionResult> Clone(string id)
    {
        try
        {
            var newMap = await mindMapService.CloneMindMapAsync(id);
            TempData["SuccessMessage"] = $"Mind map cloned as '{newMap.Name}'.";
            return RedirectToAction(nameof(ViewMap), new { id = newMap.Id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Clone failed: {ex.Message}";
            return RedirectToAction(nameof(ViewMap), new { id });
        }
    }

    // POST: /MindMap/CreateFromTemplate
    [HttpPost]
    public async Task<IActionResult> CreateFromTemplate(string templateName, string name, string? workContextId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Mind map name is required.";
            return RedirectToAction(nameof(Create), new { workContextId });
        }

        try
        {
            var mindMap = await mindMapService.CreateFromTemplateAsync(templateName, name, workContextId);
            TempData["SuccessMessage"] = $"Mind map '{name}' created from {templateName} template.";
            return RedirectToAction(nameof(ViewMap), new { id = mindMap.Id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to create from template: {ex.Message}";
            return RedirectToAction(nameof(Create), new { workContextId });
        }
    }

    private string GetNodeColor(string nodeType)
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
            _ => "#97bc62"
        };
    }

    // GET: /MindMap/Search?q=keyword&nodeType=Idea
    public async Task<IActionResult> Search(string q, string? nodeType, string? workContextId)
    {
        var contexts = await projectManager.GetAllProjectsAsync();
        var mindMaps = new List<MindMapCollection>();

        foreach (var context in contexts)
        {
            var maps = await mindMapService.GetMindMapsByContextAsync(context.Id);
            mindMaps.AddRange(maps);
        }

        // Apply search filters
        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.ToLower();
            mindMaps = mindMaps.Where(m =>
                m.Name.ToLower().Contains(searchTerm) ||
                m.Description?.ToLower().Contains(searchTerm) == true ||
                m.Nodes.Any(n =>
                    n.Text.ToLower().Contains(searchTerm) ||
                    n.SourceText?.ToLower().Contains(searchTerm) == true ||
                    n.Tags.Any(t => t.ToLower().Contains(searchTerm))
                )
            ).ToList();
        }

        if (!string.IsNullOrWhiteSpace(nodeType))
            mindMaps = mindMaps.Where(m =>
                m.Nodes.Any(n => n.NodeType == nodeType)
            ).ToList();

        if (!string.IsNullOrWhiteSpace(workContextId))
            mindMaps = mindMaps.Where(m => m.WorkContextId == workContextId).ToList();

        ViewBag.SearchQuery = q;
        ViewBag.NodeTypeFilter = nodeType;
        ViewBag.WorkContextFilter = workContextId;
        ViewBag.TotalResults = mindMaps.Count;

        return View(nameof(Index), mindMaps);
    }

    // GET: /MindMap/Stats/abc123
    public async Task<IActionResult> Stats(string id)
    {
        var mindMap = await mindMapService.GetMindMapAsync(id);

        var stats = new MindMapStatsViewModel
        {
            MindMapId = id,
            MindMapName = mindMap.Name,
            TotalNodes = mindMap.Nodes.Count,
            NodesByType = mindMap.Nodes.GroupBy(n => n.NodeType)
                .ToDictionary(g => g.Key, g => g.Count()),
            RootNodes = mindMap.Nodes.Count(n => n.ParentId == null),
            LeafNodes = mindMap.Nodes.Count(n => mindMap.Nodes.All(child => child.ParentId != n.Id)),
            MaxDepth = CalculateMaxDepth(mindMap.Nodes),
            TotalTags = mindMap.Nodes.SelectMany(n => n.Tags).Distinct().Count(),
            TopTags = mindMap.Nodes.SelectMany(n => n.Tags)
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToDictionary(g => g.Key, g => g.Count()),
            CreatedAt = mindMap.CreatedAt,
            UpdatedAt = mindMap.UpdatedAt,
            WorkContextId = mindMap.WorkContextId
        };

        return View(stats);
    }

    private int CalculateMaxDepth(List<MindMapNodeModel> nodes)
    {
        if (nodes.Count == 0) return 0;

        var childrenMap = nodes.Where(n => n.ParentId != null)
                               .GroupBy(n => n.ParentId!)
                               .ToDictionary(g => g.Key, g => g.ToList());

        var maxDepth = 0;
        foreach (var root in nodes.Where(n => n.ParentId == null))
            maxDepth = Math.Max(maxDepth, CalculateNodeDepth(root, childrenMap, 1));
        return maxDepth;
    }

    private static int CalculateNodeDepth(MindMapNodeModel node, Dictionary<string, List<MindMapNodeModel>> childrenMap, int currentDepth)
    {
        if (!childrenMap.TryGetValue(node.Id, out var children))
            return currentDepth;

        return children.Select(child => CalculateNodeDepth(child, childrenMap, currentDepth + 1)).Prepend(currentDepth).Max();
    }

    // ========== API for interactive mind map (save/load/promote) ==========

    /// <summary>GET /MindMap/ApiMap/{id} - Returns graph as JSON for Cytoscape.</summary>
    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiMap(string? id)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest();
        var mindMap = await mindMapService.GetMindMapAsync(id);
        if (string.IsNullOrEmpty(mindMap.Name))
            return NotFound();
        return Json(new
        {
            mapId = mindMap.Id, workContextId = mindMap.WorkContextId, name = mindMap.Name, nodes = mindMap.Nodes ??
                []
        });
    }

    /// <summary>POST /MindMap/ApiUpdateNode - Add or update a node. Query: mapId. Body: node data.</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiUpdateNode([FromQuery] string mapId, [FromBody] MindMapNodeApiDto dto)
    {
        if (string.IsNullOrEmpty(mapId))
            return BadRequest();
        var node = new MindMapNodeModel
        {
            Id = dto.Id ?? Guid.NewGuid().ToString(),
            Text = dto.Title ?? dto.Label ?? "New Node",
            NodeType = dto.NodeType ?? "Idea",
            Color = dto.Color ?? "#81C784",
            PositionX = dto.PositionX,
            PositionY = dto.PositionY,
            ParentId = dto.ParentId,
            Notes = dto.Content
        };
        var updated = await mindMapService.AddOrUpdateNodeAsync(mapId, node);
        if (updated == null) return NotFound();
        return Json(updated);
    }

    /// <summary>PATCH /MindMap/ApiUpdateNodePosition - Update node position. Query: mapId, nodeId. Body: { x, y }.</summary>
    [HttpPatch]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiUpdateNodePosition([FromQuery] string mapId, [FromQuery] string nodeId,
        [FromBody] NodePositionDto dto)
    {
        if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(nodeId))
            return BadRequest();
        var mindMap = await mindMapService.GetMindMapAsync(mapId);
        var node = mindMap?.Nodes?.FirstOrDefault(n => n.Id == nodeId);
        if (node == null) return NotFound();
        node.PositionX = dto.X;
        node.PositionY = dto.Y;
        await mindMapService.UpdateNodeAsync(mapId, node);
        return Ok();
    }

    /// <summary>POST /MindMap/ApiPromoteNode - Promote mind map node to tree node and create Kanban card. Query: mapId, nodeId.</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiPromoteNode([FromQuery] string mapId, [FromQuery] string nodeId)
    {
        if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(nodeId))
            return BadRequest();
        var mindMap = await mindMapService.GetMindMapAsync(mapId);
        var node = mindMap?.Nodes?.FirstOrDefault(n => n.Id == nodeId);
        if (node == null) return NotFound();
        var projectId = mindMap?.WorkContextId ?? "default";
        logger.LogInformation("Promoting mind map node '{NodeText}' (ID: {NodeId}) to project context: '{ProjectId}'", node.Text, nodeId, projectId);
        
        // Check if this node already exists as an Orchard Core MindMapNode content item
        TreeNode treeNode;
        try
        {
            // Try to use the proper promotion pipeline if the node is an Orchard Core content item
            treeNode = await decompositionEngine.PromoteMindMapToTreeAsync(nodeId);
            logger.LogInformation("Successfully promoted MindMapNode {NodeId} to TreeNode {TreeNodeId} using proper pipeline", 
                nodeId, treeNode.Id);
        }
        catch (ArgumentException)
        {
            // Node is not an Orchard Core content item, create tree node directly
            logger.LogWarning("MindMapNode {NodeId} not found as Orchard Core content item, creating TreeNode directly", nodeId);
            treeNode = await decompositionEngine.CreateTreeNodeAsync(projectId, node.Text, node.Notes ?? "", node.Id, node.SourceReference, node.SourceText);
        }
        
        // Promote tree node to Kanban cards using proper pipeline
        List<KanbanCard> kanbanCards = [];
        try
        {
            kanbanCards = await decompositionEngine.PromoteTreeToKanbanAsync(treeNode.Id);
            logger.LogInformation("Successfully promoted TreeNode {TreeNodeId} to {CardCount} Kanban cards", 
                treeNode.Id, kanbanCards.Count);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the promotion
            logger.LogError(ex, "Failed to promote TreeNode {TreeNodeId} to Kanban, falling back to direct card creation", treeNode.Id);
            
            // Fallback: Create Kanban card directly
            var kanbanCard = await CreateKanbanCardFromNode(node, projectId);
            if (kanbanCard != null)
            {
                kanbanCards.Add(MapToKanbanCard(kanbanCard));
            }
        }
        
        node.IsPromoted = true;
        node.PromotedToId = treeNode.Id;
        node.Status = "Approved";
        
        // Store Kanban card IDs in Notes field
        var kanbanCardIds = kanbanCards.Select(c => c.Id).ToList();
        if (kanbanCardIds.Count > 0)
        {
            var kanbanIdPrefix = string.Join(",", kanbanCardIds.Select(id => $"KanbanCard:{id}"));
            node.Notes = kanbanIdPrefix + (string.IsNullOrEmpty(node.Notes) ? "" : $"\n\n{node.Notes}");
        }
        
        await mindMapService.UpdateNodeAsync(mapId, node);
        
        var refinementUrl = Url.Action(nameof(Index), "Refinement", new { area = "Compost.Kanban", id = treeNode.Id });
        return Json(new { 
            treeNodeId = treeNode.Id, 
            url = refinementUrl ?? $"/Kanban/Refinement/{treeNode.Id}",
            kanbanCardIds = kanbanCardIds,
            kanbanCardCount = kanbanCards.Count
        });
    }

    private static KanbanCard MapToKanbanCard(ContentItem cardItem)
    {
        var part = cardItem.As<KanbanCardPart>();
        return new KanbanCard
        {
            Id = cardItem.ContentItemId,
            Title = cardItem.DisplayText ?? "",
            Status = part?.Status ?? KanbanStatus.Backlog,
            StoryPoints = part?.StoryPoints ?? 0,
            SourceTreeNodeId = part?.SourceTreeNodeId
        };
    }

    /// <summary>POST /MindMap/ApiPromoteToStructure - Promote mind map node to structure. Query: mapId, nodeId.</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiPromoteToStructure([FromQuery] string mapId, [FromQuery] string nodeId)
    {
        if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(nodeId))
            return BadRequest();

        var mindMap = await mindMapService.GetMindMapAsync(mapId);
        var node = mindMap?.Nodes?.FirstOrDefault(n => n.Id == nodeId);
        if (node == null) return NotFound();

        // Check if node has children (hierarchical structure requirement)
        var childNodes = mindMap?.Nodes?.Where(n => n.ParentId == nodeId).ToList() ?? [];
        if (childNodes.Count == 0)
            return BadRequest(new { error = "Only nodes with children can be promoted to structure" });

        var projectId = mindMap?.WorkContextId ?? "default";

        // First create tree node, then promote to structure
        var treeNode = await decompositionEngine.CreateTreeNodeAsync(projectId, node.Text, node.Notes ?? "", node.Id, node.SourceReference, node.SourceText);
        var structureNode = await decompositionEngine.PromoteTreeToStructureAsync(treeNode.Id);

        // Update mind map node
        node.IsPromoted = true;
        node.PromotedToId = structureNode.Id;
        node.Status = "Structure";
        await mindMapService.UpdateNodeAsync(mapId, node);

        return Json(new
        {
            structureId = structureNode.Id,
            treeNodeId = treeNode.Id,
            message = $"Promoted to structure: {structureNode.Title}"
        });
    }

    /// <summary>POST /MindMap/ApiPromoteToKanban - Promote mind map node directly to Kanban card. Query: mapId, nodeId.</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiPromoteToKanban([FromQuery] string mapId, [FromQuery] string nodeId)
    {
        if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(nodeId))
            return BadRequest();

        var mindMap = await mindMapService.GetMindMapAsync(mapId);
        var node = mindMap?.Nodes?.FirstOrDefault(n => n.Id == nodeId);
        if (node == null) return NotFound();

        var projectId = mindMap?.WorkContextId ?? "default";
        
        try
        {
            // Create tree node properly with mind map node data
            var description = !string.IsNullOrEmpty(node.SourceText) ? node.SourceText : 
                              !string.IsNullOrEmpty(node.Notes) ? node.Notes : node.Text;
            
            var treeNode = await decompositionEngine.CreateTreeNodeAsync(
                projectId, 
                node.Text, 
                description, 
                node.Id, 
                node.SourceReference, 
                node.SourceText);
            
            if (treeNode == null)
            {
                return BadRequest(new { error = "Failed to create tree node from mind map node" });
            }
            
            var cards = await decompositionEngine.PromoteTreeToKanbanAsync(treeNode.Id);
            if (cards == null || cards.Count == 0)
            {
                return BadRequest(new { error = "Failed to create Kanban cards from tree node" });
            }
        
            // Update mind map node to reflect promotion
            node.IsPromoted = true;
            node.PromotedToId = cards.FirstOrDefault()?.Id;
            node.Status = "Kanban";
            await mindMapService.UpdateNodeAsync(mapId, node);

            var refinementUrl = Url.Action(nameof(Index), "Refinement", new { area = "Compost.Kanban", id = treeNode.Id });
            
            return Json(new
            {
                kanbanCardId = cards.FirstOrDefault()?.Id,
                cardCount = cards.Count,
                treeNodeId = treeNode.Id,
                url = refinementUrl ?? $"/Kanban/Refinement/{treeNode.Id}",
                message = "Promoted '" + (node.Text ?? "") + "' to Kanban card"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Promotion failed", details = ex.Message });
        }
    }

    /// <summary>
    /// POST /MindMap/ApiCreateMeetingNodes - Creates proper Orchard Core MindMapNode content items from meeting-extracted nodes.
    /// This bridges meeting transcription to the full decomposition pipeline.
    /// Query: meetingId
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiCreateMeetingNodes([FromQuery] string meetingId)
    {
        if (string.IsNullOrEmpty(meetingId))
            return BadRequest(new { error = "Meeting ID is required" });

        try
        {
            // First, ensure mind map nodes are extracted from the meeting
            var meeting = await transcriptionService.GetMeetingByIdAsync(meetingId);
            if (meeting == null)
                return NotFound(new { error = "Meeting not found" });

            // Extract nodes if not already done
            if (meeting.ExtractedNodes == null || meeting.ExtractedNodes.Count == 0)
            {
                logger.LogInformation("No extracted nodes found for meeting {MeetingId}, extracting now...", meetingId);
                await transcriptionService.ExtractMindMapNodesAsync(meetingId);
                meeting = await transcriptionService.GetMeetingByIdAsync(meetingId); // Refresh
            }

            // Create proper Orchard Core MindMapNode content items
            var createdNodeIds = await transcriptionService.CreateMindMapNodeContentItemsAsync(meetingId);
            
            if (createdNodeIds.Count == 0)
            {
                return BadRequest(new { error = "No nodes could be created from meeting. Ensure the meeting has transcript segments." });
            }

            logger.LogInformation("Created {Count} MindMapNode content items from meeting {MeetingId}", 
                createdNodeIds.Count, meetingId);

            // Also create a mind map collection for visualization
            var mindMap = await mindMapService.CreateMindMapFromMeetingNodesAsync(
                meeting.ExtractedNodes, 
                $"Meeting: {meeting.Title}", 
                meeting.WorkContextId, 
                meetingId);

            return Json(new
            {
                success = true,
                meetingId = meetingId,
                mindMapId = mindMap.Id,
                mindMapUrl = Url.Action(nameof(ViewMap), new { id = mindMap.Id }),
                createdNodeCount = createdNodeIds.Count,
                createdNodeIds = createdNodeIds,
                message = $"Created {createdNodeIds.Count} MindMapNode content items from meeting. Use these IDs with PromoteMindMapToTreeAsync."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating mind map nodes from meeting {MeetingId}", meetingId);
            return StatusCode(500, new { error = "Failed to create nodes", details = ex.Message });
        }
    }

    /// <summary>DELETE /MindMap/ApiDeleteNode - Remove node from map. Query: mapId, nodeId.</summary>
    [HttpDelete]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiDeleteNode([FromQuery] string mapId, [FromQuery] string nodeId)
    {
        if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(nodeId))
            return BadRequest();
        await mindMapService.RemoveNodeAsync(mapId, nodeId);
        return Ok();
    }

    /// <summary>POST /MindMap/ApiUpdateNodeStyle - Update node styling.</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiUpdateNodeStyle([FromBody] NodeStyleUpdateDto dto)
    {
        if (string.IsNullOrEmpty(dto.MapId) || string.IsNullOrEmpty(dto.NodeId))
            return BadRequest();

        var mindMap = await mindMapService.GetMindMapAsync(dto.MapId);
        var node = mindMap?.Nodes.FirstOrDefault(n => n.Id == dto.NodeId);

        if (node == null)
            return NotFound();

        // Update node style properties
        if (!string.IsNullOrEmpty(dto.Color))
            node.Color = dto.Color;
        if (!string.IsNullOrEmpty(dto.NodeType))
            node.NodeType = dto.NodeType;
        if (!string.IsNullOrEmpty(dto.Shape))
            node.Shape = dto.Shape;
        if (dto.FontSize.HasValue)
            node.FontSize = dto.FontSize.Value;
        if (dto.Size.HasValue)
            node.Size = dto.Size.Value;

        await mindMapService.UpdateNodeAsync(dto.MapId, node);
        return Ok(new { message = "Node style updated" });
    }

    /// <summary>POST /MindMap/ApiApplyLayout - Apply layout to mind map.</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiApplyLayout([FromBody] LayoutDto dto)
    {
        if (string.IsNullOrEmpty(dto.MapId) || string.IsNullOrEmpty(dto.LayoutType))
            return BadRequest();

        var mindMap = await mindMapService.GetMindMapAsync(dto.MapId);
        if (mindMap == null)
            return NotFound();

        // Apply layout algorithm
        var layoutPositions = ApplyLayoutAlgorithm(mindMap.Nodes.ToList(), dto.LayoutType);

        // Update node positions
        foreach (var node in mindMap.Nodes)
            if (layoutPositions.TryGetValue(node.Id, out var position))
            {
                node.PositionX = position.X;
                node.PositionY = position.Y;
            }

        await mindMapService.UpdateMindMapAsync(mindMap);
        return Ok(new { message = $"Applied {dto.LayoutType} layout", positions = layoutPositions });
    }

    /// <summary>GET /MindMap/ExportAsImage - Export mind map as image.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportAsImage(string id, string format = "png")
    {
        var mindMap = await mindMapService.GetMindMapAsync(id);
        if (mindMap == null)
            return NotFound();

        // This would integrate with a library like html2canvas or similar
        // For now, return a placeholder response
        return Json(new
        {
            message = "Export functionality requires client-side implementation",
            mapId = id,
            format,
            nodes = mindMap.Nodes.Count,
            edges = mindMap.Edges.Count
        });
    }

    /// <summary>GET /MindMap/ExportAsJson - Export mind map as JSON.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportAsJson(string id)
    {
        var mindMap = await mindMapService.GetMindMapAsync(id);
        if (mindMap == null)
            return NotFound();

        var json = JsonSerializer.Serialize(mindMap, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var bytes = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"mindmap_{mindMap.Name}_{DateTime.Now:yyyyMMdd}.json");
    }

    /// <summary>POST /MindMap/ImportFromJson - Import mind map from JSON.</summary>
    [HttpPost]
    public async Task<IActionResult> ImportFromJson(IFormFile file, string projectId)
    {
        if (file is not { Length: not 0 })
            return BadRequest("No file uploaded");

        try
        {
            var json = await new StreamReader(file.OpenReadStream()).ReadToEndAsync();
            var mindMap = JsonSerializer.Deserialize<MindMapCollection>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (mindMap == null)
                return BadRequest("Invalid JSON format");

            // Generate new ID and set context
            mindMap.Id = Guid.NewGuid().ToString();
            mindMap.WorkContextId = projectId;
            mindMap.CreatedAt = DateTime.UtcNow;
            mindMap.UpdatedAt = DateTime.UtcNow;

            await mindMapService.SaveMindMapAsync(mindMap);
            return RedirectToAction(nameof(ViewMap), new { id = mindMap.Id });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error importing JSON: {ex.Message}");
        }
    }

    /// <summary>GET /MindMap/ApiGetTemplates - Get available node templates.</summary>
    [HttpGet]
    public IActionResult ApiGetTemplates()
    {
        var templates = new[]
        {
            new { id = "concept", name = "Concept", color = "#4CAF50", shape = "ellipse", icon = "💡" },
            new { id = "feature", name = "Feature", color = "#2196F3", shape = "rectangle", icon = "🚀" },
            new { id = "task", name = nameof(Task), color = "#FF9800", shape = "roundrectangle", icon = "✓" },
            new { id = "issue", name = "Issue", color = "#F44336", shape = "diamond", icon = "⚠" },
            new { id = "decision", name = "Decision", color = "#9C27B0", shape = "triangle", icon = "🔀" },
            new { id = "resource", name = "Resource", color = "#607D8B", shape = "hexagon", icon = "📚" },
            new { id = "user", name = nameof(User), color = "#795548", shape = "round-rectangle", icon = "👤" },
            new { id = "data", name = "Data", color = "#009688", shape = "barrel", icon = "📊" }
        };

        return Json(templates);
    }

    private Dictionary<string, NodePositionDto> ApplyLayoutAlgorithm(List<MindMapNodeModel> nodes,
        string layoutType)
    {
        var positions = new Dictionary<string, NodePositionDto>();
        var rootNode = nodes.FirstOrDefault(n => n.NodeType == "Root");

        if (rootNode == null)
            return positions;

        switch (layoutType.ToLower())
        {
            case "radial":
                return ApplyRadialLayout(nodes, rootNode);
            case "grid":
                return ApplyGridLayout(nodes);
            case "hierarchical":
                return ApplyHierarchicalLayout(nodes, rootNode);
            case "circular":
                return ApplyCircularLayout(nodes, rootNode);
            case "force":
                return ApplyForceDirectedLayout(nodes);
            default:
                return ApplyRadialLayout(nodes, rootNode);
        }
    }

    private Dictionary<string, NodePositionDto> ApplyRadialLayout(List<MindMapNodeModel> nodes,
        MindMapNodeModel root)
    {
        var positions = new Dictionary<string, NodePositionDto>();
        var centerX = 400.0;
        var centerY = 300.0;
        var radiusStep = 100.0;

        positions[root.Id] = new NodePositionDto { X = centerX, Y = centerY };

        var levelGroups = nodes.Where(n => n.Id != root.Id)
            .GroupBy(n => GetNodeLevel(n, root, nodes))
            .OrderBy(g => g.Key);

        foreach (var group in levelGroups)
        {
            var level = group.Key;
            var radius = radiusStep * level;
            var angleStep = 2 * Math.PI / group.Count();
            var startAngle = -Math.PI / 2;

            for (var i = 0; i < group.Count(); i++)
            {
                var angle = startAngle + angleStep * i;
                var x = centerX + radius * Math.Cos(angle);
                var y = centerY + radius * Math.Sin(angle);
                positions[group.ElementAt(i).Id] = new NodePositionDto { X = x, Y = y };
            }
        }

        return positions;
    }

    private Dictionary<string, NodePositionDto> ApplyGridLayout(List<MindMapNodeModel> nodes)
    {
        var positions = new Dictionary<string, NodePositionDto>();
        var cols = Math.Ceiling(Math.Sqrt(nodes.Count));
        var spacing = 150.0;
        var startX = 100.0;
        var startY = 100.0;

        for (var i = 0; i < nodes.Count; i++)
        {
            var row = i / cols;
            var col = i % cols;
            positions[nodes[i].Id] = new NodePositionDto { X = startX + col * spacing, Y = startY + row * spacing };
        }

        return positions;
    }

    private Dictionary<string, NodePositionDto> ApplyHierarchicalLayout(List<MindMapNodeModel> nodes,
        MindMapNodeModel root)
    {
        var positions = new Dictionary<string, NodePositionDto>();
        var levelHeight = 120.0;
        var nodeSpacing = 150.0;

        positions[root.Id] = new NodePositionDto { X = 400, Y = 50 };

        var levelGroups = nodes.Where(n => n.Id != root.Id)
            .GroupBy(n => GetNodeLevel(n, root, nodes))
            .OrderBy(g => g.Key);

        foreach (var group in levelGroups)
        {
            var level = group.Key;
            var y = 50 + level * levelHeight;
            var totalWidth = (group.Count() - 1) * nodeSpacing;
            var startX = 400 - totalWidth / 2;

            for (var i = 0; i < group.Count(); i++)
            {
                var x = startX + i * nodeSpacing;
                positions[group.ElementAt(i).Id] = new NodePositionDto { X = x, Y = y };
            }
        }

        return positions;
    }

    private Dictionary<string, NodePositionDto> ApplyCircularLayout(List<MindMapNodeModel> nodes,
        MindMapNodeModel root)
    {
        var positions = new Dictionary<string, NodePositionDto>();
        var centerX = 400.0;
        var centerY = 300.0;
        var radius = 200.0;

        positions[root.Id] = new NodePositionDto { X = centerX, Y = centerY };

        var otherNodes = nodes.Where(n => n.Id != root.Id).ToList();
        var angleStep = 2 * Math.PI / otherNodes.Count;

        for (var i = 0; i < otherNodes.Count; i++)
        {
            var angle = angleStep * i;
            var x = centerX + radius * Math.Cos(angle);
            var y = centerY + radius * Math.Sin(angle);
            positions[otherNodes[i].Id] = new NodePositionDto { X = x, Y = y };
        }

        return positions;
    }

    private Dictionary<string, NodePositionDto> ApplyForceDirectedLayout(List<MindMapNodeModel> nodes)
    {
        // Simple force-directed layout simulation
        var positions = new Dictionary<string, NodePositionDto>();
        var random = new Random();

        // Initialize random positions
        foreach (var node in nodes) positions[node.Id] = new NodePositionDto { X = random.Next(100, 700), Y = random.Next(100, 500) };

        // Apply basic repulsion/attraction forces
        for (var iteration = 0; iteration < 50; iteration++)
        {
            var newPositions = new Dictionary<string, NodePositionDto>(positions);

            foreach (var node1 in nodes)
            {
                var pos1 = positions[node1.Id];
                var x1 = pos1.X;
                var y1 = pos1.Y;
                var fx = 0.0;
                var fy = 0.0;

                // Repulsion between all nodes
                foreach (var node2 in nodes)
                {
                    if (node1.Id == node2.Id) continue;

                    var pos2 = positions[node2.Id];
                    var x2 = pos2.X;
                    var y2 = pos2.Y;
                    var dx = x1 - x2;
                    var dy = y1 - y2;
                    var distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance is > 0 and < 200)
                    {
                        var force = 50.0 / distance;
                        fx += dx / distance * force;
                        fy += dy / distance * force;
                    }
                }

                // Attraction along edges
                foreach (var edge in node1.Edges ?? [])
                {
                    var targetNode = nodes.FirstOrDefault(n => n.Id == edge.ToNodeId);
                    if (targetNode != null)
                    {
                        var pos2 = positions[targetNode.Id];
                        var x2 = pos2.X;
                        var y2 = pos2.Y;
                        var dx = x2 - x1;
                        var dy = y2 - y1;
                        var distance = Math.Sqrt(dx * dx + dy * dy);

                        if (distance > 100)
                        {
                            var force = (distance - 100) * 0.01;
                            fx += dx * force;
                            fy += dy * force;
                        }
                    }
                }

                // Update position
                var newX = Math.Max(50, Math.Min(750, x1 + fx * 0.1));
                var newY = Math.Max(50, Math.Min(550, y1 + fy * 0.1));
                newPositions[node1.Id] = new NodePositionDto { X = newX, Y = newY };
            }

            positions = newPositions;
        }

        return positions;
    }

    private int GetNodeLevel(MindMapNodeModel node, MindMapNodeModel root, List<MindMapNodeModel> allNodes)
    {
        if (node.Id == root.Id) return 0;

        var level = 0;
        var current = node;
        var visited = new HashSet<string>();

        while (current != null && !visited.Contains(current.Id) && level < 10)
        {
            visited.Add(current.Id);
            if (current.ParentId == root.Id) return level + 1;
            if (string.IsNullOrEmpty(current.ParentId)) return level + 1;

            current = allNodes.FirstOrDefault(n => n.Id == current.ParentId);
            level++;
        }

        return level;
    }

    /// <summary>
    /// Creates a Kanban card from a mind map node with transcript excerpt
    /// </summary>
    private async Task<ContentItem?> CreateKanbanCardFromNode(MindMapNodeModel node, string projectId)
    {
        try
        {
            // Create new Kanban card content item
            var cardItem = await contentManager.NewAsync(nameof(KanbanCard));
            
            // Set basic properties
            cardItem.DisplayText = node.Text;
            
            // Set markdown content - prioritize SourceText (transcript content), then Notes, then Text
            if (cardItem.Content.MarkdownBodyPart != null)
            {
                var description = !string.IsNullOrEmpty(node.SourceText) ? node.SourceText : 
                                  !string.IsNullOrEmpty(node.Notes) ? node.Notes : node.Text;
                cardItem.Content.MarkdownBodyPart.Markdown = description;
            }
            
            // Configure Kanban card part
            var cardPart = cardItem.As<KanbanCardPart>();
            cardPart.WorkContextId = projectId;
            cardPart.Status = KanbanStatus.Backlog;
            cardPart.OrderInColumn = 0;
            cardPart.Priority = PriorityLevel.Medium;
            
            // Set transcript excerpt if available
            if (!string.IsNullOrEmpty(node.SourceText))
            {
                cardPart.SourceTranscriptExcerpt = node.SourceText;
            }
            
            // Set acceptance criteria based on node type
            cardPart.AcceptanceCriteria = GenerateAcceptanceCriteria(node);
            
            // Apply changes and create
            cardItem.Apply(cardPart);
            await contentManager.CreateAsync(cardItem);
            await contentManager.PublishAsync(cardItem);
            
            return cardItem;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating Kanban card: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generates acceptance criteria based on mind map node type
    /// </summary>
    private List<string> GenerateAcceptanceCriteria(MindMapNodeModel node)
    {
        var criteria = new List<string>();
        
        switch (node.NodeType)
        {
            case "Requirement":
                criteria.Add("Requirement is clearly defined and testable");
                criteria.Add("Implementation meets the specified requirement");
                criteria.Add("Acceptance tests are written and passing");
                break;
                
            case "Action":
                criteria.Add("Action item is completed as specified");
                criteria.Add("Results are documented and verified");
                criteria.Add("Stakeholders have reviewed and approved");
                break;
                
            case "Idea":
                criteria.Add("Idea is feasible and well-defined");
                criteria.Add("Proof of concept is demonstrated");
                criteria.Add("Business value is validated");
                break;
                
            case "Decision":
                criteria.Add("Decision is documented with rationale");
                criteria.Add("All stakeholders are informed");
                criteria.Add("Implementation plan is in place");
                break;
                
            default:
                criteria.Add("Task is completed according to specifications");
                criteria.Add("Quality standards are met");
                criteria.Add("Work is reviewed and approved");
                break;
        }
        
        return criteria;
    }
}

public class NodeStyleUpdateDto
{
    public string MapId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? NodeType { get; set; }
    public string? Shape { get; set; }
    public int? FontSize { get; set; }
    public int? Size { get; set; }
}

public class LayoutDto
{
    public string MapId { get; set; } = string.Empty;
    public string LayoutType { get; set; } = string.Empty;
}

public class MindMapNodeApiDto
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Label { get; set; }
    public string? Content { get; set; }
    public string? NodeType { get; set; }
    public string? Color { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public string? ParentId { get; set; }
}

public class NodePositionDto
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class MindMapRequestModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WorkContextId { get; set; } = string.Empty;
}

public class CreateMindMapViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? WorkContextId { get; set; }
    public string SourceType { get; set; } = "Text"; // Text, Transcript, Requirements
    public string SourceText { get; set; } = string.Empty;
}
