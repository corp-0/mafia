using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;

namespace Mafia.Core.Ecs.Blueprints.Phases;

public class HouseholdEntityPhase : IGenerationPhase
{
    public void Execute(WorldGenerationContext ctx)
    {
        var assigned = new HashSet<Entity>();

        // Sort characters by rank (desc) then age (desc)
        var characters = ctx.Roster.Values
            .Where(e => e.Has<Character>())
            .OrderByDescending(e => e.Has<Rank>() ? (int)e.Ref<Rank>().Id : -1)
            .ThenByDescending(e => e.Ref<Age>().Amount)
            .ToList();

        foreach (var character in characters)
        {
            if (assigned.Contains(character)) continue;

            var members = new List<Entity> { character };
            assigned.Add(character);

            // Gather spouse
            var spouses = character.Get<SpouseOf>(Match.Entity);
            for (var i = 0; i < spouses.Length; i++)
            {
                var spouse = spouses[i].Target;
                if (assigned.Add(spouse))
                    members.Add(spouse);
            }

            // Gather children (via ParentOf)
            var children = character.Get<ParentOf>(Match.Entity);
            for (var i = 0; i < children.Length; i++)
            {
                var child = children[i].Target;
                if (assigned.Add(child))
                    members.Add(child);
            }

            // Pick head: highest rank, tiebreak by age
            var head = members
                .OrderByDescending(e => e.Has<Rank>() ? (int)e.Ref<Rank>().Id : -1)
                .ThenByDescending(e => e.Ref<Age>().Amount)
                .First();

            // Spawn household entity
            var household = ctx.World.Spawn();
            household.Add<Household>();

            // Wire relations
            foreach (var member in members)
                member.TryAddRelation<MemberOfHousehold>(household);

            household.Add(new HeadOfHousehold(head), head);
        }
    }
}
