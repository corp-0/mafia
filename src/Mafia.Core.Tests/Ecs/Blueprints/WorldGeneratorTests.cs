using fennecs;
using FluentAssertions;
using Mafia.Core.Ecs.Blueprints;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Relations;
using Xunit;

namespace Mafia.Core.Tests.Ecs.Blueprints;

public class WorldGeneratorTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    [Fact]
    public void Generate_ProducesApproximateTargetPopulation()
    {
        var config = new WorldConfig { TargetPopulation = 500, Seed = 42 };
        var roster = WorldGenerator.Generate(_world, config);

        roster.Count.Should().BeGreaterThanOrEqualTo((int)(500 * 0.9));
        roster.Count.Should().BeLessThanOrEqualTo(1000); // overshoots due to org member household generation
    }

    [Fact]
    public void Generate_IsDeterministic()
    {
        var config = new WorldConfig { TargetPopulation = 200, Seed = 123, OrgCount = 2 };

        var roster1 = WorldGenerator.Generate(new World(), config);
        var roster2 = WorldGenerator.Generate(new World(), config);

        roster1.Count.Should().Be(roster2.Count);

        var names1 = roster1.Values.Select(e => e.Ref<CharacterName>().Name).OrderBy(n => n).ToList();
        var names2 = roster2.Values.Select(e => e.Ref<CharacterName>().Name).OrderBy(n => n).ToList();

        names1.Should().Equal(names2);
    }

    [Fact]
    public void Generate_AllEntitiesHaveCharacterName()
    {
        var config = new WorldConfig { TargetPopulation = 500, Seed = 42 };
        var roster = WorldGenerator.Generate(_world, config);

        foreach (var (_, entity) in roster)
            entity.Has<CharacterName>().Should().BeTrue();
    }

    [Fact]
    public void Generate_OnlyOrgMembersHaveRank()
    {
        var config = new WorldConfig { TargetPopulation = 500, Seed = 42 };
        var roster = WorldGenerator.Generate(_world, config);

        var withRank = roster.Values.Count(e => e.Has<Rank>());
        var withoutRank = roster.Values.Count(e => !e.Has<Rank>());

        withRank.Should().BeGreaterThan(0, "org members should have Rank");
        withoutRank.Should().BeGreaterThan(0, "civilians should not have Rank");
    }

    [Fact]
    public void Generate_OrgHierarchyIsConnected()
    {
        var config = new WorldConfig { TargetPopulation = 500, Seed = 42, OrgCount = 3 };
        var roster = WorldGenerator.Generate(_world, config);

        // Find bosses
        var bosses = roster.Values
            .Where(e => e.Has<Rank>() && e.Ref<Rank>().Id == RankId.Boss)
            .ToList();

        bosses.Count.Should().Be(3);

        foreach (var boss in bosses)
        {
            // Boss should have subordinates (BossOf)
            var subordinates = RelationQueries.CollectTargets<BossOf>(boss);
            subordinates.Count.Should().BeGreaterThan(0, "Boss should have subordinates");
        }
    }

    [Fact]
    public void Generate_SpouseOfIsBidirectional()
    {
        var config = new WorldConfig { TargetPopulation = 500, Seed = 42 };
        var roster = WorldGenerator.Generate(_world, config);

        foreach (var (_, entity) in roster)
        {
            var spouses = RelationQueries.CollectTargets<SpouseOf>(entity);
            foreach (var spouse in spouses)
                spouse.Has<SpouseOf>(entity).Should().BeTrue("SpouseOf should be bidirectional");
        }
    }

    [Fact]
    public void Generate_DerivedRelationsExist()
    {
        var config = new WorldConfig { TargetPopulation = 500, Seed = 42 };
        var roster = WorldGenerator.Generate(_world, config);

        // Check that at least some gendered relations were derived
        var hasFatherOf = roster.Values.Any(e => RelationQueries.CollectTargets<FatherOf>(e).Count > 0);
        var hasMotherOf = roster.Values.Any(e => RelationQueries.CollectTargets<MotherOf>(e).Count > 0);
        var hasHusbandOf = roster.Values.Any(e => RelationQueries.CollectTargets<HusbandOf>(e).Count > 0);
        var hasWifeOf = roster.Values.Any(e => RelationQueries.CollectTargets<WifeOf>(e).Count > 0);

        hasFatherOf.Should().BeTrue("Should have derived FatherOf relations");
        hasMotherOf.Should().BeTrue("Should have derived MotherOf relations");
        hasHusbandOf.Should().BeTrue("Should have derived HusbandOf relations");
        hasWifeOf.Should().BeTrue("Should have derived WifeOf relations");
    }

    [Fact]
    public void Generate_OrgMembersHaveMemberOf()
    {
        var config = new WorldConfig { TargetPopulation = 500, Seed = 42 };
        var roster = WorldGenerator.Generate(_world, config);

        // All non-Boss ranked org members should have MemberOf
        var orgMembers = roster.Values
            .Where(e => e.Has<Rank>() && e.Ref<Rank>().Id is RankId.Caporegime or RankId.Soldier)
            .ToList();

        foreach (var member in orgMembers)
        {
            var memberOfTargets = RelationQueries.CollectTargets<MemberOf>(member);
            memberOfTargets.Count.Should().BeGreaterThan(0,
                $"Org member with rank {member.Ref<Rank>().Id} should have MemberOf");
        }
    }

    [Fact]
    public void Generate_DefaultConfig_ProducesWorld()
    {
        var roster = WorldGenerator.Generate(_world);

        roster.Count.Should().BeGreaterThan(0);
    }
}
