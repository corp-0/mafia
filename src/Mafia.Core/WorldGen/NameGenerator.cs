using Mafia.Core.Content.Registries;
using Mafia.Core.Ecs.Components.Identity;

namespace Mafia.Core.WorldGen;

public class NameGenerator(SeededRandom rng, INameRepository names)
{
    private readonly HashSet<string> _usedFullNames = new(StringComparer.Ordinal);
    private readonly HashSet<string> _usedSurnames = new(StringComparer.Ordinal);

    public string PickFirstName(Sex sex) =>
        sex == Sex.Male ? rng.Pick(names.MaleNames) : rng.Pick(names.FemaleNames);

    public string PickSurname() => rng.Pick(names.Surnames);

    public string PickUniqueSurname(IReadOnlySet<string>? excluded = null)
    {
        for (var i = 0; i < 100; i++)
        {
            var surname = rng.Pick(names.Surnames);
            if (excluded != null && excluded.Contains(surname)) continue;
            if (_usedSurnames.Add(surname))
                return surname;
        }

        // Fallback: generate a unique surname by appending a number
        var baseName = rng.Pick(names.Surnames);
        var counter = 2;
        while (!_usedSurnames.Add($"{baseName}{counter}"))
            counter++;
        return $"{baseName}{counter}";
    }

    public string GenerateUniqueName(Sex sex, string surname)
    {
        for (var i = 0; i < 100; i++)
        {
            var firstName = PickFirstName(sex);
            var fullName = $"{firstName} {surname}";
            if (_usedFullNames.Add(fullName))
                return fullName;
        }

        // Fallback: append a number
        var baseName = PickFirstName(sex);
        var counter = 2;
        string name;
        do
        {
            name = $"{baseName} {surname} {counter}";
            counter++;
        } while (!_usedFullNames.Add(name));

        return name;
    }
}
