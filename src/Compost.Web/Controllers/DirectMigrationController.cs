using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;

namespace Compost.Web.Controllers
{
    public class DirectMigrationController(
        IContentDefinitionManager contentDefinitionManager,
        ILogger<DirectMigrationController> logger)
        : Controller
    {
        [HttpGet]
        [Route("/run-transcription-migration")]
        public async Task<IActionResult> RunTranscriptionMigration()
        {
            try
            {
                logger.LogInformation("Starting direct transcription migration...");

                // First, create the MeetingPart
                await contentDefinitionManager.AlterPartDefinitionAsync("MeetingPart", part => part
                    .WithField("MeetingId", field => field.OfType("TextField").WithDisplayName("Meeting ID"))
                    .WithField("WorkContextId", field => field.OfType("TextField").WithDisplayName("Work Project ID"))
                    .WithField("Status", field => field.OfType("TextField").WithDisplayName("Status"))
                    .WithField("AudioFilePath", field => field.OfType("TextField").WithDisplayName("Audio File Path"))
                    .WithField("TranscriptText", field => field.OfType("TextField").WithDisplayName("Transcript Text"))
                    .WithField("StartedAt", field => field.OfType("DateTimeField").WithDisplayName("Started At"))
                    .WithField("EndedAt", field => field.OfType("DateTimeField").WithDisplayName("Ended At"))
                    .WithField("DurationSeconds", field => field.OfType("NumericField").WithDisplayName("Duration (Seconds)"))
                    .WithField("TranscriptionCompletedAt", field => field.OfType("DateTimeField").WithDisplayName("Transcription Completed At"))
                    .WithField("IsProcessed", field => field.OfType("BooleanField").WithDisplayName("Is Processed"))
                    .WithField("Notes", field => field.OfType("TextField").WithDisplayName("Notes"))
                    .WithField("Summary", field => field.OfType("TextField").WithDisplayName("Summary"))
                    .WithField("AutoExtractMindMapNodes", field => field.OfType("BooleanField").WithDisplayName("Auto Extract Mind Map Nodes"))
                    .WithField("AutoExtractActionItems", field => field.OfType("BooleanField").WithDisplayName("Auto Extract Action Items")));

                logger.LogInformation("MeetingPart created successfully");

                // Then create the Meeting content type
                await contentDefinitionManager.AlterTypeDefinitionAsync("Meeting", type => type
                    .WithPart("MeetingPart", part => part
                        .WithPosition("1")
                        .WithDisplayName("Meeting Details"))
                    .WithPart("TitlePart", part => part
                        .WithPosition("0")
                        .WithDisplayName("Title"))
                    .WithPart("AutoroutePart", part => part
                        .WithPosition("2")
                        .WithDisplayName("URL"))
                    .WithPart("CommonPart", part => part
                        .WithPosition("3")
                        .WithDisplayName("Common")));

                logger.LogInformation("Meeting content type created successfully");

                return Content("✅ Migration completed successfully! MeetingPart and Meeting content type have been created. Try creating a new transcription now - it should persist to the database.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running direct migration");
                return Content($"❌ Migration failed: {ex.Message}");
            }
        }
    }
}
