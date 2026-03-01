using fennecs;
using FluentAssertions;
using Mafia.Core.Context;
using Mafia.Core.WorldGen;
using Mafia.Core.WorldGen.Phases;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Extensions;
using Xunit;

namespace Mafia.Core.Tests.WorldGen;

public class HouseholdEntityPhaseTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    private WorldGenerationContext CreateContext()
    {
        var config = new WorldConfig();
        var rng = new SeededRandom(42);
        return new WorldGenerationContext
        {
            World = _world,
            Config = config,
            Rng = rng,
            NameGen = new NameGenerator(rng),
            StatRoller = new StatRoller(rng, config),
            CharacterFactory = new CharacterFactory(_world)
        };
    }

    private Entity SpawnCharacter(WorldGenerationContext ctx, string name, int age,
        Sex sex = Sex.Male, RankId? rank = null)
    {
        var entity = _world.Spawn();
        entity.Add<Character>();
        entity.Add(new CharacterName(name, ""));
        entity.Add(new Age { Amount = age });
        entity.Add(sex);
        if (rank.HasValue)
            entity.Add(new Rank(rank.Value));
        var id = ctx.NextId();
        ctx.Roster[id] = entity;
        return entity;
    }

    [Fact]
    public void Execute_SingleCharacter_CreatesHousehold()
    {
        var ctx = CreateContext();
        var character = SpawnCharacter(ctx, "Vito", 50, rank: RankId.Boss);

        new HouseholdEntityPhase().Execute(ctx);

        var households = character.Get<MemberOfHousehold>(Match.Entity);
        households.Length.Should().Be(1);
        var household = households[0].Target;
        household.Has<Household>().Should().BeTrue();
    }

    [Fact]
    public void Execute_MarriedCouple_SameHousehold()
    {
        var ctx = CreateContext();
        var husband = SpawnCharacter(ctx, "Vito", 50, rank: RankId.Boss);
        var wife = SpawnCharacter(ctx, "Carmela", 45, Sex.Female);
        husband.TryAddRelation<SpouseOf>(wife);
        wife.TryAddRelation<SpouseOf>(husband);

        new HouseholdEntityPhase().Execute(ctx);

        var husbandHouseholds = husband.Get<MemberOfHousehold>(Match.Entity);
        var wifeHouseholds = wife.Get<MemberOfHousehold>(Match.Entity);
        husbandHouseholds.Length.Should().Be(1);
        wifeHouseholds.Length.Should().Be(1);
        husbandHouseholds[0].Target.Should().Be(wifeHouseholds[0].Target);
    }

    [Fact]
    public void Execute_FamilyWithChildren_AllInSameHousehold()
    {
        var ctx = CreateContext();
        var father = SpawnCharacter(ctx, "Vito", 50, rank: RankId.Boss);
        var mother = SpawnCharacter(ctx, "Carmela", 45, Sex.Female);
        var son = SpawnCharacter(ctx, "Michael", 25);
        father.TryAddRelation<SpouseOf>(mother);
        mother.TryAddRelation<SpouseOf>(father);
        father.TryAddRelation<ParentOf>(son);
        mother.TryAddRelation<ParentOf>(son);

        new HouseholdEntityPhase().Execute(ctx);

        var household = father.Get<MemberOfHousehold>(Match.Entity)[0].Target;

        son.Get<MemberOfHousehold>(Match.Entity).Length.Should().Be(1);
        son.Get<MemberOfHousehold>(Match.Entity)[0].Target.Should().Be(household);
    }

    [Fact]
    public void Execute_HeadIsHighestRank()
    {
        var ctx = CreateContext();
        var soldier = SpawnCharacter(ctx, "Soldier", 50, rank: RankId.Soldier);
        var boss = SpawnCharacter(ctx, "Boss", 40, rank: RankId.Boss);
        soldier.TryAddRelation<SpouseOf>(boss);
        boss.TryAddRelation<SpouseOf>(soldier);

        new HouseholdEntityPhase().Execute(ctx);

        // Boss processes first (higher rank), but they share household
        // Head should be the boss
        var household = boss.Get<MemberOfHousehold>(Match.Entity)[0].Target;
        var heads = household.Get<HeadOfHousehold>(Match.Entity);
        heads.Length.Should().Be(1);
        heads[0].Target.Should().Be(boss);
    }

    [Fact]
    public void Execute_SameRank_HeadIsOldest()
    {
        var ctx = CreateContext();
        var older = SpawnCharacter(ctx, "Elder", 60, rank: RankId.Soldier);
        var younger = SpawnCharacter(ctx, "Youth", 30, rank: RankId.Soldier);
        older.TryAddRelation<SpouseOf>(younger);
        younger.TryAddRelation<SpouseOf>(older);

        new HouseholdEntityPhase().Execute(ctx);

        var household = older.Get<MemberOfHousehold>(Match.Entity)[0].Target;
        var heads = household.Get<HeadOfHousehold>(Match.Entity);
        heads[0].Target.Should().Be(older);
    }

    [Fact]
    public void Execute_AllCharactersAssigned()
    {
        var ctx = CreateContext();
        SpawnCharacter(ctx, "A", 40, rank: RankId.Soldier);
        SpawnCharacter(ctx, "B", 35, rank: RankId.Associate);
        SpawnCharacter(ctx, "C", 50, rank: RankId.Caporegime);

        new HouseholdEntityPhase().Execute(ctx);

        foreach (var entity in ctx.Roster.Values)
        {
            entity.Get<MemberOfHousehold>(Match.Entity).Length.Should().BeGreaterThan(0,
                "Character should be assigned to a household");
        }
    }

    [Fact]
    public void Execute_UnrelatedCharacters_SeparateHouseholds()
    {
        var ctx = CreateContext();
        var a = SpawnCharacter(ctx, "A", 40, rank: RankId.Soldier);
        var b = SpawnCharacter(ctx, "B", 35, rank: RankId.Associate);

        new HouseholdEntityPhase().Execute(ctx);

        var householdA = a.Get<MemberOfHousehold>(Match.Entity)[0].Target;
        var householdB = b.Get<MemberOfHousehold>(Match.Entity)[0].Target;
        householdA.Should().NotBe(householdB);
    }

    [Fact]
    public void Execute_HouseholdHasMarkerComponent()
    {
        var ctx = CreateContext();
        SpawnCharacter(ctx, "Vito", 50, rank: RankId.Boss);

        new HouseholdEntityPhase().Execute(ctx);

        var household = ctx.Roster.Values.First()
            .Get<MemberOfHousehold>(Match.Entity)[0].Target;
        household.Has<Household>().Should().BeTrue();
    }
}
