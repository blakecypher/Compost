using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Compost Snippets",
    Author = "Compost Team",
    Website = "https://github.com/compost",
    Version = "0.1.0",
    Description = "Searchable code repository and snippet management.",
    Category = "Content Management",
    Dependencies = ["OrchardCore.Contents", "Compost.Contexts"]
)]
