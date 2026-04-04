using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Compost Structure",
    Author = "Compost Team",
    Website = "https://github.com/blakecypher/Compost",
    Version = "0.1.0",
    Description = "Hierarchical team and department organization with kanban board association.",
    Category = "Content Management",
    Dependencies = ["OrchardCore.Contents", "Compost.Contexts", "Compost.Kanban"]
)]
