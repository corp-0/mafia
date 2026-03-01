using fennecs;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Relations;

namespace Mafia.Core.WorldGen;

public static class WorldValidator
{
    public static List<string> Validate(WorldGenerationContext ctx)
    {
        var errors = new List<string>();

        ValidateMinimumPopulation(ctx, errors);

        foreach ((var id, Entity entity) in ctx.Roster)
        {
            ValidateCoreComponents(entity, errors, id);
            CheckSelfRelations(entity, errors, id);
        }

        CheckSpouseBidirectionality(ctx, errors);
        foreach (OrgSkeleton org in ctx.Orgs)
        {
            EnsureBossIsValid(ctx, errors, org);
            EnsureOrgIsValid(ctx, errors, org);
        }
        
        return errors;
    }
    
    private static void EnsureOrgIsValid(WorldGenerationContext ctx, List<string> errors, OrgSkeleton org)
    {
        if (!ctx.OrgEntities.TryGetValue(org.Surname, out Entity orgEntity))
            errors.Add($"Org {org.Surname} missing dedicated org entity");
        else
        {
            if (!orgEntity.Has<OrgName>())
                errors.Add($"Org {org.Surname} org entity missing OrgName component");
        }
    }

    private static void EnsureBossIsValid(WorldGenerationContext ctx, List<string> errors, OrgSkeleton org)
    {
        if (!ctx.OrgBossEntities.TryGetValue(org.Surname, out Entity boss))
            errors.Add($"Org {org.Surname} missing boss entity");
        else
        {
            if (!boss.Has<Rank>() || boss.Ref<Rank>().Id != RankId.Boss)
                errors.Add($"Org {org.Surname} boss entity doesn't have Boss rank");
        }
    }

    private static void CheckSpouseBidirectionality(WorldGenerationContext ctx, List<string> errors)
    {
        foreach ((var id, Entity entity) in ctx.Roster)
        {
            var spouses = RelationQueries.CollectTargets<SpouseOf>(entity);
            foreach (Entity spouse in spouses)
            {
                if (!spouse.Has<SpouseOf>(entity))
                    errors.Add($"Entity {id} has SpouseOf but it's not bidirectional");
            }
        }
    }

    private static void CheckSelfRelations(Entity entity, List<string> errors, string id)
    {
        if (entity.Has<SpouseOf>(entity))
            errors.Add($"Entity {id} has SpouseOf self-relation");
        if (entity.Has<ParentOf>(entity))
            errors.Add($"Entity {id} has ParentOf self-relation");
    }

    private static void ValidateCoreComponents(Entity entity, List<string> errors, string id)
    {
        if (!entity.Has<CharacterName>())
            errors.Add($"Entity {id} missing CharacterName component");
    }

    private static void ValidateMinimumPopulation(WorldGenerationContext ctx, List<string> errors)
    {
        var minPop = (int)(ctx.Config.TargetPopulation * 0.9);
        if (ctx.PopulationCount < minPop)
            errors.Add($"Population {ctx.PopulationCount} below 90% of target {ctx.Config.TargetPopulation}");
    }
}
