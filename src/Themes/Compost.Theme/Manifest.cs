using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Compost Theme",
    Author = "Compost Team",
    Website = "https://github.com/compost",
    Version = "1.0.0",
    Description = "Modern responsive theme for Compost application with dark mode support and mobile-first design.",
    Category = "Themes",
    Dependencies = ["OrchardCore.Theme", "OrchardCore.Navigation", "OrchardCore.Contents"]
)]
