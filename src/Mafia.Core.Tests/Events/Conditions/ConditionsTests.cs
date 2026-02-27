using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Conditions;
using Xunit;

namespace Mafia.Core.Tests.Events.Conditions;

public class ConditionsTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    private EntityScope CreateScope() => new(_world);

    private Entity SpawnEntity() => _world.Spawn();

    #region HasTagCondition

    [Fact]
    public void HasTag_EntityHasTag_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add<Arrested>();
        scope.WithAnchor("root", entity);

        Assert.True(new HasTagCondition<Arrested>("root").Evaluate(scope));
    }

    [Fact]
    public void HasTag_EntityLacksTag_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        Assert.False(new HasTagCondition<Arrested>("root").Evaluate(scope));
    }

    [Fact]
    public void HasTag_InvalidPath_ReturnsFalse()
    {
        var scope = CreateScope();

        Assert.False(new HasTagCondition<Arrested>("nobody").Evaluate(scope));
    }

    [Fact]
    public void HasTag_ThroughRelation_Evaluates()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        michael.Add<Killed>();
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito);

        Assert.True(new HasTagCondition<Killed>("vito.FatherOf").Evaluate(scope));
    }

    #endregion

    #region HasRelationship

    [Fact]
    public void HasRelationship_Exists_ReturnsTrue()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito).WithAnchor("michael", michael);

        Assert.True(new HasRelationship<FatherOf>("vito", "michael").Evaluate(scope));
    }

    [Fact]
    public void HasRelationship_DoesNotExist_ReturnsFalse()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        scope.WithAnchor("a", a).WithAnchor("b", b);

        Assert.False(new HasRelationship<FatherOf>("a", "b").Evaluate(scope));
    }

    [Fact]
    public void HasRelationship_WrongDirection_ReturnsFalse()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito).WithAnchor("michael", michael);

        Assert.False(new HasRelationship<FatherOf>("michael", "vito").Evaluate(scope));
    }

    [Fact]
    public void HasRelationship_WrongType_ReturnsFalse()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito).WithAnchor("michael", michael);

        Assert.False(new HasRelationship<BossOf>("vito", "michael").Evaluate(scope));
    }

    [Fact]
    public void HasRelationship_InvalidPath_ReturnsFalse()
    {
        var scope = CreateScope();

        Assert.False(new HasRelationship<FatherOf>("nobody", "ghost").Evaluate(scope));
    }

    #endregion

    #region HasMinimumRank

    [Fact]
    public void HasMinimumRank_ExactMatch_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Caporegime));
        scope.WithAnchor("root", entity);

        Assert.True(new HasMinimumRank("root", RankId.Caporegime).Evaluate(scope));
    }

    [Fact]
    public void HasMinimumRank_HigherRank_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Boss));
        scope.WithAnchor("root", entity);

        Assert.True(new HasMinimumRank("root", RankId.Soldier).Evaluate(scope));
    }

    [Fact]
    public void HasMinimumRank_LowerRank_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Soldier));
        scope.WithAnchor("root", entity);

        Assert.False(new HasMinimumRank("root", RankId.Caporegime).Evaluate(scope));
    }

    [Fact]
    public void HasMinimumRank_NoRank_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        Assert.False(new HasMinimumRank("root", RankId.Associate).Evaluate(scope));
    }

    [Fact]
    public void HasMinimumRank_InvalidPath_ReturnsFalse()
    {
        var scope = CreateScope();

        Assert.False(new HasMinimumRank("nobody", RankId.Associate).Evaluate(scope));
    }

    [Fact]
    public void HasMinimumRank_LowestRank_AssociateAlwaysPasses()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Associate));
        scope.WithAnchor("root", entity);

        Assert.True(new HasMinimumRank("root", RankId.Associate).Evaluate(scope));
    }

    #endregion

    #region StatThreshold

    [Fact]
    public void StatThreshold_GreaterThan_Above_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(7));
        scope.WithAnchor("root", entity);

        Assert.True(new StatThreshold<Muscle>("root", Comparison.GreaterThan, 5).Evaluate(scope));
    }

    [Fact]
    public void StatThreshold_GreaterThan_Equal_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("root", entity);

        Assert.False(new StatThreshold<Muscle>("root", Comparison.GreaterThan, 5).Evaluate(scope));
    }

    [Fact]
    public void StatThreshold_GreaterThan_Below_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(3));
        scope.WithAnchor("root", entity);

        Assert.False(new StatThreshold<Muscle>("root", Comparison.GreaterThan, 5).Evaluate(scope));
    }

    [Fact]
    public void StatThreshold_LessThan_Below_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(3));
        scope.WithAnchor("root", entity);

        Assert.True(new StatThreshold<Muscle>("root", Comparison.LessThan, 5).Evaluate(scope));
    }

    [Fact]
    public void StatThreshold_LessThan_Equal_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("root", entity);

        Assert.False(new StatThreshold<Muscle>("root", Comparison.LessThan, 5).Evaluate(scope));
    }

    [Fact]
    public void StatThreshold_GreaterThanOrEqual_Equal_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("root", entity);

        Assert.True(new StatThreshold<Muscle>("root", Comparison.GreaterThanOrEqualTo, 5).Evaluate(scope));
    }

    [Fact]
    public void StatThreshold_LessThanOrEqual_Equal_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("root", entity);

        Assert.True(new StatThreshold<Muscle>("root", Comparison.LessThanOrEqualTo, 5).Evaluate(scope));
    }

    [Fact]
    public void StatThreshold_MissingStat_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        Assert.False(new StatThreshold<Muscle>("root", Comparison.GreaterThan, 0).Evaluate(scope));
    }

    [Fact]
    public void StatThreshold_InvalidPath_ReturnsFalse()
    {
        var scope = CreateScope();

        Assert.False(new StatThreshold<Muscle>("nobody", Comparison.GreaterThan, 0).Evaluate(scope));
    }

    [Fact]
    public void StatThreshold_Equal_Matching_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("root", entity);

        Assert.True(new StatThreshold<Muscle>("root", Comparison.Equal, 5).Evaluate(scope));
    }

    [Fact]
    public void StatThreshold_Equal_NotMatching_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("root", entity);

        Assert.False(new StatThreshold<Muscle>("root", Comparison.Equal, 3).Evaluate(scope));
    }

    [Fact]
    public void StatThreshold_WorksWithDifferentStats()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Nerve(8));
        entity.Add(new Charm(3));
        scope.WithAnchor("root", entity);

        Assert.True(new StatThreshold<Nerve>("root", Comparison.GreaterThan, 5).Evaluate(scope));
        Assert.False(new StatThreshold<Charm>("root", Comparison.GreaterThan, 5).Evaluate(scope));
    }

    #endregion

    #region SameLocation

    [Fact]
    public void SameLocation_NoLocationRelation_ReturnsFalse()
    {
        // SameLocation navigates "{path}.location" which requires a relation named "location"
        // No such relation exists yet, so this always returns false
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        scope.WithAnchor("a", a).WithAnchor("b", b);

        Assert.False(new SameLocation("a", "b").Evaluate(scope));
    }

    [Fact]
    public void SameLocation_InvalidPaths_ReturnsFalse()
    {
        var scope = CreateScope();

        Assert.False(new SameLocation("nobody", "ghost").Evaluate(scope));
    }

    #endregion

    #region Composite Conditions – AllOf

    [Fact]
    public void AllOf_AllTrue_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add<Arrested>();
        entity.Add(new Muscle(7));
        scope.WithAnchor("root", entity);

        var condition = new AllOf(
            new HasTagCondition<Arrested>("root"),
            new StatThreshold<Muscle>("root", Comparison.GreaterThan, 5)
        );

        Assert.True(condition.Evaluate(scope));
    }

    [Fact]
    public void AllOf_OneFalse_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add<Arrested>();
        entity.Add(new Muscle(3));
        scope.WithAnchor("root", entity);

        var condition = new AllOf(
            new HasTagCondition<Arrested>("root"),
            new StatThreshold<Muscle>("root", Comparison.GreaterThan, 5)
        );

        Assert.False(condition.Evaluate(scope));
    }

    [Fact]
    public void AllOf_Empty_ReturnsTrue()
    {
        var scope = CreateScope();

        Assert.True(new AllOf().Evaluate(scope));
    }

    [Fact]
    public void AllOf_SingleCondition_DelegatesToIt()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add<Arrested>();
        scope.WithAnchor("root", entity);

        Assert.True(new AllOf(new HasTagCondition<Arrested>("root")).Evaluate(scope));
        Assert.False(new AllOf(new HasTagCondition<Killed>("root")).Evaluate(scope));
    }

    #endregion

    #region Composite Conditions – AnyOf

    [Fact]
    public void AnyOf_OneTrue_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add<Arrested>();
        scope.WithAnchor("root", entity);

        var condition = new AnyOf(
            new HasTagCondition<Arrested>("root"),
            new HasTagCondition<Killed>("root")
        );

        Assert.True(condition.Evaluate(scope));
    }

    [Fact]
    public void AnyOf_NoneTrue_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        var condition = new AnyOf(
            new HasTagCondition<Arrested>("root"),
            new HasTagCondition<Killed>("root")
        );

        Assert.False(condition.Evaluate(scope));
    }

    [Fact]
    public void AnyOf_Empty_ReturnsFalse()
    {
        var scope = CreateScope();

        Assert.False(new AnyOf().Evaluate(scope));
    }

    #endregion

    #region Composite Conditions – NoneOf

    [Fact]
    public void NoneOf_NoneTrue_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        var condition = new NoneOf(
            new HasTagCondition<Arrested>("root"),
            new HasTagCondition<Killed>("root")
        );

        Assert.True(condition.Evaluate(scope));
    }

    [Fact]
    public void NoneOf_OneTrue_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add<Arrested>();
        scope.WithAnchor("root", entity);

        var condition = new NoneOf(
            new HasTagCondition<Arrested>("root"),
            new HasTagCondition<Killed>("root")
        );

        Assert.False(condition.Evaluate(scope));
    }

    [Fact]
    public void NoneOf_Empty_ReturnsTrue()
    {
        var scope = CreateScope();

        Assert.True(new NoneOf().Evaluate(scope));
    }

    #endregion

    #region Composite Conditions – Nesting

    [Fact]
    public void Composites_CanBeNested()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add<Arrested>();
        entity.Add(new Muscle(7));
        entity.Add(new Rank(RankId.Caporegime));
        scope.WithAnchor("root", entity);

        // (arrested AND muscle > 5) OR rank >= Boss
        var condition = new AnyOf(
            new AllOf(
                new HasTagCondition<Arrested>("root"),
                new StatThreshold<Muscle>("root", Comparison.GreaterThan, 5)
            ),
            new HasMinimumRank("root", RankId.Boss)
        );

        // First branch is true (arrested + muscle 7 > 5), second is false (Caporegime < Boss)
        Assert.True(condition.Evaluate(scope));
    }

    [Fact]
    public void Composites_NoneOf_WithAllOf_Inside()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(3));
        scope.WithAnchor("root", entity);

        // NOT (arrested AND muscle > 5). Neither is true, so NoneOf returns true
        var condition = new NoneOf(
            new AllOf(
                new HasTagCondition<Arrested>("root"),
                new StatThreshold<Muscle>("root", Comparison.GreaterThan, 5)
            )
        );

        Assert.True(condition.Evaluate(scope));
    }

    #endregion
}
