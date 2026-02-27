using fennecs;
using FluentAssertions;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Events.Conditions;
using Mafia.Core.Events.Effects;
using Xunit;

namespace Mafia.Core.Tests.Events;

public class CustomTagTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    private EntityScope CreateScope() => new(_world);

    private Entity SpawnEntity() => _world.Spawn();

    [Fact]
    public void AddCustomTag_ThenHasCustomTag_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        new AddCustomTag("enforcer", "root").Apply(scope);

        new HasCustomTag("enforcer", "root").Evaluate(scope).Should().BeTrue();
    }

    [Fact]
    public void HasCustomTag_AbsentTag_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        new AddCustomTag("enforcer", "root").Apply(scope);

        new HasCustomTag("hitman", "root").Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void HasCustomTag_NoComponent_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        new HasCustomTag("enforcer", "root").Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void RemoveCustomTag_RemovesTag()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        new AddCustomTag("enforcer", "root").Apply(scope);
        new RemoveCustomTag("enforcer", "root").Apply(scope);

        new HasCustomTag("enforcer", "root").Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void RemoveCustomTag_NoComponent_DoesNotThrow()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        new RemoveCustomTag("enforcer", "root").Apply(scope);
    }

    [Fact]
    public void AddCustomTag_Duplicate_IsIdempotent()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        new AddCustomTag("enforcer", "root").Apply(scope);
        new AddCustomTag("enforcer", "root").Apply(scope);

        new HasCustomTag("enforcer", "root").Evaluate(scope).Should().BeTrue();
        entity.Ref<CustomTags>().Tags.Count.Should().Be(1);
    }

    [Fact]
    public void HasCustomTag_OrdinalComparison_CaseSensitive()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        new AddCustomTag("test", "root").Apply(scope);

        new HasCustomTag("test", "root").Evaluate(scope).Should().BeTrue();
        new HasCustomTag("Test", "root").Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void AddCustomTag_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new AddCustomTag("enforcer", "nobody").Apply(scope);
    }

    [Fact]
    public void HasCustomTag_InvalidPath_ReturnsFalse()
    {
        var scope = CreateScope();

        new HasCustomTag("enforcer", "nobody").Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void RemoveCustomTag_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new RemoveCustomTag("enforcer", "nobody").Apply(scope);
    }

    [Fact]
    public void AddCustomTag_Describe_ReturnsLocalizableWithTagName()
    {
        var scope = CreateScope();
        var desc = new AddCustomTag("enforcer", "root").Describe(scope);

        desc.Key.Should().Be("effect.add_trait");
        desc.Args["trait"].Should().Be("enforcer");
    }

    [Fact]
    public void RemoveCustomTag_Describe_ReturnsLocalizableWithTagName()
    {
        var scope = CreateScope();
        var desc = new RemoveCustomTag("enforcer", "root").Describe(scope);

        desc.Key.Should().Be("effect.remove_trait");
        desc.Args["trait"].Should().Be("enforcer");
    }
}
