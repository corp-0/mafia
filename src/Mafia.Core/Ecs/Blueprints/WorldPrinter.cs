using fennecs;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Ecs.Relations;

namespace Mafia.Core.Ecs.Blueprints;

public static class WorldPrinter
{
    public static void Print(Dictionary<string, Entity> roster, Action<string>? log = null)
    {
        log ??= Console.WriteLine;

        // Identify org bosses
        var bosses = roster.Values
            .Where(e => e.Has<Rank>() && e.Ref<Rank>().Id == RankId.Boss)
            .ToList();

        foreach (var boss in bosses)
        {
            var surname = boss.Has<Surname>() ? boss.Ref<Surname>().Value : "???";
            log($"╔══════════════════════════════════════════════════╗");
            log($"║  {surname} Family");
            log($"╚══════════════════════════════════════════════════╝");
            log("");

            // Print org tree
            PrintOrgTree(boss, log);
            log("");

            // Print full character cards for boss + capos
            var capos = RelationQueries.CollectTargets<BossOf>(boss)
                .Where(e => e.Has<Rank>() && e.Ref<Rank>().Id == RankId.Caporegime)
                .ToList();

            PrintCharacterCard(boss, log);
            foreach (var capo in capos)
                PrintCharacterCard(capo, log);

            log("");
        }

        // Civilian summary
        var civilians = roster.Values.Where(e => !e.Has<Rank>() && !HasMemberOf(e)).ToList();
        var civFamilyHeads = civilians.Where(e =>
            RelationQueries.CollectTargets<SpouseOf>(e).Count > 0 ||
            RelationQueries.CollectTargets<ParentOf>(e).Count > 0).ToList();

        log($"── Civilians: {civilians.Count} total, {civFamilyHeads.Count} family heads ──");
        foreach (var head in civFamilyHeads.Take(5))
        {
            var hi = head.Ref<CharacterName>();
            var spouses = RelationQueries.CollectTargets<SpouseOf>(head);
            var children = RelationQueries.CollectTargets<ParentOf>(head);
            var spouseStr = spouses.Count > 0 ? $" + {spouses[0].Ref<CharacterName>().Name}" : "";
            log($"  {hi.Name}{spouseStr}, {children.Count} children");
        }
        if (civFamilyHeads.Count > 5)
            log($"  ... and {civFamilyHeads.Count - 5} more families");

        log("");
        log($"=== Total: {roster.Count} characters ===");
    }

    private static void PrintOrgTree(Entity boss, Action<string> log)
    {
        var bi = boss.Ref<CharacterName>();
        log($"  {bi.Name} [Boss]");

        var capos = RelationQueries.CollectTargets<BossOf>(boss)
            .Where(e => e.Has<Rank>() && e.Ref<Rank>().Id == RankId.Caporegime)
            .ToList();

        for (var ci = 0; ci < capos.Count; ci++)
        {
            var capo = capos[ci];
            var isLastCapo = ci == capos.Count - 1;
            var capoId = capo.Ref<CharacterName>();
            var ubTag = capo.Has<Underboss>() ? " [Underboss]" : " [Capo]";
            var branch = isLastCapo ? "└── " : "├── ";
            var cont = isLastCapo ? "    " : "│   ";

            log($"  {branch}{capoId.Name}{ubTag}");

            var soldiers = RelationQueries.CollectTargets<BossOf>(capo)
                .Where(e => e.Has<Rank>() && e.Ref<Rank>().Id == RankId.Soldier)
                .ToList();

            for (var si = 0; si < soldiers.Count; si++)
            {
                var soldier = soldiers[si];
                var isLastSoldier = si == soldiers.Count - 1;
                var soldierName = soldier.Ref<CharacterName>().Name;
                var sBranch = isLastSoldier ? "└── " : "├── ";
                var sCont = isLastSoldier ? "    " : "│   ";

                var associates = RelationQueries.CollectTargets<BossOf>(soldier)
                    .Where(e => e.Has<Rank>() && e.Ref<Rank>().Id == RankId.Associate)
                    .ToList();

                if (associates.Count == 0)
                {
                    log($"  {cont}{sBranch}{soldierName} [Soldier]");
                }
                else
                {
                    log($"  {cont}{sBranch}{soldierName} [Soldier]");
                    for (var ai = 0; ai < associates.Count; ai++)
                    {
                        var assoc = associates[ai];
                        var isLastAssoc = ai == associates.Count - 1;
                        var aBranch = isLastAssoc ? "└── " : "├── ";
                        log($"  {cont}{sCont}{aBranch}{assoc.Ref<CharacterName>().Name} [Orbiter]");
                    }
                }
            }
        }
    }

    private static void PrintCharacterCard(Entity entity, Action<string> log)
    {
        var cn = entity.Ref<CharacterName>();
        var nick = string.IsNullOrEmpty(cn.NickName) ? "" : $" \"{cn.NickName}\"";
        var rankStr = entity.Has<Rank>() ? entity.Ref<Rank>().Id.ToString() : "Civilian";
        var ub = entity.Has<Underboss>() ? " [Underboss]"
            : entity.Has<Rank>() && entity.Ref<Rank>().Id == RankId.Caporegime ? " [Capo]"
            : "";

        log($"  [{cn.Name}{nick}]");
        log($"    Age: {entity.Ref<Age>().Amount} | Sex: {entity.Ref<Sex>()} | Rank: {rankStr}{ub}");
        log($"    Muscle:{entity.Ref<Muscle>().Amount} Nerve:{entity.Ref<Nerve>().Amount} Brains:{entity.Ref<Brains>().Amount} Charm:{entity.Ref<Charm>().Amount} Instinct:{entity.Ref<Instinct>().Amount} | Wealth: ${entity.Ref<Wealth>().Amount}");
        
        PrintRelation<HusbandOf>(entity, "Husband of", log);
        PrintRelation<WifeOf>(entity, "Wife of", log);
        PrintRelation<FatherOf>(entity, "Father of", log);
        PrintRelation<MotherOf>(entity, "Mother of", log);
        PrintRelation<SonOf>(entity, "Son of", log);
        PrintRelation<DaughterOf>(entity, "Daughter of", log);
        PrintRelation<BrotherOf>(entity, "Brother of", log);
        PrintRelation<SisterOf>(entity, "Sister of", log);
        PrintRelation<HalfBrotherOf>(entity, "Half-brother of", log);
        PrintRelation<HalfSisterOf>(entity, "Half-sister of", log);
        PrintRelation<GrandfatherOf>(entity, "Grandfather of", log);
        PrintRelation<GrandmotherOf>(entity, "Grandmother of", log);
        PrintRelation<GrandsonOf>(entity, "Grandson of", log);
        PrintRelation<GranddaughterOf>(entity, "Granddaughter of", log);
        PrintRelation<UncleOf>(entity, "Uncle of", log);
        PrintRelation<AuntOf>(entity, "Aunt of", log);
        PrintRelation<NephewOf>(entity, "Nephew of", log);
        PrintRelation<NieceOf>(entity, "Niece of", log);
        PrintRelation<CousinOf>(entity, "Cousin of", log);
        PrintRelation<SubordinateOf>(entity, "Subordinate of", log);
        PrintRelation<BossOf>(entity, "Boss of", log);
        PrintRelation<MemberOf>(entity, "Member of", log);
    }

    private static void PrintRelation<TRelation>(Entity entity, string label, Action<string> log)
        where TRelation : struct, Relations.Interfaces.IRelation
    {
        var targets = RelationQueries.CollectTargets<TRelation>(entity);
        if (targets.Count == 0) return;

        var names = targets.Select(t =>
            t.Has<CharacterName>() ? t.Ref<CharacterName>().Name :
            t.Has<OrgName>() ? t.Ref<OrgName>().Value : "???");
        log($"    {label}: {string.Join(", ", names)}");
    }

    private static bool HasMemberOf(Entity e) => RelationQueries.CollectTargets<MemberOf>(e).Count > 0;
}
