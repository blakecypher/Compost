using System;
using System.Collections.Generic;
using Compost.Core.Models;
using OrchardCore.ContentManagement;
using Newtonsoft.Json;

namespace Compost.Kanban.Models;

/// <summary>
/// Handles reading SourceMeetingId as either string or object (for backward compatibility)
/// </summary>
public class TreeNodeSourceMeetingIdConverter : JsonConverter<string?>
{
    public override string? ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            return reader.Value?.ToString();
        }
        if (reader.TokenType == JsonToken.StartObject)
        {
            // Skip the object and return null (corrupted data)
            reader.Skip();
            return null;
        }
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }
        // For any other type, try to read as string
        return reader.Value?.ToString();
    }

    public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }
}

/// <summary>
/// Content part for Tree Node - used for detailed requirement refinement
/// </summary>
public class TreeNodePart : ContentPart
{
    /// <summary>
    /// Reference to the project context
    /// </summary>
    [JsonProperty("workContextId")]
    public string? WorkContextId { get; set; }

    /// <summary>
    /// Reference to the originating mind map node
    /// </summary>
    [JsonProperty("sourceMindMapNodeId")]
    public string? SourceMindMapNodeId { get; set; }

    /// <summary>
    /// Reference to the originating meeting ID
    /// </summary>
    [JsonProperty("sourceMeetingId")]
    [JsonConverter(typeof(TreeNodeSourceMeetingIdConverter))]
    public string? SourceMeetingId { get; set; }

    /// <summary>
    /// The excerpt from the transcript that generated this requirement
    /// </summary>
    [JsonProperty("sourceTranscriptExcerpt")]
    public string? SourceTranscriptExcerpt { get; set; }

    /// <summary>
    /// Parent tree node ID (for hierarchy)
    /// </summary>
    [JsonProperty("parentNodeId")]
    public string? ParentNodeId { get; set; }

    /// <summary>
    /// Acceptance criteria for this requirement
    /// </summary>
    [JsonProperty("acceptanceCriteria")]
    public List<string> AcceptanceCriteria { get; set; } = [];

    /// <summary>
    /// Technical requirements or implementation notes
    /// </summary>
    [JsonProperty("technicalRequirements")]
    public List<string> TechnicalRequirements { get; set; } = [];

    /// <summary>
    /// Estimated complexity/size
    /// </summary>
    [JsonProperty("complexity")]
    public ComplexityLevel Complexity { get; set; } = ComplexityLevel.Unknown;

    /// <summary>
    /// Priority level
    /// </summary>
    [JsonProperty("priority")]
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

    /// <summary>
    /// Whether this has been promoted to one or more kanban cards
    /// </summary>
    [JsonProperty("isPromotedToKanban")]
    public bool IsPromotedToKanban { get; set; }

    /// <summary>
    /// Related kanban card content item IDs
    /// </summary>
    [JsonProperty("kanbanCardIds")]
    public List<string> KanbanCardIds { get; set; } = [];

    /// <summary>
    /// Whether this has been promoted to a structure node
    /// </summary>
    [JsonProperty("isPromotedToStructure")]
    public bool IsPromotedToStructure { get; set; }

    /// <summary>
    /// Reference to the structure node if promoted
    /// </summary>
    [JsonProperty("structureNodeId")]
    public string? StructureNodeId { get; set; }
}
