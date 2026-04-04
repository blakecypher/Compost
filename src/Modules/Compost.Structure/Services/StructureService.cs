using Compost.Core.Models;
using Microsoft.Extensions.Logging;

namespace Compost.Structure.Services;

public class StructureService : IStructureService
{
    private readonly ILogger<StructureService> _logger;
    private readonly Dictionary<string, StructureNode> _structures = new();

    public StructureService(ILogger<StructureService> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<StructureNode>> GetAllStructuresAsync()
    {
        return Task.FromResult(_structures.Values.AsEnumerable());
    }

    public Task<StructureNode?> GetStructureByIdAsync(string id)
    {
        _structures.TryGetValue(id, out var structure);
        return Task.FromResult(structure);
    }

    public Task<StructureNode> CreateStructureAsync(string title, string description, StructureType type)
    {
        var structure = new StructureNode
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Description = description,
            StructureType = type
        };

        _structures[structure.Id] = structure;
        _logger.LogInformation("Created structure {StructureId} of type {Type}", structure.Id, type);

        return Task.FromResult(structure);
    }

    public Task UpdateStructureAsync(StructureNode structure)
    {
        if (structure?.Id == null)
        {
            throw new ArgumentException("Structure and ID cannot be null");
        }

        _structures[structure.Id] = structure;
        _logger.LogInformation("Updated structure {StructureId}", structure.Id);

        return Task.CompletedTask;
    }

    public Task DeleteStructureAsync(string id)
    {
        _structures.Remove(id);
        _logger.LogInformation("Deleted structure {StructureId}", id);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<StructureNode>> GetChildStructuresAsync(string parentId)
    {
        if (!_structures.TryGetValue(parentId, out var parent))
        {
            return Task.FromResult(Enumerable.Empty<StructureNode>());
        }

        var children = parent.ChildStructureIds
            .Select(childId => _structures.GetValueOrDefault(childId))
            .Where(s => s != null)
            .Cast<StructureNode>();

        return Task.FromResult(children);
    }

    public Task AddChildStructureAsync(string parentId, string childId)
    {
        if (!_structures.TryGetValue(parentId, out var parent))
        {
            throw new InvalidOperationException($"Parent structure {parentId} not found");
        }

        if (!_structures.ContainsKey(childId))
        {
            throw new InvalidOperationException($"Child structure {childId} not found");
        }

        parent.ChildStructureIds.Add(childId);
        _logger.LogInformation("Added child {ChildId} to parent {ParentId}", childId, parentId);

        return Task.CompletedTask;
    }

    public Task RemoveChildStructureAsync(string parentId, string childId)
    {
        if (_structures.TryGetValue(parentId, out var parent))
        {
            parent.ChildStructureIds.Remove(childId);
            _logger.LogInformation("Removed child {ChildId} from parent {ParentId}", childId, parentId);
        }

        return Task.CompletedTask;
    }
}
