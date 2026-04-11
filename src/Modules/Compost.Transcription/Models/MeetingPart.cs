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
    public string MeetingId { get; set; } = string.Empty;
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string WorkContextId { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
    
    public DateTime? StartedAt { get; set; }
    
    public DateTime? EndedAt { get; set; }
    
    public int DurationSeconds { get; set; }
    
    public DateTime? TranscriptionCompletedAt { get; set; }
    
    public bool IsProcessed { get; set; }

    public string TranscriptText { get; set; }
    
    public string TranscriptJson { get; set; }
    
    [JsonIgnore]
    public List<TranscriptSegment> Transcript
    {
        get
        {
            // First check the strongly typed property TranscriptJson
            string rawData = TranscriptJson;
            
            // FALLBACK: If TranscriptJson is empty, check the underlying Content JObject directly (handles casing mismatches)
            if (string.IsNullOrEmpty(rawData) && Content != null)
            {
                // Try both standard and lowercase keys
                rawData = Content["TranscriptJson"]?.ToString() ?? Content["transcriptJson"]?.ToString();
            }

            if (string.IsNullOrEmpty(rawData)) 
            {
                return [];
            }

            try
            {
                return JsonConvert.DeserializeObject<List<TranscriptSegment>>(rawData) ?? [];
            }
            catch
            {
                return [];
            }
        }
        set 
        { 
            TranscriptJson = value?.Count > 0 ? JsonConvert.SerializeObject(value) : null;
        }
    }
    
    public List<ActionItem> ActionItems { get; set; } = [];
    
    public List<MindMapNode> ExtractedNodes { get; set; } = [];
    
    /// <summary>
    /// IDs of Orchard Core MindMapNode content items created from this meeting.
    /// These can be promoted through the decomposition pipeline.
    /// </summary>
    public List<string> ExtractedNodeIds { get; set; } = [];
    
    public string AudioFilePath { get; set; }
    
    public string Notes { get; set; }
    
    public string Summary { get; set; }
    
    [DefaultValue(true)]
    public bool AutoExtractMindMapNodes { get; set; } = true;
    
    [DefaultValue(true)]
    public bool AutoExtractActionItems { get; set; } = true;
}
