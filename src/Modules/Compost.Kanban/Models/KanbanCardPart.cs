using System;
using System.Collections.Generic;
using Compost.Core.Models;
using Newtonsoft.Json;
using OrchardCore.ContentManagement;

namespace Compost.Kanban.Models;

/// <summary>
/// Handles reading SourceMeetingId as either string or object (for backward compatibility)
/// </summary>
public class SourceMeetingIdConverter : JsonConverter<string>
{
    public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.String:
                return reader.Value?.ToString();
            case JsonToken.StartObject:
                // Skip the object and return null (corrupted data)
                reader.Skip();
                return null;
            case JsonToken.Null:
                return null;
            case JsonToken.None:
            case JsonToken.StartArray:
            case JsonToken.StartConstructor:
            case JsonToken.PropertyName:
            case JsonToken.Comment:
            case JsonToken.Raw:
            case JsonToken.Integer:
            case JsonToken.Float:
            case JsonToken.Boolean:
            case JsonToken.Undefined:
            case JsonToken.EndObject:
            case JsonToken.EndArray:
            case JsonToken.EndConstructor:
            case JsonToken.Date:
            case JsonToken.Bytes:
            default:
                // For any other type, try to read as string
                return reader.Value?.ToString();
        }
    }

    public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }
}

/// <summary>
/// Content part for Kanban Card - represents an actionable task
/// </summary>
public class KanbanCardPart : ContentPart
{
    /// <summary>
    /// Reference to the project context
    /// </summary>
    [JsonProperty("workContextId")]
    public string WorkContextId { get; set; }

    /// <summary>
    /// Reference to the source tree node
    /// </summary>
    [JsonProperty("sourceTreeNodeId")]
    public string SourceTreeNodeId { get; set; }

    /// <summary>
    /// Reference to the source meeting if applicable
    /// </summary>
    [JsonProperty("sourceMeetingId")]
    [JsonConverter(typeof(SourceMeetingIdConverter))]
    public string SourceMeetingId { get; set; }

    /// <summary>
    /// The excerpt from the transcript that generated this card
    /// </summary>
    [JsonProperty("sourceTranscriptExcerpt")]
    public string SourceTranscriptExcerpt { get; set; }

    /// <summary>
    /// Story points estimation
    /// </summary>
    [JsonProperty("storyPoints")]
    public int? StoryPoints { get; set; }

    /// <summary>
    /// AI-suggested story points
    /// </summary>
    [JsonProperty("suggestedStoryPoints")]
    public int? SuggestedStoryPoints { get; set; }

    /// <summary>
    /// Priority level of the card
    /// </summary>
    [JsonProperty("priority")]
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

    /// <summary>
    /// Due date for the card
    /// </summary>
    [JsonProperty("dueDate")]
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Assigned user for this card
    /// </summary>
    [JsonProperty("assignee")]
    public string Assignee { get; set; }

    /// <summary>
    /// Tags for categorizing and filtering cards
    /// </summary>
    [JsonProperty("tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Current status column
    /// </summary>
    [JsonProperty("status")]
    public KanbanStatus Status { get; set; } = KanbanStatus.Backlog;

    /// <summary>
    /// Priority within the status column (ordering)
    /// </summary>
    [JsonProperty("orderInColumn")]
    public int OrderInColumn { get; set; }

    /// <summary>
    /// Acceptance criteria (can be synced from tree node or added specifically)
    /// </summary>
    [JsonProperty("acceptanceCriteria")]
    public List<string> AcceptanceCriteria { get; set; } = [];

    /// <summary>
    /// Checklist items for this task
    /// </summary>
    [JsonProperty("checklist")]
    public List<ChecklistItem> Checklist { get; set; } = [];

    /// <summary>
    /// Time spent on this task (in seconds)
    /// </summary>
    [JsonProperty("timeSpentSeconds")]
    public long TimeSpentSeconds { get; set; }

    /// <summary>
    /// Whether the card is currently blocked
    /// </summary>
    [JsonProperty("isBlocked")]
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Reason for being blocked
    /// </summary>
    [JsonProperty("blockedReason")]
    public string BlockedReason { get; set; }
}
