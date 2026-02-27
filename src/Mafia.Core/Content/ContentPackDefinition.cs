namespace Mafia.Core.Content;

public sealed record ContentPackDefinition(
    string Id,
    string Name,
    string Version,
    string? Author,
    string? Description,
    int LoadOrder,
    string DirectoryPath);
