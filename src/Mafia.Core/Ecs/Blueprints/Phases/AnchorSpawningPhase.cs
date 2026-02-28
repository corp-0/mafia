using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Ecs.Relations;

namespace Mafia.Core.Ecs.Blueprints.Phases;

public class AnchorSpawningPhase : IGenerationPhase
{
    public void Execute(WorldGenerationContext ctx)
    {
        foreach (var org in ctx.Orgs)
        {
            var orgEntity = ctx.World.Spawn();
            orgEntity.Add(new OrgName($"{org.Surname} Family"));
            ctx.OrgEntities[org.Surname] = orgEntity;

            var bossEntity = SpawnOrgMember(ctx, org.Surname, org.Boss);
            ctx.OrgBossEntities[org.Surname] = bossEntity;
            bossEntity.TryAddRelation<MemberOf>(orgEntity);

            foreach (var capo in org.Capos)
            {
                var sharesSurname = true;
                var surname = sharesSurname ? org.Surname : ctx.NameGen.PickSurname();
                var capoEntity = SpawnOrgMember(ctx, surname, capo.Slot);

                // Hierarchy: capo reports to boss
                capoEntity.TryAddRelation<SubordinateOf>(bossEntity);
                bossEntity.TryAddRelation<BossOf>(capoEntity);
                capoEntity.TryAddRelation<MemberOf>(orgEntity);

                if (capo.IsUnderboss)
                    capoEntity.Add<Underboss>();

                foreach (var soldier in capo.Soldiers)
                {
                    var soldierSurname = ctx.Rng.Chance(ctx.Config.AssociateSharesSurnameProbability)
                        ? org.Surname
                        : ctx.NameGen.PickSurname();
                    var soldierEntity = SpawnOrgMember(ctx, soldierSurname, soldier.Slot);

                    // Hierarchy: soldier reports to capo
                    soldierEntity.TryAddRelation<SubordinateOf>(capoEntity);
                    capoEntity.TryAddRelation<BossOf>(soldierEntity);
                    soldierEntity.TryAddRelation<MemberOf>(orgEntity);

                    foreach (var associate in soldier.Associates)
                    {
                        var assocSurname = ctx.Rng.Chance(ctx.Config.AssociateSharesSurnameProbability)
                            ? org.Surname
                            : ctx.NameGen.PickSurname();
                        var assocEntity = SpawnOrgMember(ctx, assocSurname, associate);

                        // Hierarchy: associate reports to soldier
                        assocEntity.TryAddRelation<SubordinateOf>(soldierEntity);
                        soldierEntity.TryAddRelation<BossOf>(assocEntity);
                        assocEntity.TryAddRelation<MemberOf>(orgEntity);
                    }
                }
            }
        }
    }

    private static Entity SpawnOrgMember(WorldGenerationContext ctx, string surname, OrgSlot slot)
    {
        var name = ctx.NameGen.GenerateUniqueName(Components.Identity.Sex.Male, surname);
        var blueprint = ctx.StatRoller.RollOrgMember(name, surname, slot.Rank);
        return ctx.SpawnCharacter(blueprint);
    }
}
