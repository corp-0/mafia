using FluentAssertions;
using Mafia.Core.Content.Parsers;
using Xunit;

namespace Mafia.Core.Tests.Content.Parsers;

public class MetadataTomlReaderTests
{
    [Fact]
    public void Read_MultipleEntries_ParsesAll()
    {
        const string toml = """
            [chain_smoker]
            title_key = "TAG_CHAIN_SMOKER_TITLE"
            description_key = "TAG_CHAIN_SMOKER_DESC"

            [enforcer]
            title_key = "TAG_ENFORCER_TITLE"
            description_key = "TAG_ENFORCER_DESC"
            """;

        var results = MetadataTomlReader.Read(toml);

        results.Should().HaveCount(2);
    }

    [Fact]
    public void Read_CorrectTitleAndDescription()
    {
        const string toml = """
            [chain_smoker]
            title_key = "TAG_CHAIN_SMOKER_TITLE"
            description_key = "TAG_CHAIN_SMOKER_DESC"
            """;

        var results = MetadataTomlReader.Read(toml);

        var entry = results.Should().ContainSingle().Subject;
        entry.NormalizedKey.Should().Be("chainsmoker");
        entry.Metadata.TitleKey.Should().Be("TAG_CHAIN_SMOKER_TITLE");
        entry.Metadata.DescriptionKey.Should().Be("TAG_CHAIN_SMOKER_DESC");
    }

    [Fact]
    public void Read_SingleWordKey_NormalizesCorrectly()
    {
        const string toml = """
            [enforcer]
            title_key = "TAG_ENFORCER_TITLE"
            description_key = "TAG_ENFORCER_DESC"
            """;

        var results = MetadataTomlReader.Read(toml);

        var entry = results.Should().ContainSingle().Subject;
        entry.NormalizedKey.Should().Be("enforcer");
        entry.Metadata.TitleKey.Should().Be("TAG_ENFORCER_TITLE");
        entry.Metadata.DescriptionKey.Should().Be("TAG_ENFORCER_DESC");
    }

    [Fact]
    public void Read_MultiWordSnakeCase_NormalizesToLowerPascal()
    {
        const string toml = """
            [member_of_household]
            title_key = "REL_MEMBER_OF_HOUSEHOLD_TITLE"
            description_key = "REL_MEMBER_OF_HOUSEHOLD_DESC"
            """;

        var results = MetadataTomlReader.Read(toml);

        var entry = results.Should().ContainSingle().Subject;
        entry.NormalizedKey.Should().Be("memberofhousehold");
    }

    [Fact]
    public void Deserialize_ReturnsDtosWithCorrectFields()
    {
        const string toml = """
            [heavy_drinker]
            title_key = "TAG_HEAVY_DRINKER_TITLE"
            description_key = "TAG_HEAVY_DRINKER_DESC"
            """;

        var dtos = MetadataTomlReader.Deserialize(toml);

        dtos.Should().ContainKey("heavy_drinker");
        dtos["heavy_drinker"].TitleKey.Should().Be("TAG_HEAVY_DRINKER_TITLE");
        dtos["heavy_drinker"].DescriptionKey.Should().Be("TAG_HEAVY_DRINKER_DESC");
    }
}
