using FluentAssertions;
using fennecs;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Xunit;

namespace Mafia.Core.Tests.Ecs.Relations;

public class TargetPoolResolverTests : IDisposable
{
    private readonly World _world = new();
    private readonly TargetPoolResolver _resolver = new();

    public void Dispose() => _world.Dispose();

    private Entity Spawn(RankId? rank = null)
    {
        var e = _world.Spawn();
        e.Add<Character>();
        if (rank is { } r)
            e.Add(new Rank(r));
        return e;
    }

    private static void SetupBossSubordinate(Entity boss, Entity sub)
    {
        boss.Add(new BossOf(sub), sub);
        sub.Add(new SubordinateOf(boss), boss);
    }

    #region root_crew Capo is crew leader

    [Fact]
    public void Crew_Capo_ReturnsEntireSubtree()
    {
        // Capo → Soldiers → Associates
        var capo = Spawn(RankId.Caporegime);
        var soldier1 = Spawn(RankId.Soldier);
        var soldier2 = Spawn(RankId.Soldier);
        var associate = Spawn(RankId.Associate);

        SetupBossSubordinate(capo, soldier1);
        SetupBossSubordinate(capo, soldier2);
        SetupBossSubordinate(soldier1, associate);

        var crew = _resolver.Resolve("root_crew", capo)!;

        crew.Count.Should().Be(3);
        crew.Should().Contain(soldier1);
        crew.Should().Contain(soldier2);
        crew.Should().Contain(associate);
    }

    #endregion

    #region root_crew Soldier/Associate gets whole crew

    [Fact]
    public void Crew_Soldier_ReturnsWholeCrewExcludingSelf()
    {
        // Capo → Soldier1, Soldier2 → Associate
        var capo = Spawn(RankId.Caporegime);
        var soldier1 = Spawn(RankId.Soldier);
        var soldier2 = Spawn(RankId.Soldier);
        var associate = Spawn(RankId.Associate);

        SetupBossSubordinate(capo, soldier1);
        SetupBossSubordinate(capo, soldier2);
        SetupBossSubordinate(soldier2, associate);

        var crew = _resolver.Resolve("root_crew", soldier1)!;

        // Capo + soldier2 + associate, but not soldier1 (self)
        crew.Count.Should().Be(3);
        crew.Should().Contain(capo);
        crew.Should().Contain(soldier2);
        crew.Should().Contain(associate);
        crew.Should().NotContain(soldier1);
    }

    [Fact]
    public void Crew_Associate_ClimbsThroughSoldierToCapo()
    {
        // Capo → Soldier → Associate1, Associate2
        var capo = Spawn(RankId.Caporegime);
        var soldier = Spawn(RankId.Soldier);
        var associate1 = Spawn(RankId.Associate);
        var associate2 = Spawn(RankId.Associate);

        SetupBossSubordinate(capo, soldier);
        SetupBossSubordinate(soldier, associate1);
        SetupBossSubordinate(soldier, associate2);

        var crew = _resolver.Resolve("root_crew", associate1)!;

        // Capo + soldier + associate2, but not associate1 (self)
        crew.Count.Should().Be(3);
        crew.Should().Contain(capo);
        crew.Should().Contain(soldier);
        crew.Should().Contain(associate2);
        crew.Should().NotContain(associate1);
    }

    [Fact]
    public void Crew_NoBossChain_ReturnsEmpty()
    {
        var loner = Spawn(RankId.Soldier);

        var crew = _resolver.Resolve("root_crew", loner)!;

        crew.Should().BeEmpty();
    }

    #endregion

    #region root_crime_family

    [Fact]
    public void CrimeFamily_ClimbsToBoss_CollectsWholeTree()
    {
        // Boss → Capo1 (underboss), Capo2 → Soldiers
        var boss = Spawn(RankId.Boss);
        var capo1 = Spawn(RankId.Caporegime);
        var capo2 = Spawn(RankId.Caporegime);
        var soldier1 = Spawn(RankId.Soldier);
        var soldier2 = Spawn(RankId.Soldier);

        SetupBossSubordinate(boss, capo1);
        SetupBossSubordinate(boss, capo2);
        SetupBossSubordinate(capo2, soldier1);
        SetupBossSubordinate(capo2, soldier2);

        // Ask from soldier1's perspective should climb to Boss
        var family = _resolver.Resolve("root_crime_family", soldier1)!;

        // Boss, Capo1, Capo2, Soldier1, Soldier2 all Soldier+
        family.Count.Should().Be(5);
        family.Should().Contain(boss);
        family.Should().Contain(capo1);
        family.Should().Contain(capo2);
        family.Should().Contain(soldier1);
        family.Should().Contain(soldier2);
    }

    [Fact]
    public void CrimeFamily_ExcludesAssociates()
    {
        var boss = Spawn(RankId.Boss);
        var capo = Spawn(RankId.Caporegime);
        var soldier = Spawn(RankId.Soldier);
        var associate = Spawn(RankId.Associate);

        SetupBossSubordinate(boss, capo);
        SetupBossSubordinate(capo, soldier);
        SetupBossSubordinate(capo, associate);

        var family = _resolver.Resolve("root_crime_family", soldier)!;

        family.Should().Contain(boss);
        family.Should().Contain(capo);
        family.Should().Contain(soldier);
        family.Should().NotContain(associate);
    }

    [Fact]
    public void CrimeFamily_ExcludesEntitiesWithNoRank()
    {
        var boss = Spawn(RankId.Boss);
        var noRank = _world.Spawn(); // no Rank component at all
        noRank.Add<Character>();
        boss.Add(new BossOf(noRank), noRank);
        noRank.Add(new SubordinateOf(boss), boss);

        var family = _resolver.Resolve("root_crime_family", boss)!;

        family.Should().HaveCount(1); // just the Boss
        family.Should().Contain(boss);
        family.Should().NotContain(noRank);
    }

    [Fact]
    public void CrimeFamily_FromBoss_IncludesSelf()
    {
        var boss = Spawn(RankId.Boss);
        var soldier = Spawn(RankId.Soldier);
        SetupBossSubordinate(boss, soldier);

        var family = _resolver.Resolve("root_crime_family", boss)!;

        family.Count.Should().Be(2);
        family.Should().Contain(boss);
        family.Should().Contain(soldier);
    }

    #endregion

    #region Simple pools (delegation)

    [Fact]
    public void RootSubordinates_ReturnsDirectBossOfTargets()
    {
        var boss = Spawn(RankId.Boss);
        var sub = Spawn(RankId.Soldier);
        boss.Add(new BossOf(sub), sub);

        var result = _resolver.Resolve("root_subordinates", boss)!;

        result.Should().HaveCount(1);
        result.Should().Contain(sub);
    }

    [Fact]
    public void RootFamily_ReturnsFamilyRelationTargets()
    {
        var vito = Spawn();
        var michael = Spawn();
        vito.Add(new FatherOf(michael), michael);

        var result = _resolver.Resolve("root_family", vito)!;

        result.Should().HaveCount(1);
        result.Should().Contain(michael);
    }

    [Fact]
    public void RootCreditors_ReturnsOwesTargets()
    {
        var debtor = Spawn();
        var creditor = Spawn();
        debtor.Add(new Owes(creditor) { Amount = 500 }, creditor);

        var result = _resolver.Resolve("root_creditors", debtor)!;

        result.Should().HaveCount(1);
        result.Should().Contain(creditor);
    }

    #endregion

    #region Unknown pool

    [Fact]
    public void UnknownPool_ReturnsNull()
    {
        var entity = Spawn();

        var result = _resolver.Resolve("same_territory", entity);

        result.Should().BeNull();
    }

    #endregion
}
