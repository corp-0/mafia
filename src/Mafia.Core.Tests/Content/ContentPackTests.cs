using FluentAssertions;
using Mafia.Core.Content;
using Mafia.Core.Content.Parsers;
using Xunit;

namespace Mafia.Core.Tests.Content;

public class ContentPackTests
{
    // ═══════════════════════════════════════════════
    //  Manifest Parsing
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_ParsesAllFields()
    {
        const string toml = """
            id = "my_pack"
            name = "My Pack"
            version = "2.1.0"
            author = "TestAuthor"
            description = "A test content pack."
            load_order = 42
            """;

        var pack = ContentPackManifestReader.Read(toml, "/some/path");

        pack.Id.Should().Be("my_pack");
        pack.Name.Should().Be("My Pack");
        pack.Version.Should().Be("2.1.0");
        pack.Author.Should().Be("TestAuthor");
        pack.Description.Should().Be("A test content pack.");
        pack.LoadOrder.Should().Be(42);
        pack.DirectoryPath.Should().Be("/some/path");
    }

    [Fact]
    public void Read_OptionalFieldsDefaultCorrectly()
    {
        const string toml = """
            id = "minimal"
            name = "Minimal Pack"
            """;

        var pack = ContentPackManifestReader.Read(toml, "/dir");

        pack.Version.Should().Be("1.0.0");
        pack.Author.Should().BeNull();
        pack.Description.Should().BeNull();
        pack.LoadOrder.Should().Be(0);
    }

    [Fact]
    public void Read_AttachesDirectoryPath()
    {
        const string toml = """
            id = "test"
            name = "Test"
            """;

        var pack = ContentPackManifestReader.Read(toml, "/content/my_mod");

        pack.DirectoryPath.Should().Be("/content/my_mod");
    }

    // ═══════════════════════════════════════════════
    //  ManifestFileName constant
    // ═══════════════════════════════════════════════

    [Fact]
    public void ManifestFileName_IsContentPackToml()
    {
        ContentPackManifestReader.MANIFEST_FILE_NAME.Should().Be("content_pack.toml");
    }
}
