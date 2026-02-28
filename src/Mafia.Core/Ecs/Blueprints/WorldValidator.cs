using fennecs;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Relations;

namespace Mafia.Core.Ecs.Blueprints;

public static class WorldValidator
{
    public static List<string> Validate(WorldGenerationContext ctx)
    {
        var errors = new List<string>();

        // Population within 90% of target
        var minPop = (int)(ctx.Config.TargetPopulation * 0.9);
        if (ctx.PopulationCount < minPop)
            errors.Add($"Population {ctx.PopulationCount} below 90% of target {ctx.Config.TargetPopulation}");

        foreach (var (id, entity) in ctx.Roster)
        {
            // All entities have core identity components
            if (!entity.Has<CharacterName>())
                errors.Add($"Entity {id} missing CharacterName component");

            // No self-relations (SpouseOf)
            if (entity.Has<SpouseOf>(entity))
                errors.Add($"Entity {id} has SpouseOf self-relation");
            if (entity.Has<ParentOf>(entity))
                errors.Add($"Entity {id} has ParentOf self-relation");
        }

        // SpouseOf bidirectional check
        foreach (var (id, entity) in ctx.Roster)
        {
            var spouses = RelationQueries.CollectTargets<SpouseOf>(entity);
            foreach (var spouse in spouses)
            {
                if (!spouse.Has<SpouseOf>(entity))
                    errors.Add($"Entity {id} has SpouseOf but it's not bidirectional");
            }
        }

        // Each org has exactly one Boss and a dedicated org entity
        foreach (var org in ctx.Orgs)
        {
            if (!ctx.OrgBossEntities.ContainsKey(org.Surname))
                errors.Add($"Org {org.Surname} missing boss entity");
            else
            {
                var boss = ctx.OrgBossEntities[org.Surname];
                if (!boss.Has<Rank>() || boss.Ref<Rank>().Id != RankId.Boss)
                    errors.Add($"Org {org.Surname} boss entity doesn't have Boss rank");
            }

            if (!ctx.OrgEntities.ContainsKey(org.Surname))
                errors.Add($"Org {org.Surname} missing dedicated org entity");
            else
            {
                var orgEntity = ctx.OrgEntities[org.Surname];
                if (!orgEntity.Has<OrgName>())
                    errors.Add($"Org {org.Surname} org entity missing OrgName component");
            }
        }

        return errors;
    }
}
