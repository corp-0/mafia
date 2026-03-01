using fennecs;
using FluentAssertions;
using Mafia.Core.WorldGen;
using Mafia.Core.WorldGen.Phases;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Relations;
using Xunit;

namespace Mafia.Core.Tests.WorldGen;

public class WorldGeneratorTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    private WorldGenerationContext CreateContext(WorldConfig? config = null)
    {
        config ??= new WorldConfig();
        var rng = new SeededRandom(config.Seed);
        var nameGen = new NameGenerator(rng);
        var statRoller = new StatRoller(rng, config);
        var factory = new CharacterFactory(_world);

        return new WorldGenerationContext
        {
            World = _world,
            Config = config,
            Rng = rng,
            NameGen = nameGen,
            StatRoller = statRoller,
            CharacterFactory = factory
        };
    }

    private static void RunPhases(WorldGenerationContext ctx, params IGenerationPhase[] phases)
    {
        foreach (var phase in phases)
            phase.Execute(ctx);
    }

    [Fact]
    public void Generate_ProducesApproximateTargetPopulation()
    {
        var ctx = CreateContext(new WorldConfig { TargetPopulation = 500, Seed = 42 });
        RunPhases(ctx,
            new OrgSkeletonPhase(),
            new AnchorSpawningPhase(),
            new HouseholdPhase(),
            new FillerPopulationPhase());

        ctx.Roster.Count.Should().BeGreaterThanOrEqualTo((int)(500 * 0.9));
        ctx.Roster.Count.Should().BeLessThanOrEqualTo(1000);
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
        var ctx = CreateContext(new WorldConfig { TargetPopulation = 200, Seed = 42, OrgCount = 2 });
        RunPhases(ctx,
            new OrgSkeletonPhase(),
            new AnchorSpawningPhase(),
            new HouseholdPhase(),
            new FillerPopulationPhase());

        foreach (var (_, entity) in ctx.Roster)
            entity.Has<CharacterName>().Should().BeTrue();
    }

    [Fact]
    public void Generate_OnlyOrgMembersHaveRank()
    {
        var ctx = CreateContext(new WorldConfig { TargetPopulation = 200, Seed = 42, OrgCount = 2 });
        RunPhases(ctx,
            new OrgSkeletonPhase(),
            new AnchorSpawningPhase(),
            new HouseholdPhase(),
            new FillerPopulationPhase());

        var withRank = ctx.Roster.Values.Count(e => e.Has<Rank>());
        var withoutRank = ctx.Roster.Values.Count(e => !e.Has<Rank>());

        withRank.Should().BeGreaterThan(0, "org members should have Rank");
        withoutRank.Should().BeGreaterThan(0, "civilians should not have Rank");
    }

    [Fact]
    public void Generate_OrgHierarchyIsConnected()
    {
        var ctx = CreateContext(new WorldConfig { TargetPopulation = 500, Seed = 42, OrgCount = 3 });
        RunPhases(ctx,
            new OrgSkeletonPhase(),
            new AnchorSpawningPhase());

        var bosses = ctx.Roster.Values
            .Where(e => e.Has<Rank>() && e.Ref<Rank>().Id == RankId.Boss)
            .ToList();

        bosses.Count.Should().Be(3);

        foreach (var boss in bosses)
        {
            var subordinates = RelationQueries.CollectTargets<BossOf>(boss);
            subordinates.Count.Should().BeGreaterThan(0, "Boss should have subordinates");
        }
    }

    [Fact]
    public void Generate_SpouseOfIsBidirectional()
    {
        var ctx = CreateContext(new WorldConfig { TargetPopulation = 200, Seed = 42, OrgCount = 2 });
        RunPhases(ctx,
            new OrgSkeletonPhase(),
            new AnchorSpawningPhase(),
            new HouseholdPhase());

        foreach (var (_, entity) in ctx.Roster)
        {
            var spouses = RelationQueries.CollectTargets<SpouseOf>(entity);
            foreach (var spouse in spouses)
                spouse.Has<SpouseOf>(entity).Should().BeTrue("SpouseOf should be bidirectional");
        }
    }

    [Fact]
    public void Generate_DerivedRelationsExist()
    {
        var ctx = CreateContext(new WorldConfig { TargetPopulation = 200, Seed = 42, OrgCount = 2 });
        RunPhases(ctx,
            new OrgSkeletonPhase(),
            new AnchorSpawningPhase(),
            new HouseholdPhase(),
            new RelationResolutionPhase());

        var hasFatherOf = ctx.Roster.Values.Any(e => RelationQueries.CollectTargets<FatherOf>(e).Count > 0);
        var hasMotherOf = ctx.Roster.Values.Any(e => RelationQueries.CollectTargets<MotherOf>(e).Count > 0);
        var hasHusbandOf = ctx.Roster.Values.Any(e => RelationQueries.CollectTargets<HusbandOf>(e).Count > 0);
        var hasWifeOf = ctx.Roster.Values.Any(e => RelationQueries.CollectTargets<WifeOf>(e).Count > 0);

        hasFatherOf.Should().BeTrue("Should have derived FatherOf relations");
        hasMotherOf.Should().BeTrue("Should have derived MotherOf relations");
        hasHusbandOf.Should().BeTrue("Should have derived HusbandOf relations");
        hasWifeOf.Should().BeTrue("Should have derived WifeOf relations");
    }

    [Fact]
    public void Generate_OrgMembersHaveMemberOf()
    {
        var ctx = CreateContext(new WorldConfig { TargetPopulation = 500, Seed = 42 });
        RunPhases(ctx,
            new OrgSkeletonPhase(),
            new AnchorSpawningPhase());

        var orgMembers = ctx.Roster.Values
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
