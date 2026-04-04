using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Compost Kanban",
    Author = "Compost Team",
    Website = "https://github.com/blakecypher/Compost",
    Version = "0.1.0",
    Description = "Advanced drag-and-drop task management board.",
    Category = "Content Management",
    Dependencies = ["OrchardCore.Contents", "Compost.Contexts"]
)]
