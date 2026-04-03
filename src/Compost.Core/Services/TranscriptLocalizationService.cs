namespace Compost.Core.Services;

public class TranscriptLocalizationService : ITranscriptLocalizationService
{
    private string _currentCulture = "en-US";
    
    // Localized dictionaries for different cultures
    private static readonly Dictionary<string, Dictionary<string, string>> LocalizedStrings = new()
    {
        ["en-US"] = new Dictionary<string, string>
        {
            ["Transcript.Speaker.Unknown"] = "Unknown Speaker",
            ["Transcript.Time.Format"] = "{0:hh\\:mm\\:ss}",
            ["Transcript.Confidence.High"] = "High",
            ["Transcript.Confidence.Medium"] = "Medium", 
            ["Transcript.Confidence.Low"] = "Low",
            ["Transcript.Status.Recording"] = "Recording",
            ["Transcript.Status.Processing"] = "Processing",
            ["Transcript.Status.Completed"] = "Completed",
            ["Transcript.Status.Interim"] = "Recognizing...",
            ["Transcript.Segment.StartTime"] = "Start Time",
            ["Transcript.Segment.EndTime"] = "End Time",
            ["Transcript.Segment.Duration"] = "Duration",
            ["Transcript.Speaker.Label"] = "Speaker",
            ["Transcript.Text.Placeholder"] = "Transcript text will appear here...",
            ["Transcript.Button.Back"] = "Back",
            ["Transcript.Button.Extract"] = "Extract Insights",
            ["Transcript.Label.Transcript"] = "Transcript",
            ["Transcript.Label.From"] = "from"
        },
        ["es-ES"] = new Dictionary<string, string>
        {
            ["Transcript.Speaker.Unknown"] = "Orador Desconocido",
            ["Transcript.Time.Format"] = "{0:hh\\:mm\\:ss}",
            ["Transcript.Confidence.High"] = "Alta",
            ["Transcript.Confidence.Medium"] = "Media",
            ["Transcript.Confidence.Low"] = "Baja",
            ["Transcript.Status.Recording"] = "Grabando",
            ["Transcript.Status.Processing"] = "Procesando",
            ["Transcript.Status.Completed"] = "Completado",
            ["Transcript.Status.Interim"] = "Reconociendo...",
            ["Transcript.Segment.StartTime"] = "Hora de Inicio",
            ["Transcript.Segment.EndTime"] = "Hora de Fin",
            ["Transcript.Segment.Duration"] = "Duración",
            ["Transcript.Speaker.Label"] = "Orador",
            ["Transcript.Text.Placeholder"] = "El texto aparecerá aquí...",
            ["Transcript.Button.Back"] = "Atrás",
            ["Transcript.Button.Extract"] = "Extraer Insights",
            ["Transcript.Label.Transcript"] = "Transcripción",
            ["Transcript.Label.From"] = "de"
        },
        ["fr-FR"] = new Dictionary<string, string>
        {
            ["Transcript.Speaker.Unknown"] = "Interlocuteur Inconnu",
            ["Transcript.Time.Format"] = "{0:hh\\:mm\\:ss}",
            ["Transcript.Confidence.High"] = "Élevée",
            ["Transcript.Confidence.Medium"] = "Moyenne",
            ["Transcript.Confidence.Low"] = "Faible",
            ["Transcript.Status.Recording"] = "Enregistrement",
            ["Transcript.Status.Processing"] = "Traitement",
            ["Transcript.Status.Completed"] = "Terminé",
            ["Transcript.Status.Interim"] = "Reconnaissance...",
            ["Transcript.Segment.StartTime"] = "Heure de Début",
            ["Transcript.Segment.EndTime"] = "Heure de Fin",
            ["Transcript.Segment.Duration"] = "Durée",
            ["Transcript.Speaker.Label"] = "Interlocuteur",
            ["Transcript.Text.Placeholder"] = "Le texte apparaîtra ici...",
            ["Transcript.Button.Back"] = "Retour",
            ["Transcript.Button.Extract"] = "Extraire Insights",
            ["Transcript.Label.Transcript"] = "Transcription",
            ["Transcript.Label.From"] = "du"
        }
    };

    public string CurrentCulture => _currentCulture;

    public void SetCulture(string culture)
    {
        _currentCulture = culture;
    }

    public string GetString(string key, string? culture = null)
    {
        var targetCulture = culture ?? _currentCulture;
        
        if (LocalizedStrings.TryGetValue(targetCulture, out var dict) && dict.TryGetValue(key, out var value))
        {
            return value;
        }
        
        // Fallback to en-US
        if (LocalizedStrings.TryGetValue("en-US", out var fallbackDict) && fallbackDict.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }
        
        return key; // Return key as last resort
    }

    public string GetString(string key, Dictionary<string, object> parameters, string? culture = null)
    {
        var template = GetString(key, culture);
        
        foreach (var param in parameters)
        {
            template = template.Replace($"{{{param.Key}}}", param.Value.ToString() ?? "");
        }
        
        return template;
    }
}
