using fennecs;
using FluentAssertions;
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

        new HasTagCondition<Arrested>("root").Evaluate(scope).Should().BeTrue();
    }

    [Fact]
    public void HasTag_EntityLacksTag_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        new HasTagCondition<Arrested>("root").Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void HasTag_InvalidPath_ReturnsFalse()
    {
        var scope = CreateScope();

        new HasTagCondition<Arrested>("nobody").Evaluate(scope).Should().BeFalse();
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

        new HasTagCondition<Killed>("vito.FatherOf").Evaluate(scope).Should().BeTrue();
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

        new HasRelationship<FatherOf>("vito", "michael").Evaluate(scope).Should().BeTrue();
    }

    [Fact]
    public void HasRelationship_DoesNotExist_ReturnsFalse()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        scope.WithAnchor("a", a).WithAnchor("b", b);

        new HasRelationship<FatherOf>("a", "b").Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void HasRelationship_WrongDirection_ReturnsFalse()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito).WithAnchor("michael", michael);

        new HasRelationship<FatherOf>("michael", "vito").Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void HasRelationship_WrongType_ReturnsFalse()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito).WithAnchor("michael", michael);

        new HasRelationship<BossOf>("vito", "michael").Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void HasRelationship_InvalidPath_ReturnsFalse()
    {
        var scope = CreateScope();

        new HasRelationship<FatherOf>("nobody", "ghost").Evaluate(scope).Should().BeFalse();
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

        new HasMinimumRank("root", RankId.Caporegime).Evaluate(scope).Should().BeTrue();
    }

    [Fact]
    public void HasMinimumRank_HigherRank_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Boss));
        scope.WithAnchor("root", entity);

        new HasMinimumRank("root", RankId.Soldier).Evaluate(scope).Should().BeTrue();
    }

    [Fact]
    public void HasMinimumRank_LowerRank_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Soldier));
        scope.WithAnchor("root", entity);

        new HasMinimumRank("root", RankId.Caporegime).Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void HasMinimumRank_NoRank_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        new HasMinimumRank("root", RankId.Associate).Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void HasMinimumRank_InvalidPath_ReturnsFalse()
    {
        var scope = CreateScope();

        new HasMinimumRank("nobody", RankId.Associate).Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void HasMinimumRank_LowestRank_AssociateAlwaysPasses()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Associate));
        scope.WithAnchor("root", entity);

        new HasMinimumRank("root", RankId.Associate).Evaluate(scope).Should().BeTrue();
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

        new StatThreshold<Muscle>("root", Comparison.GreaterThan, 5).Evaluate(scope).Should().BeTrue();
    }

    [Fact]
    public void StatThreshold_GreaterThan_Equal_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("root", entity);

        new StatThreshold<Muscle>("root", Comparison.GreaterThan, 5).Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void StatThreshold_GreaterThan_Below_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(3));
        scope.WithAnchor("root", entity);

        new StatThreshold<Muscle>("root", Comparison.GreaterThan, 5).Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void StatThreshold_LessThan_Below_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(3));
        scope.WithAnchor("root", entity);

        new StatThreshold<Muscle>("root", Comparison.LessThan, 5).Evaluate(scope).Should().BeTrue();
    }

    [Fact]
    public void StatThreshold_LessThan_Equal_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("root", entity);

        new StatThreshold<Muscle>("root", Comparison.LessThan, 5).Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void StatThreshold_GreaterThanOrEqual_Equal_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("root", entity);

        new StatThreshold<Muscle>("root", Comparison.GreaterThanOrEqualTo, 5).Evaluate(scope).Should().BeTrue();
    }

    [Fact]
    public void StatThreshold_LessThanOrEqual_Equal_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("root", entity);

        new StatThreshold<Muscle>("root", Comparison.LessThanOrEqualTo, 5).Evaluate(scope).Should().BeTrue();
    }

    [Fact]
    public void StatThreshold_MissingStat_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("root", entity);

        new StatThreshold<Muscle>("root", Comparison.GreaterThan, 0).Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void StatThreshold_InvalidPath_ReturnsFalse()
    {
        var scope = CreateScope();

        new StatThreshold<Muscle>("nobody", Comparison.GreaterThan, 0).Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void StatThreshold_Equal_Matching_ReturnsTrue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("root", entity);

        new StatThreshold<Muscle>("root", Comparison.Equal, 5).Evaluate(scope).Should().BeTrue();
    }

    [Fact]
    public void StatThreshold_Equal_NotMatching_ReturnsFalse()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("root", entity);

        new StatThreshold<Muscle>("root", Comparison.Equal, 3).Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void StatThreshold_WorksWithDifferentStats()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Nerve(8));
        entity.Add(new Charm(3));
        scope.WithAnchor("root", entity);

        new StatThreshold<Nerve>("root", Comparison.GreaterThan, 5).Evaluate(scope).Should().BeTrue();
        new StatThreshold<Charm>("root", Comparison.GreaterThan, 5).Evaluate(scope).Should().BeFalse();
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

        new SameLocation("a", "b").Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void SameLocation_InvalidPaths_ReturnsFalse()
    {
        var scope = CreateScope();

        new SameLocation("nobody", "ghost").Evaluate(scope).Should().BeFalse();
    }

    #endregion

    #region Composite Conditions  AllOf

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

        condition.Evaluate(scope).Should().BeTrue();
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

        condition.Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void AllOf_Empty_ReturnsTrue()
    {
        var scope = CreateScope();

        new AllOf().Evaluate(scope).Should().BeTrue();
    }

    [Fact]
    public void AllOf_SingleCondition_DelegatesToIt()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add<Arrested>();
        scope.WithAnchor("root", entity);

        new AllOf(new HasTagCondition<Arrested>("root")).Evaluate(scope).Should().BeTrue();
        new AllOf(new HasTagCondition<Killed>("root")).Evaluate(scope).Should().BeFalse();
    }

    #endregion

    #region Composite Conditions  AnyOf

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

        condition.Evaluate(scope).Should().BeTrue();
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

        condition.Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void AnyOf_Empty_ReturnsFalse()
    {
        var scope = CreateScope();

        new AnyOf().Evaluate(scope).Should().BeFalse();
    }

    #endregion

    #region Composite Conditions  NoneOf

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

        condition.Evaluate(scope).Should().BeTrue();
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

        condition.Evaluate(scope).Should().BeFalse();
    }

    [Fact]
    public void NoneOf_Empty_ReturnsTrue()
    {
        var scope = CreateScope();

        new NoneOf().Evaluate(scope).Should().BeTrue();
    }

    #endregion

    #region Composite Conditions  Nesting

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
        condition.Evaluate(scope).Should().BeTrue();
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

        condition.Evaluate(scope).Should().BeTrue();
    }

    #endregion
}
