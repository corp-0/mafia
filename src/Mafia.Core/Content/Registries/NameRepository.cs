using Mafia.Core.Content.Parsers.Dtos;

namespace Mafia.Core.Content.Registries;

public sealed class NameRepository : INameRepository
{
    private readonly List<string> _maleNames = [];
    private readonly List<string> _femaleNames = [];
    private readonly List<string> _surnames = [];
    private readonly Dictionary<string, string[]> _nicknames = new(StringComparer.Ordinal);

    public IReadOnlyList<string> MaleNames => _maleNames;
    public IReadOnlyList<string> FemaleNames => _femaleNames;
    public IReadOnlyList<string> Surnames => _surnames;

    public IReadOnlyList<string> GetNicknames(string name) =>
        _nicknames.TryGetValue(name, out var nicks) ? nicks : [];

    public void Register(NamesDto dto)
    {
        RegisterGendered(dto.Male, _maleNames);
        RegisterGendered(dto.Female, _femaleNames);
        _surnames.AddRange(dto.Surnames);
    }

    private void RegisterGendered(GenderedNamesDto gendered, List<string> target)
    {
        target.AddRange(gendered.Names);

        foreach (var (name, nicknames) in gendered.Nicknames)
            _nicknames[name] = nicknames;
    }
}
