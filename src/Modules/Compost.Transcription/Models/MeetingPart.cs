using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Compost.Core.Models;
using Newtonsoft.Json;
using OrchardCore.ContentManagement;

namespace Compost.Transcription.Models;

public class MeetingPart : ContentPart
{
    [JsonProperty("meetingId")]
    public string MeetingId { get; set; } = string.Empty;
    
    [Required]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonProperty("workContextId")]
    public string WorkContextId { get; set; } = string.Empty;
    
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonProperty("audioFilePath")]
    public string AudioFilePath { get; set; }
    
    [JsonProperty("transcriptText")]
    public string TranscriptText { get; set; }
    
    [JsonProperty("transcriptJson")]
    public string TranscriptJson { get; set; }
    
    [JsonIgnore]
    public List<TranscriptSegment> Transcript
    {
        get
        {
            if (string.IsNullOrEmpty(TranscriptJson)) return [];
            try
            {
                return JsonConvert.DeserializeObject<List<TranscriptSegment>>(TranscriptJson) ?? [];
            }
            catch
            {
                return [];
            }
        }
        set => TranscriptJson = value?.Count > 0 ? JsonConvert.SerializeObject(value) : null;
    }
    
    [JsonProperty("actionItems")]
    public List<ActionItem> ActionItems { get; set; } = [];
    
    [JsonProperty("extractedNodes")]
    public List<MindMapNode> ExtractedNodes { get; set; } = [];
    
    [JsonProperty("startedAt")]
    public DateTime? StartedAt { get; set; }
    
    [JsonProperty("endedAt")]
    public DateTime? EndedAt { get; set; }
    
    [JsonProperty("durationSeconds")]
    public int DurationSeconds { get; set; }
    
    [JsonProperty("transcriptionCompletedAt")]
    public DateTime? TranscriptionCompletedAt { get; set; }
    
    [JsonProperty("isProcessed")]
    public bool IsProcessed { get; set; }
    
    [JsonProperty("notes")]
    public string Notes { get; set; }
    
    [JsonProperty("summary")]
    public string Summary { get; set; }
    
    [JsonProperty("autoExtractMindMapNodes", NullValueHandling = NullValueHandling.Ignore)]
    [DefaultValue(true)]
    public bool AutoExtractMindMapNodes { get; set; } = true;
    
    [JsonProperty("autoExtractActionItems", NullValueHandling = NullValueHandling.Ignore)]
    [DefaultValue(true)]
    public bool AutoExtractActionItems { get; set; } = true;
}
