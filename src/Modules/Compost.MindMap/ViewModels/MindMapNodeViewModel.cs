using System.ComponentModel.DataAnnotations;

namespace Compost.MindMap.ViewModels;

public class MindMapNodeViewModel
{
    public string MindMapId { get; set; } = string.Empty;
    public string? NodeId { get; set; }
    
    [Required(ErrorMessage = "Node text is required")]
    public string Text { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Node type is required")]
    public string NodeType { get; set; } = "Idea";
    
    public string? ParentNodeId { get; set; }
    
    public double PositionX { get; set; } = 400;
    
    public double PositionY { get; set; } = 300;
    
    public string? Tags { get; set; }
    
    public string? SourceText { get; set; }
}
