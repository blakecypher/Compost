using Compost.Core.Models;

namespace Compost.Structure.Services;

public interface IStructureService
{
    Task<IEnumerable<StructureNode>> GetAllStructuresAsync();
    Task<StructureNode?> GetStructureByIdAsync(string id);
    Task<StructureNode> CreateStructureAsync(string title, string description, StructureType type);
    Task UpdateStructureAsync(StructureNode structure);
    Task DeleteStructureAsync(string id);
    Task<IEnumerable<StructureNode>> GetChildStructuresAsync(string parentId);
    Task AddChildStructureAsync(string parentId, string childId);
    Task RemoveChildStructureAsync(string parentId, string childId);
}
