using fennecs;
using FluentAssertions;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects;
using Mafia.Core.Extensions;
using Xunit;

namespace Mafia.Core.Tests.Context;

public class EntityScopeTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    private EntityScope CreateScope() => new(_world);

    private Entity SpawnEntity() => _world.Spawn();

    #region Anchor Management

    [Fact]
    public void ResolveAnchor_UnknownName_ReturnsNull()
    {
        var scope = CreateScope();

        scope.ResolveAnchor("nobody").Should().BeNull();
    }

    [Fact]
    public void WithAnchor_RegistersEntity_ResolvableByName()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();

        scope.WithAnchor("vito", entity);

        scope.ResolveAnchor("vito").Should().Be(entity);
    }

    [Fact]
    public void WithAnchor_OverwritesSameName()
    {
        var scope = CreateScope();
        var first = SpawnEntity();
        var second = SpawnEntity();

        scope.WithAnchor("don", first);
        scope.WithAnchor("don", second);

        scope.ResolveAnchor("don").Should().Be(second);
    }

    [Fact]
    public void WithAnchor_ReturnsScopeForChaining()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();

        var result = scope.WithAnchor("test", entity);

        result.Should().BeSameAs(scope);
    }

    #endregion

    #region Navigate  Empty and Anchor-Only Paths

    [Fact]
    public void Navigate_EmptyPath_ReturnsNull()
    {
        var scope = CreateScope();

        scope.Navigate("").Should().BeNull();
    }

    [Fact]
    public void Navigate_AnchorOnly_ReturnsAnchoredEntity()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("player", entity);

        var result = scope.Navigate("player");

        result.Should().Be(entity);
    }

    [Fact]
    public void Navigate_UnknownAnchor_ReturnsNull()
    {
        var scope = CreateScope();

        scope.Navigate("ghost").Should().BeNull();
    }

    #endregion

    #region Navigate  Single Relation Hop

    [Fact]
    public void Navigate_SingleRelation_FollowsLink()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        // Vito is father of Michael
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito);

        // vito.FatherOf → Michael (the child)
        var result = scope.Navigate("vito.FatherOf");

        result.Should().Be(michael);
    }

    [Fact]
    public void Navigate_SingleRelation_MissingRelation_ReturnsNull()
    {
        var scope = CreateScope();
        var loner = SpawnEntity();
        scope.WithAnchor("loner", loner);

        scope.Navigate("loner.FatherOf").Should().BeNull();
    }

    #endregion

    #region Navigate  Multi-Segment Paths

    [Fact]
    public void Navigate_TwoHops_ResolvesChain()
    {
        var scope = CreateScope();
        var antonio = SpawnEntity();
        var vito = SpawnEntity();
        var michael = SpawnEntity();

        // Antonio is father of Vito, Vito is father of Michael
        antonio.Add(new FatherOf(vito), vito);
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("antonio", antonio);

        // antonio.FatherOf.FatherOf → Vito → Michael (the grandson)
        var result = scope.Navigate("antonio.FatherOf.FatherOf");

        result.Should().Be(michael);
    }

    [Fact]
    public void Navigate_ThreeHops_ResolvesChain()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        var carmela = SpawnEntity();
        var carlo = SpawnEntity();

        // Vito is father of Michael, Michael is husband of Carmela, Carmela is sister of Carlo
        vito.Add(new FatherOf(michael), michael);
        michael.Add(new HusbandOf(carmela), carmela);
        carmela.Add(new SisterOf(carlo), carlo);
        scope.WithAnchor("vito", vito);

        // vito.FatherOf.HusbandOf.SisterOf → Michael → Carmela → Carlo
        var result = scope.Navigate("vito.FatherOf.HusbandOf.SisterOf");

        result.Should().Be(carlo);
    }

    [Fact]
    public void Navigate_ChainBreaksInMiddle_ReturnsNull()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();

        // Vito is father of Michael, but Michael has no MotherOf relation
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito);

        scope.Navigate("vito.FatherOf.MotherOf").Should().BeNull();
    }

    [Fact]
    public void Navigate_UnknownRelationName_ReturnsNull()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("entity", entity);

        scope.Navigate("entity.NonExistentRelation").Should().BeNull();
    }

    #endregion

    #region Navigate  Different Relation Types

    [Fact]
    public void Navigate_MotherOf_Resolves()
    {
        var scope = CreateScope();
        var carmela = SpawnEntity();
        var michael = SpawnEntity();
        // Carmela is mother of Michael
        carmela.Add(new MotherOf(michael), michael);
        scope.WithAnchor("carmela", carmela);

        scope.Navigate("carmela.MotherOf").Should().Be(michael);
    }

    [Fact]
    public void Navigate_SiblingRelations_Resolve()
    {
        var scope = CreateScope();
        var sonny = SpawnEntity();
        var connie = SpawnEntity();
        var michael = SpawnEntity();

        // Sonny is brother of Michael, Connie is sister of Michael
        sonny.Add(new BrotherOf(michael), michael);
        connie.Add(new SisterOf(michael), michael);
        scope.WithAnchor("sonny", sonny);
        scope.WithAnchor("connie", connie);

        scope.Navigate("sonny.BrotherOf").Should().Be(michael);
        scope.Navigate("connie.SisterOf").Should().Be(michael);
    }

    [Fact]
    public void Navigate_SpouseRelations_Resolve()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var carmela = SpawnEntity();

        // Vito is husband of Carmela, Carmela is wife of Vito
        vito.Add(new HusbandOf(carmela), carmela);
        carmela.Add(new WifeOf(vito), vito);
        scope.WithAnchor("vito", vito);
        scope.WithAnchor("carmela", carmela);

        scope.Navigate("vito.HusbandOf").Should().Be(carmela);
        scope.Navigate("carmela.WifeOf").Should().Be(vito);
    }

    [Fact]
    public void Navigate_SubordinateOf_Resolves()
    {
        var scope = CreateScope();
        var tommy = SpawnEntity();
        var don = SpawnEntity();

        // Tommy is subordinate of the Don
        tommy.Add(new SubordinateOf(don), don);
        scope.WithAnchor("tommy", tommy);

        scope.Navigate("tommy.SubordinateOf").Should().Be(don);
    }

    #endregion

    #region Navigate  Multiple Anchors

    [Fact]
    public void Navigate_MultipleAnchors_IndependentResolution()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var carlo = SpawnEntity();
        var michael = SpawnEntity();
        var sonny = SpawnEntity();

        // Vito is father of Michael, Carlo is father of Sonny
        vito.Add(new FatherOf(michael), michael);
        carlo.Add(new FatherOf(sonny), sonny);
        scope.WithAnchor("vito", vito);
        scope.WithAnchor("carlo", carlo);

        scope.Navigate("vito.FatherOf").Should().Be(michael);
        scope.Navigate("carlo.FatherOf").Should().Be(sonny);
    }

    #endregion

    #region EntityExtensions  GetComponent

    [Fact]
    public void GetComponent_ReturnsValue()
    {
        var entity = SpawnEntity();
        entity.Add(new Muscle(75));

        var muscle = entity.GetComponent<Muscle>();

        muscle.Should().NotBeNull();
        muscle.Value.Amount.Should().Be(75);
    }

    [Fact]
    public void GetComponent_MissingComponent_ReturnsNull()
    {
        var entity = SpawnEntity();

        entity.GetComponent<Muscle>().Should().BeNull();
    }

    #endregion

    #region EntityExtensions  TryAddComponent

    [Fact]
    public void TryAddComponent_AddsNewComponent()
    {
        var entity = SpawnEntity();

        var success = entity.TryAddComponent(new Rank(RankId.Soldier));

        success.Should().BeTrue();
        entity.Has<Rank>().Should().BeTrue();
        entity.Ref<Rank>().Id.Should().Be(RankId.Soldier);
    }

    [Fact]
    public void TryAddComponent_AlreadyExists_ReturnsFalse()
    {
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Soldier));

        entity.TryAddComponent(new Rank(RankId.Boss)).Should().BeFalse();
    }

    #endregion

    #region EntityExtensions  TryRemoveComponent

    [Fact]
    public void TryRemoveComponent_RemovesExistingComponent()
    {
        var entity = SpawnEntity();
        entity.Add<Arrested>();

        var success = entity.TryRemoveComponent<Arrested>();

        success.Should().BeTrue();
        entity.Has<Arrested>().Should().BeFalse();
    }

    [Fact]
    public void TryRemoveComponent_MissingComponent_ReturnsFalse()
    {
        var entity = SpawnEntity();

        entity.TryRemoveComponent<Arrested>().Should().BeFalse();
    }

    #endregion

    #region EntityExtensions  ModifyComponent

    [Fact]
    public void ModifyComponent_TransformsExistingComponent()
    {
        var entity = SpawnEntity();
        entity.Add(new Muscle(50));

        var success = entity.ModifyComponent<Muscle>(m => m with { Amount = m.Amount + 30 });

        success.Should().BeTrue();
        entity.Ref<Muscle>().Amount.Should().Be(80);
    }

    [Fact]
    public void ModifyComponent_MissingComponent_ReturnsFalse()
    {
        var entity = SpawnEntity();

        entity.ModifyComponent<Muscle>(m => m with { Amount = 100 }).Should().BeFalse();
    }

    #endregion

    #region EntityExtensions  TryAddRelation / TryRemoveRelation

    [Fact]
    public void TryAddRelation_AddsNewRelation()
    {
        var a = SpawnEntity();
        var b = SpawnEntity();

        var success = a.TryAddRelation<FatherOf>(b);

        success.Should().BeTrue();
        a.Has<FatherOf>(b).Should().BeTrue();
    }

    [Fact]
    public void TryAddRelation_AlreadyExists_ReturnsFalse()
    {
        var a = SpawnEntity();
        var b = SpawnEntity();
        a.Add(new FatherOf(b), b);

        a.TryAddRelation<FatherOf>(b).Should().BeFalse();
    }

    [Fact]
    public void TryRemoveRelation_RemovesExistingRelation()
    {
        var a = SpawnEntity();
        var b = SpawnEntity();
        a.Add(new FatherOf(b), b);

        var success = a.TryRemoveRelation<FatherOf>(b);

        success.Should().BeTrue();
        a.Has<FatherOf>(b).Should().BeFalse();
    }

    [Fact]
    public void TryRemoveRelation_MissingRelation_ReturnsFalse()
    {
        var a = SpawnEntity();
        var b = SpawnEntity();

        a.TryRemoveRelation<FatherOf>(b).Should().BeFalse();
    }

    #endregion

    #region Navigate Edge Cases

    [Fact]
    public void Navigate_TrailingDot_ReturnsAnchorEntity()
    {
        // Path "player." → remaining path after anchor is empty,
        // so the while loop is skipped and the anchor entity is returned.
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("player", entity);

        scope.Navigate("player.").Should().Be(entity);
    }

    [Fact]
    public void Navigate_DoubleDot_HasEmptyMiddleSegment()
    {
        // "player..FatherOf" has an empty segment between dots
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("player", entity);

        scope.Navigate("player..FatherOf").Should().BeNull();
    }

    [Fact]
    public void Navigate_DotOnly_ReturnsNull()
    {
        var scope = CreateScope();

        // "." → anchor = "", which won't match
        scope.Navigate(".").Should().BeNull();
    }

    [Fact]
    public void Navigate_ScopeIsReusableWithDifferentPaths()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        var connie = SpawnEntity();

        // Vito is father of Michael, Vito is father of Connie (using MotherOf for variety)
        vito.Add(new FatherOf(michael), michael);
        vito.Add(new MotherOf(connie), connie);
        scope.WithAnchor("vito", vito);

        scope.Navigate("vito.FatherOf").Should().Be(michael);
        scope.Navigate("vito.MotherOf").Should().Be(connie);
        scope.Navigate("vito").Should().Be(vito);
    }

    #endregion

    #region DisableCharacter / EnableCharacter Effects

    [Fact]
    public void DisableCharacter_AddsReasonAndDisabledTag()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new DisableCharacter<Arrested>("target").Apply(scope);

        entity.Has<Arrested>().Should().BeTrue();
        entity.Has<Disabled>().Should().BeTrue();
        entity.Ref<Disabled>().Count.Should().Be(1);
    }

    [Fact]
    public void DisableCharacter_MultipleReasons_IncrementsCount()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new DisableCharacter<Arrested>("target").Apply(scope);
        new DisableCharacter<Killed>("target").Apply(scope);

        entity.Has<Arrested>().Should().BeTrue();
        entity.Has<Killed>().Should().BeTrue();
        entity.Ref<Disabled>().Count.Should().Be(2);
    }

    [Fact]
    public void DisableCharacter_DuplicateReason_IsIgnored()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new DisableCharacter<Arrested>("target").Apply(scope);
        new DisableCharacter<Arrested>("target").Apply(scope);

        entity.Ref<Disabled>().Count.Should().Be(1);
    }

    [Fact]
    public void EnableCharacter_RemovesReasonAndDisabledTag()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new DisableCharacter<Arrested>("target").Apply(scope);
        new EnableCharacter<Arrested>("target").Apply(scope);

        entity.Has<Arrested>().Should().BeFalse();
        entity.Has<Disabled>().Should().BeFalse();
    }

    [Fact]
    public void EnableCharacter_MultipleReasons_DecrementsCount()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new DisableCharacter<Arrested>("target").Apply(scope);
        new DisableCharacter<Killed>("target").Apply(scope);
        new EnableCharacter<Arrested>("target").Apply(scope);

        entity.Has<Arrested>().Should().BeFalse();
        entity.Has<Killed>().Should().BeTrue();
        entity.Has<Disabled>().Should().BeTrue();
        entity.Ref<Disabled>().Count.Should().Be(1);
    }

    [Fact]
    public void EnableCharacter_AllReasonsRemoved_RemovesDisabled()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new DisableCharacter<Arrested>("target").Apply(scope);
        new DisableCharacter<Killed>("target").Apply(scope);
        new EnableCharacter<Arrested>("target").Apply(scope);
        new EnableCharacter<Killed>("target").Apply(scope);

        entity.Has<Disabled>().Should().BeFalse();
    }

    [Fact]
    public void EnableCharacter_ReasonNotPresent_IsIgnored()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new EnableCharacter<Arrested>("target").Apply(scope);

        entity.Has<Disabled>().Should().BeFalse();
    }

    #endregion
}
