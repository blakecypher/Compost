using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Compost Analytics",
    Author = "Compost Team",
    Website = "https://github.com/blakecypher/Compost",
    Version = "1.0.0",
    Description = "Analytics dashboard for velocity tracking and pattern usage statistics",
    Category = "Analytics",
    Dependencies = ["OrchardCore.ContentManagement", "OrchardCore.Navigation", "Compost.Kanban"]
)]
