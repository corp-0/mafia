namespace Mafia.Core.Content.Parsers.Dtos;

public sealed class NamesDto
{
    public GenderedNamesDto Male { get; set; } = new();
    public GenderedNamesDto Female { get; set; } = new();
    public string[] Surnames { get; set; } = [];
}

public sealed class GenderedNamesDto
{
    public string[] Names { get; set; } = [];
    public Dictionary<string, string[]> Nicknames { get; set; } = new();
}
