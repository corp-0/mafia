using FluentAssertions;
using Mafia.Core.Content.Registries;
using Xunit;

namespace Mafia.Core.Tests.Content.Registries;

public class ContentMetadataRegistryTests
{
    private readonly ContentMetadataRegistry _registry = new();

    [Fact]
    public void Register_And_Get_ReturnsMetadata()
    {
        var metadata = new ContentMetadata("TITLE", "DESC");
        _registry.Register("chainsmoker", metadata);

        _registry.Get("chainsmoker").Should().BeSameAs(metadata);
    }

    [Fact]
    public void Get_UnknownKey_ReturnsNull()
    {
        _registry.Get("nonexistent").Should().BeNull();
    }

    [Fact]
    public void Register_SameKeyTwice_LastWriteWins()
    {
        var original = new ContentMetadata("ORIG_TITLE", "ORIG_DESC");
        var replacement = new ContentMetadata("MOD_TITLE", "MOD_DESC");

        _registry.Register("enforcer", original);
        _registry.Register("enforcer", replacement);

        _registry.Get("enforcer").Should().BeSameAs(replacement);
    }
}
