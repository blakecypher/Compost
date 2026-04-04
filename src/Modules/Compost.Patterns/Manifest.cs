using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Compost Patterns",
    Author = "Compost Team",
    Website = "https://github.com/blakecypher/Compost",
    Version = "0.1.0",
    Description = "Architectural pattern library and AI-driven suggestions.",
    Category = "Content Management",
    Dependencies = ["OrchardCore.Contents", "Compost.Contexts"]
)]
