using OrchardCore.ContentManagement;

namespace Compost.Structure.Models;

/// <summary>
/// Content part for Structure Node - represents hierarchical team/department organization
/// </summary>
public class StructureNodePart : ContentPart
{
    // Fields are defined via migrations and accessed through ContentItem.Fields
    // StructureType, ParentStructureId, ChildStructureIds, KanbanBoardId, 
    // WorkContextId, MemberIds, LeadId, Color
}
