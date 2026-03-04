using FluentAssertions;
using Mafia.Core.WorldGen;
using Mafia.Core.Ecs.Components.Identity;
using Xunit;

namespace Mafia.Core.Tests.WorldGen;

public class NameGeneratorTests
{
    private static readonly TestNameRepository Names = new();

    [Fact]
    public void GenerateUniqueName_NeverReturnsDuplicates()
    {
        var rng = new SeededRandom(42);
        var gen = new NameGenerator(rng, Names);
        var names = new HashSet<string>();

        for (var i = 0; i < 200; i++)
        {
            var name = gen.GenerateUniqueName(Sex.Male, "Corleone");
            names.Add(name).Should().BeTrue($"Duplicate name generated: {name}");
        }
    }

    [Fact]
    public void PickUniqueSurname_ReturnsDistinctSurnames()
    {
        var rng = new SeededRandom(42);
        var gen = new NameGenerator(rng, Names);
        var surnames = new HashSet<string>();

        for (var i = 0; i < 10; i++)
        {
            var surname = gen.PickUniqueSurname();
            surnames.Add(surname).Should().BeTrue($"Duplicate surname: {surname}");
        }
    }

    [Fact]
    public void PickUniqueSurname_RespectsExclusions()
    {
        var rng = new SeededRandom(42);
        var gen = new NameGenerator(rng, Names);
        var excluded = new HashSet<string> { "Corleone", "Tattaglia" };

        for (var i = 0; i < 10; i++)
        {
            var surname = gen.PickUniqueSurname(excluded);
            surname.Should().NotBe("Corleone");
            surname.Should().NotBe("Tattaglia");
        }
    }

    [Fact]
    public void PickFirstName_ReturnsDifferentNamesForGenders()
    {
        var rng = new SeededRandom(42);
        var gen = new NameGenerator(rng, Names);

        var maleNames = Enumerable.Range(0, 50).Select(_ => gen.PickFirstName(Sex.Male)).ToHashSet();
        var femaleNames = Enumerable.Range(0, 50).Select(_ => gen.PickFirstName(Sex.Female)).ToHashSet();

        // The male and female pools should not fully overlap
        maleNames.Overlaps(femaleNames).Should().BeFalse();
    }
}
