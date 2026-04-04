using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Compost Mind Map",
    Author = "Compost Team",
    Website = "https://github.com/blakecypher/Compost",
    Version = "0.1.0",
    Description = "Interactive mind map visualization with Cytoscape.js for organizing ideas and requirements.",
    Category = "Content Management",
    Dependencies = ["OrchardCore.Contents", "Compost.Contexts"]
)]
