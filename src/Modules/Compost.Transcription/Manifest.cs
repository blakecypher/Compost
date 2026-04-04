using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Compost Transcription",
    Author = "Compost Team",
    Website = "https://github.com/blakecypher/Compost",
    Version = "0.1.0",
    Description = "Meeting recording and real-time transcription using Azure Speech Services.",
    Category = "Content Management",
    Dependencies = ["OrchardCore.Contents", "Compost.Contexts"]
)]
