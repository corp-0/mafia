using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects;
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

        Assert.Null(scope.ResolveAnchor("nobody"));
    }

    [Fact]
    public void WithAnchor_RegistersEntity_ResolvableByName()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();

        scope.WithAnchor("vito", entity);

        Assert.Equal(entity, scope.ResolveAnchor("vito"));
    }

    [Fact]
    public void WithAnchor_OverwritesSameName()
    {
        var scope = CreateScope();
        var first = SpawnEntity();
        var second = SpawnEntity();

        scope.WithAnchor("don", first);
        scope.WithAnchor("don", second);

        Assert.Equal(second, scope.ResolveAnchor("don"));
    }

    [Fact]
    public void WithAnchor_ReturnsScopeForChaining()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();

        var result = scope.WithAnchor("test", entity);

        Assert.Same(scope, result);
    }

    #endregion

    #region Navigate – Empty and Anchor-Only Paths

    [Fact]
    public void Navigate_EmptyPath_ReturnsNull()
    {
        var scope = CreateScope();

        Assert.Null(scope.Navigate(""));
    }

    [Fact]
    public void Navigate_AnchorOnly_ReturnsAnchoredEntity()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("player", entity);

        var result = scope.Navigate("player");

        Assert.Equal(entity, result);
    }

    [Fact]
    public void Navigate_UnknownAnchor_ReturnsNull()
    {
        var scope = CreateScope();

        Assert.Null(scope.Navigate("ghost"));
    }

    #endregion

    #region Navigate – Single Relation Hop

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

        Assert.Equal(michael, result);
    }

    [Fact]
    public void Navigate_SingleRelation_MissingRelation_ReturnsNull()
    {
        var scope = CreateScope();
        var loner = SpawnEntity();
        scope.WithAnchor("loner", loner);

        Assert.Null(scope.Navigate("loner.FatherOf"));
    }

    #endregion

    #region Navigate – Multi-Segment Paths

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

        Assert.Equal(michael, result);
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

        Assert.Equal(carlo, result);
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

        Assert.Null(scope.Navigate("vito.FatherOf.MotherOf"));
    }

    [Fact]
    public void Navigate_UnknownRelationName_ReturnsNull()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("entity", entity);

        Assert.Null(scope.Navigate("entity.NonExistentRelation"));
    }

    #endregion

    #region Navigate – Different Relation Types

    [Fact]
    public void Navigate_MotherOf_Resolves()
    {
        var scope = CreateScope();
        var carmela = SpawnEntity();
        var michael = SpawnEntity();
        // Carmela is mother of Michael
        carmela.Add(new MotherOf(michael), michael);
        scope.WithAnchor("carmela", carmela);

        Assert.Equal(michael, scope.Navigate("carmela.MotherOf"));
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

        Assert.Equal(michael, scope.Navigate("sonny.BrotherOf"));
        Assert.Equal(michael, scope.Navigate("connie.SisterOf"));
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

        Assert.Equal(carmela, scope.Navigate("vito.HusbandOf"));
        Assert.Equal(vito, scope.Navigate("carmela.WifeOf"));
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

        Assert.Equal(don, scope.Navigate("tommy.SubordinateOf"));
    }

    #endregion

    #region Navigate – Multiple Anchors

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

        Assert.Equal(michael, scope.Navigate("vito.FatherOf"));
        Assert.Equal(sonny, scope.Navigate("carlo.FatherOf"));
    }

    #endregion

    #region GetComponent

    [Fact]
    public void GetComponent_ReturnsComponentValue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(75));
        scope.WithAnchor("player", entity);

        var muscle = scope.GetComponent<Muscle>("player");

        Assert.NotNull(muscle);
        Assert.Equal(75, muscle.Value.Amount);
    }

    [Fact]
    public void GetComponent_MissingComponent_ReturnsNull()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("player", entity);

        Assert.Null(scope.GetComponent<Muscle>("player"));
    }

    [Fact]
    public void GetComponent_InvalidPath_ReturnsNull()
    {
        var scope = CreateScope();

        Assert.Null(scope.GetComponent<Muscle>("nobody"));
    }

    [Fact]
    public void GetComponent_ThroughRelation_ResolvesAndReturns()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        michael.Add(new Muscle(90));
        // Vito is father of Michael
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito);

        // Get Muscle component from vito.FatherOf (→ Michael)
        var muscle = scope.GetComponent<Muscle>("vito.FatherOf");

        Assert.NotNull(muscle);
        Assert.Equal(90, muscle.Value.Amount);
    }

    #endregion

    #region HasTag

    [Fact]
    public void HasTag_EntityHasComponent_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add<Arrested>();
        scope.WithAnchor("player", entity);

        Assert.True(scope.HasTag<Arrested>("player"));
    }

    [Fact]
    public void HasTag_EntityLacksComponent_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("player", entity);

        Assert.False(scope.HasTag<Arrested>("player"));
    }

    [Fact]
    public void HasTag_InvalidPath_ReturnsFalse()
    {
        var scope = CreateScope();

        Assert.False(scope.HasTag<Arrested>("nobody"));
    }

    #endregion

    #region SetComponent

    [Fact]
    public void SetComponent_UpdatesExistingComponent()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(50));
        scope.WithAnchor("player", entity);

        var success = scope.SetComponent("player", new Muscle(80));

        Assert.True(success);
        Assert.Equal(80, entity.Ref<Muscle>().Amount);
    }

    [Fact]
    public void SetComponent_MissingComponent_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("player", entity);

        Assert.False(scope.SetComponent("player", new Muscle(80)));
    }

    [Fact]
    public void SetComponent_InvalidPath_ReturnsFalse()
    {
        var scope = CreateScope();

        Assert.False(scope.SetComponent("nobody", new Muscle(80)));
    }

    #endregion

    #region AddComponent

    [Fact]
    public void AddComponent_AddsNewComponent()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("player", entity);

        var success = scope.AddComponent("player", new Rank(RankId.Soldier));

        Assert.True(success);
        Assert.True(entity.Has<Rank>());
        Assert.Equal(RankId.Soldier, entity.Ref<Rank>().Id);
    }

    [Fact]
    public void AddComponent_AlreadyExists_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Soldier));
        scope.WithAnchor("player", entity);

        Assert.False(scope.AddComponent("player", new Rank(RankId.Boss)));
    }

    [Fact]
    public void AddComponent_InvalidPath_ReturnsFalse()
    {
        var scope = CreateScope();

        Assert.False(scope.AddComponent("nobody", new Rank(RankId.Soldier)));
    }

    #endregion

    #region HasRelation

    [Fact]
    public void HasRelation_ExistingRelation_ReturnsTrue()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        // Vito is father of Michael
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito);
        scope.WithAnchor("michael", michael);

        Assert.True(scope.HasRelation<FatherOf>("vito", "michael"));
    }

    [Fact]
    public void HasRelation_NoRelation_ReturnsFalse()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        scope.WithAnchor("a", a);
        scope.WithAnchor("b", b);

        Assert.False(scope.HasRelation<FatherOf>("a", "b"));
    }

    [Fact]
    public void HasRelation_InvalidFromPath_ReturnsFalse()
    {
        var scope = CreateScope();
        var b = SpawnEntity();
        scope.WithAnchor("b", b);

        Assert.False(scope.HasRelation<FatherOf>("nobody", "b"));
    }

    [Fact]
    public void HasRelation_InvalidToPath_ReturnsFalse()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        scope.WithAnchor("a", a);

        Assert.False(scope.HasRelation<FatherOf>("a", "nobody"));
    }

    #endregion

    #region GetRank

    [Fact]
    public void GetRank_Boss_ReturnsBoss()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Boss));
        scope.WithAnchor("don", entity);

        Assert.Equal(RankId.Boss, scope.GetRank("don"));
    }

    [Fact]
    public void GetRank_Soldier_ReturnsSoldier()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Soldier));
        scope.WithAnchor("grunt", entity);

        Assert.Equal(RankId.Soldier, scope.GetRank("grunt"));
    }

    [Fact]
    public void GetRank_NoRank_ReturnsNull()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("civilian", entity);

        Assert.Null(scope.GetRank("civilian"));
    }

    [Fact]
    public void GetRank_InvalidPath_ReturnsNull()
    {
        var scope = CreateScope();

        Assert.Null(scope.GetRank("nobody"));
    }

    [Fact]
    public void GetRank_ThroughRelation_ResolvesAndReturnsRank()
    {
        var scope = CreateScope();
        var tommy = SpawnEntity();
        var don = SpawnEntity();
        don.Add(new Rank(RankId.Boss));
        // Tommy is subordinate of the Don
        tommy.Add(new SubordinateOf(don), don);
        scope.WithAnchor("tommy", tommy);

        Assert.Equal(RankId.Boss, scope.GetRank("tommy.SubordinateOf"));
    }

    [Fact]
    public void GetRank_IsMutuallyExclusive()
    {
        // A single Rank component means only one rank per entity
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Soldier));
        scope.WithAnchor("entity", entity);

        Assert.Equal(RankId.Soldier, scope.GetRank("entity"));

        // Promoting to Boss replaces the rank
        scope.SetComponent("entity", new Rank(RankId.Boss));

        Assert.Equal(RankId.Boss, scope.GetRank("entity"));
    }

    #endregion

    #region Navigate – Edge Cases

    [Fact]
    public void Navigate_TrailingDot_ReturnsAnchorEntity()
    {
        // Path "player." → remaining path after anchor is empty,
        // so the while loop is skipped and the anchor entity is returned.
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("player", entity);

        Assert.Equal(entity, scope.Navigate("player."));
    }

    [Fact]
    public void Navigate_DoubleDot_HasEmptyMiddleSegment()
    {
        // "player..FatherOf" has an empty segment between dots
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("player", entity);

        Assert.Null(scope.Navigate("player..FatherOf"));
    }

    [Fact]
    public void Navigate_DotOnly_ReturnsNull()
    {
        var scope = CreateScope();

        // "." → anchor = "", which won't match
        Assert.Null(scope.Navigate("."));
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

        Assert.Equal(michael, scope.Navigate("vito.FatherOf"));
        Assert.Equal(connie, scope.Navigate("vito.MotherOf"));
        Assert.Equal(vito, scope.Navigate("vito"));
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

        Assert.True(scope.HasTag<Arrested>("target"));
        Assert.True(scope.HasTag<Disabled>("target"));
        Assert.Equal(1, scope.GetComponent<Disabled>("target")!.Value.Count);
    }

    [Fact]
    public void DisableCharacter_MultipleReasons_IncrementsCount()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new DisableCharacter<Arrested>("target").Apply(scope);
        new DisableCharacter<Killed>("target").Apply(scope);

        Assert.True(scope.HasTag<Arrested>("target"));
        Assert.True(scope.HasTag<Killed>("target"));
        Assert.Equal(2, scope.GetComponent<Disabled>("target")!.Value.Count);
    }

    [Fact]
    public void DisableCharacter_DuplicateReason_IsIgnored()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new DisableCharacter<Arrested>("target").Apply(scope);
        new DisableCharacter<Arrested>("target").Apply(scope);

        Assert.Equal(1, scope.GetComponent<Disabled>("target")!.Value.Count);
    }

    [Fact]
    public void EnableCharacter_RemovesReasonAndDisabledTag()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new DisableCharacter<Arrested>("target").Apply(scope);
        new EnableCharacter<Arrested>("target").Apply(scope);

        Assert.False(scope.HasTag<Arrested>("target"));
        Assert.False(scope.HasTag<Disabled>("target"));
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

        Assert.False(scope.HasTag<Arrested>("target"));
        Assert.True(scope.HasTag<Killed>("target"));
        Assert.True(scope.HasTag<Disabled>("target"));
        Assert.Equal(1, scope.GetComponent<Disabled>("target")!.Value.Count);
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

        Assert.False(scope.HasTag<Disabled>("target"));
    }

    [Fact]
    public void EnableCharacter_ReasonNotPresent_IsIgnored()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new EnableCharacter<Arrested>("target").Apply(scope);

        Assert.False(scope.HasTag<Disabled>("target"));
    }

    #endregion
}
