using System;
using Compost.Core.Interfaces;
using Compost.Transcription.Services;
using Compost.Transcription.Hubs;
using Compost.Transcription.Drivers;
using Compost.Transcription.Handlers;
using Compost.Transcription.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.Modules;

namespace Compost.Transcription;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ITranscriptionService, TranscriptionService>();
        services.AddHttpClient<IAIIntegrationService, Compost.Core.Services.AIIntegrationService>();
        services.AddSignalR();
        services.AddScoped<IContentPartDisplayDriver, MeetingPartDisplayDriver>();
        services.AddScoped<IContentPartHandler, MeetingPartHandler>();
        
        // Register the MeetingPart
        services.AddContentPart<MeetingPart>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: nameof(Transcription),
            areaName: "Compost.Transcription",
            pattern: "Transcription/{action=Index}/{id?}",
            defaults: new { controller = nameof(Transcription), action = nameof(Index) }
        );
        
        routes.MapHub<TranscriptionHub>("/TranscriptionHub");
    }
}
