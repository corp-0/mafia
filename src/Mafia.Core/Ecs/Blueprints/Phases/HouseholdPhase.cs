using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Relations;

namespace Mafia.Core.Ecs.Blueprints.Phases;

public class HouseholdPhase : IGenerationPhase
{
    public void Execute(WorldGenerationContext ctx)
    {
        // Process anchors in order of rank (Boss → Capo → Soldier → Associate)
        var anchors = ctx.Roster.Values.ToList();
        var sortedAnchors = anchors
            .Where(e => e.Has<Rank>())
            .OrderByDescending(e => e.Ref<Rank>().Id)
            .ToList();

        foreach (var anchor in sortedAnchors)
        {
            var age = anchor.Ref<Age>().Amount;

            // Age-scaled marriage probability
            var marriageProb = age >= ctx.Config.MinMarriageAge
                ? ctx.Config.MarriageRate * Math.Min(1.0, (age - ctx.Config.MinMarriageAge) / 20.0)
                : 0.0;

            if (!ctx.Rng.Chance(marriageProb)) continue;

            // Spawn spouse
            var surname = anchor.Has<Surname>() ? anchor.Ref<Surname>().Value : "";
            var spouseAge = ctx.StatRoller.RollSpouseAge(age);
            var spouseName = ctx.NameGen.GenerateUniqueName(Sex.Female, surname);
            var spouseBlueprint = ctx.StatRoller.RollCivilian(spouseName, surname, Sex.Female, spouseAge);
            var spouse = ctx.SpawnCharacter(spouseBlueprint);

            // Wire canonical SpouseOf
            anchor.TryAddRelation<SpouseOf>(spouse);
            spouse.TryAddRelation<SpouseOf>(anchor);

            // Roll children
            var childCount = ctx.Rng.Poisson(ctx.Config.ChildrenLambda);
            var youngestParentAge = Math.Min(age, spouseAge);

            for (var i = 0; i < childCount; i++)
            {
                var childAge = ctx.StatRoller.RollChildAge(youngestParentAge);
                if (childAge < 1) continue;

                var childSex = ctx.Rng.Chance(ctx.Config.MaleRatio) ? Sex.Male : Sex.Female;
                var childSurname = surname;
                var childName = ctx.NameGen.GenerateUniqueName(childSex, childSurname);
                var childBlueprint = ctx.StatRoller.RollCivilian(childName, childSurname, childSex, childAge);
                var child = ctx.SpawnCharacter(childBlueprint);

                // Wire canonical ParentOf
                anchor.TryAddRelation<ParentOf>(child);
                spouse.TryAddRelation<ParentOf>(child);
            }
        }
    }
}
