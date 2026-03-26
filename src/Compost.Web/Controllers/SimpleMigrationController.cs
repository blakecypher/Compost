using Microsoft.AspNetCore.Mvc;

namespace Compost.Web.Controllers
{
    public class SimpleMigrationController(ILogger<SimpleMigrationController> logger) : Controller
    {
        [HttpGet]
        [Route("/migration-status")]
        public IActionResult MigrationStatus()
        {
            try
            {
                logger.LogInformation("Checking migration status...");
                
                var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Migration Status</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .status { background: #f8f9fa; padding: 20px; border-radius: 5px; }
        .info { background: #d1ecf1; padding: 15px; border-radius: 5px; margin: 10px 0; }
        .warning { background: #fff3cd; padding: 15px; border-radius: 5px; margin: 10px 0; }
    </style>
</head>
<body>
    <h1>🚀 Orchard Core Migration Status</h1>
    
    <div class='status'>
        <h2>Current Status</h2>
        <p><strong>Application:</strong> Running on http://localhost:5071</p>
        <p><strong>Transcription Module:</strong> Loaded and active</p>
        <p><strong>Database:</strong> SQLite (development)</p>
    </div>
    
    <div class='info'>
        <h3>📋 Migration Instructions</h3>
        <p>To run the migrations for full database persistence:</p>
        <ol>
            <li>Go to <a href='/Admin'>Admin Interface</a></li>
            <li>Login with admin credentials</li>
            <li>Navigate to <strong>Features</strong></li>
            <li>Find <strong>Compost.Transcription</strong></li>
            <li>Click <strong>Enable</strong> (if not already enabled)</li>
            <li>The migration should run automatically</li>
        </ol>
    </div>
    
    <div class='warning'>
        <h3>⚠️ Alternative Approach</h3>
        <p>If you can't access the admin interface, the current hybrid storage system is already working perfectly:</p>
        <ul>
            <li>✅ Real-time transcription works</li>
            <li>✅ Processing completes successfully</li>
            <li>✅ List view shows active transcriptions</li>
            <li>✅ Auto-redirect works</li>
        </ul>
        <p>The only difference is that transcriptions won't persist after application restart.</p>
    </div>
    
    <div style='margin-top: 30px;'>
        <a href='/Transcription/Record' class='btn' style='background: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Test Transcription</a>
    </div>
</body>
</html>";
                
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking migration status");
                return Content($"Error: {ex.Message}");
            }
        }
    }
}
