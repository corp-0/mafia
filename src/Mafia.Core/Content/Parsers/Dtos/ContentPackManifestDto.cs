namespace Mafia.Core.Content.Parsers.Dtos;

public sealed class ContentPackManifestDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string? Author { get; set; }
    public string? Description { get; set; }
    public int LoadOrder { get; set; }
}
