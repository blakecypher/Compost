using System.Threading.Tasks;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Compost.Structure.Controllers;

public class StructureController : Controller
{
    private readonly IDecompositionEngine _decompositionEngine;
    private readonly ILogger<StructureController> _logger;

    public StructureController(IDecompositionEngine decompositionEngine, ILogger<StructureController> logger)
    {
        _decompositionEngine = decompositionEngine;
        _logger = logger;
    }

    // GET: /Structure
    public async Task<IActionResult> Index()
    {
        // Get all structures (in production, would query from database)
        var structures = new List<StructureNode>();
        return View(structures);
    }

    // GET: /Structure/Detail/abc123
    public async Task<IActionResult> Detail(string id)
    {
        // Get structure by ID (stub implementation)
        var structure = new StructureNode
        {
            Id = id,
            Title = $"Structure {id}",
            Description = "Sample structure node",
            StructureType = StructureType.Team
        };
        
        return View(structure);
    }

    // GET: /Structure/CreateBoard/abc123
    public async Task<IActionResult> CreateBoard(string structureId)
    {
        if (string.IsNullOrEmpty(structureId))
        {
            return BadRequest("Structure ID is required");
        }

        try
        {
            var board = await _decompositionEngine.CreateKanbanBoardForStructureAsync(structureId);
            TempData["SuccessMessage"] = $"Kanban board created for structure {structureId}";
            return RedirectToAction(nameof(KanbanBoard), new { boardId = board.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create kanban board for structure {StructureId}", structureId);
            TempData["ErrorMessage"] = "Failed to create kanban board";
            return RedirectToAction(nameof(Detail), new { id = structureId });
        }
    }

    // GET: /Structure/PromoteToKanban/abc123
    public async Task<IActionResult> PromoteToKanban(string structureId)
    {
        if (string.IsNullOrEmpty(structureId))
        {
            return BadRequest("Structure ID is required");
        }

        try
        {
            var cards = await _decompositionEngine.PromoteStructureToKanbanAsync(structureId);
            TempData["SuccessMessage"] = $"Created {cards.Count} kanban cards from structure {structureId}";
            return RedirectToAction(nameof(Detail), new { id = structureId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to promote structure {StructureId} to kanban", structureId);
            TempData["ErrorMessage"] = "Failed to promote to kanban";
            return RedirectToAction(nameof(Detail), new { id = structureId });
        }
    }

    // GET: /Structure/KanbanBoard/abc123
    public async Task<IActionResult> KanbanBoard(string boardId)
    {
        if (string.IsNullOrEmpty(boardId))
        {
            return BadRequest("Board ID is required");
        }

        try
        {
            var board = await _decompositionEngine.GetKanbanBoardAsync(boardId);
            if (board == null)
            {
                return NotFound("Kanban board not found");
            }

            var cards = await _decompositionEngine.GetKanbanCardsByBoardAsync(boardId);
            
            ViewBag.Cards = cards;
            return View(board);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load kanban board {BoardId}", boardId);
            TempData["ErrorMessage"] = "Failed to load kanban board";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: /Structure/AddChild
    [HttpPost]
    public async Task<IActionResult> AddChild(string parentStructureId, string childStructureId)
    {
        if (string.IsNullOrEmpty(parentStructureId) || string.IsNullOrEmpty(childStructureId))
        {
            return BadRequest("Parent and child structure IDs are required");
        }

        try
        {
            await _decompositionEngine.AddChildStructureAsync(parentStructureId, childStructureId);
            TempData["SuccessMessage"] = "Child structure added successfully";
            return RedirectToAction(nameof(Detail), new { id = parentStructureId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add child structure {ChildId} to parent {ParentId}", 
                childStructureId, parentStructureId);
            TempData["ErrorMessage"] = "Failed to add child structure";
            return RedirectToAction(nameof(Detail), new { id = parentStructureId });
        }
    }
}
