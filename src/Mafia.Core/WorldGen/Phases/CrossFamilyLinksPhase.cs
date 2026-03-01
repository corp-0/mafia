using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Extensions;

namespace Mafia.Core.WorldGen.Phases;

public class CrossFamilyLinksPhase : IGenerationPhase
{
    public void Execute(WorldGenerationContext ctx)
    {
        // Find eligible org members for cross-org marriages
        var orgMembers = ctx.Roster.Values
            .Where(e => e.Has<Rank>() && e.Ref<Rank>().Id >= RankId.Soldier)
            .Where(e => !HasSpouse(e))
            .Where(e => e.Ref<Age>().Amount >= ctx.Config.MinMarriageAge)
            .ToList();

        foreach (var member in orgMembers)
        {
            if (!ctx.Rng.Chance(ctx.Config.CrossOrgMarriageProbability)) continue;

            var age = member.Ref<Age>().Amount;
            var surname = member.Has<Surname>() ? member.Ref<Surname>().Value : ctx.NameGen.PickSurname();
            var spouseAge = ctx.StatRoller.RollSpouseAge(age);
            var spouseName = ctx.NameGen.GenerateUniqueName(Sex.Female, surname);
            var spouseBlueprint = ctx.StatRoller.RollCivilian(spouseName, surname, Sex.Female, spouseAge);
            var spouse = ctx.SpawnCharacter(spouseBlueprint);

            member.TryAddRelation<SpouseOf>(spouse);
            spouse.TryAddRelation<SpouseOf>(member);
        }
    }

    private static bool HasSpouse(Entity entity) =>
        RelationQueries.CollectTargets<SpouseOf>(entity).Count > 0;
}
