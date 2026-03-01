using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Extensions;

namespace Mafia.Core.WorldGen.Phases;

public class NepotismPhase : IGenerationPhase
{
    public void Execute(WorldGenerationContext ctx)
    {
        // Find org members who have sons (via ParentOf)
        var orgMembers = ctx.Roster.Values
            .Where(e => e.Has<Rank>() && e.Ref<Rank>().Id >= RankId.Soldier)
            .Where(e => e.Has<MemberOf>())
            .ToList();

        foreach (var father in orgMembers)
        {
            var children = RelationQueries.CollectTargets<ParentOf>(father);
            var sons = children
                .Where(c => c.Has<Sex>() && c.Ref<Sex>() == Sex.Male)
                .Where(c => c.Has<Age>() && c.Ref<Age>().Amount >= ctx.Config.MinAdultAge)
                .Where(c => !c.Has<MemberOf>()) // Not already in an org
                .ToList();

            foreach (var son in sons)
            {
                if (!ctx.Rng.Chance(ctx.Config.SonJoinsFatherOrgProbability)) continue;

                // Find org entity via father's MemberOf
                var orgTargets = RelationQueries.CollectTargets<MemberOf>(father);
                if (orgTargets.Count == 0) continue;
                var orgEntity = orgTargets[0];

                // Determine rank
                var rank = ctx.Rng.Chance(ctx.Config.RankInheritanceBoost)
                    ? RankId.Soldier
                    : RankId.Associate;

                // Give the son a rank
                if (son.Has<Rank>())
                    son.Ref<Rank>() = new Rank(rank);
                else
                    son.Add(new Rank(rank));

                // Wire org relations
                son.TryAddRelation<MemberOf>(orgEntity);
                son.TryAddRelation<SubordinateOf>(father);
                father.TryAddRelation<BossOf>(son);
            }
        }
    }
}
