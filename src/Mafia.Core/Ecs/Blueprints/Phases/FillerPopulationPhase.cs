using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Relations;

namespace Mafia.Core.Ecs.Blueprints.Phases;

public class FillerPopulationPhase : IGenerationPhase
{
    public void Execute(WorldGenerationContext ctx)
    {
        while (ctx.PopulationCount < ctx.Config.TargetPopulation)
        {
            var remaining = ctx.Config.TargetPopulation - ctx.PopulationCount;
            if (remaining <= 0) break;

            // Create a civilian household head
            var sex = ctx.Rng.Chance(ctx.Config.MaleRatio) ? Sex.Male : Sex.Female;
            var surname = ctx.NameGen.PickSurname();
            var headName = ctx.NameGen.GenerateUniqueName(sex, surname);
            var headBlueprint = ctx.StatRoller.RollCivilian(headName, surname, sex);
            var head = ctx.SpawnCharacter(headBlueprint);

            if (ctx.PopulationCount >= ctx.Config.TargetPopulation) break;

            var headAge = head.Ref<Age>().Amount;

            // Maybe married
            if (headAge >= ctx.Config.MinMarriageAge && ctx.Rng.Chance(ctx.Config.MarriageRate))
            {
                var spouseSex = sex == Sex.Male ? Sex.Female : Sex.Male;
                var spouseAge = ctx.StatRoller.RollSpouseAge(headAge);
                var spouseSurname = sex == Sex.Male ? surname : ctx.NameGen.PickSurname();
                var spouseName = ctx.NameGen.GenerateUniqueName(spouseSex, spouseSurname);
                var spouseBlueprint = ctx.StatRoller.RollCivilian(spouseName, spouseSurname, spouseSex, spouseAge);
                var spouse = ctx.SpawnCharacter(spouseBlueprint);

                head.TryAddRelation<SpouseOf>(spouse);
                spouse.TryAddRelation<SpouseOf>(head);

                if (ctx.PopulationCount >= ctx.Config.TargetPopulation) break;

                // Children
                var childCount = ctx.Rng.Poisson(ctx.Config.ChildrenLambda);
                var youngestParentAge = Math.Min(headAge, spouseAge);

                for (var i = 0; i < childCount && ctx.PopulationCount < ctx.Config.TargetPopulation; i++)
                {
                    var childAge = ctx.StatRoller.RollChildAge(youngestParentAge);
                    if (childAge < 1) continue;

                    var childSex = ctx.Rng.Chance(ctx.Config.MaleRatio) ? Sex.Male : Sex.Female;
                    var childSurname = sex == Sex.Male ? surname : spouseSurname;
                    var childName = ctx.NameGen.GenerateUniqueName(childSex, childSurname);
                    var childBlueprint = ctx.StatRoller.RollCivilian(childName, childSurname, childSex, childAge);
                    var child = ctx.SpawnCharacter(childBlueprint);

                    head.TryAddRelation<ParentOf>(child);
                    spouse.TryAddRelation<ParentOf>(child);
                }
            }
        }
    }
}
