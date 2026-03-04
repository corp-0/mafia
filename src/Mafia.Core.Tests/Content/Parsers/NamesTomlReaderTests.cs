using FluentAssertions;
using Mafia.Core.Content.Parsers;
using Xunit;

namespace Mafia.Core.Tests.Content.Parsers;

public class NamesTomlReaderTests
{
    [Fact]
    public void Deserialize_ParsesNamesAndNicknames()
    {
        const string toml = """
            surnames = ["Corleone", "Soprano"]

            [male]
            names = ["Antonio", "Giuseppe", "Marco"]

            [male.nicknames]
            Antonio = ["Tony", "Tonny"]
            Giuseppe = ["Joe", "Peppe"]

            [female]
            names = ["Maria", "Francesca"]

            [female.nicknames]
            Francesca = ["Frankie"]
            """;

        var dto = NamesTomlReader.Deserialize(toml);

        dto.Male.Names.Should().Equal("Antonio", "Giuseppe", "Marco");
        dto.Male.Nicknames.Should().ContainKey("Antonio");
        dto.Male.Nicknames["Antonio"].Should().Equal("Tony", "Tonny");
        dto.Male.Nicknames["Giuseppe"].Should().Equal("Joe", "Peppe");

        dto.Female.Names.Should().Equal("Maria", "Francesca");
        dto.Female.Nicknames["Francesca"].Should().Equal("Frankie");

        dto.Surnames.Should().Equal("Corleone", "Soprano");
    }

    [Fact]
    public void Deserialize_EmptyNicknames_DefaultsToEmptyDictionary()
    {
        const string toml = """
            surnames = ["Corleone"]

            [male]
            names = ["Marco"]

            [female]
            names = ["Maria"]
            """;

        var dto = NamesTomlReader.Deserialize(toml);

        dto.Male.Names.Should().Equal("Marco");
        dto.Male.Nicknames.Should().BeEmpty();
        dto.Female.Names.Should().Equal("Maria");
        dto.Female.Nicknames.Should().BeEmpty();
        dto.Surnames.Should().Equal("Corleone");
    }
}
