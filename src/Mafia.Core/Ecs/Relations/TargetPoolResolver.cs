using fennecs;
using Mafia.Core.Ecs.Components.Rank;

namespace Mafia.Core.Ecs.Relations;

public class TargetPoolResolver
{
    public List<Entity>? Resolve(string poolName, Entity root)
    {
        return poolName switch
        {
            "root_subordinates" => RelationQueries.CollectTargets<BossOf>(root),
            "root_family" => RelationQueries.CollectFamilyTargets(root),
            "root_creditors" => RelationQueries.CollectTargets<Owes>(root),
            "root_crew" => CollectCrew(root),
            "root_crime_family" => CollectCrimeFamily(root),
            _ => null
        };
    }

    private static List<Entity> CollectCrew(Entity root)
    {
        // Find the capo — either root IS the capo, or climb until we reach one
        var capo = FindCrewLeader(root);
        if (capo is null)
            return [];

        // Crew = everyone under the capo (the whole subtree), excluding root
        var crew = new List<Entity>();
        if (capo.Value != root)
            crew.Add(capo.Value);
        CollectSubordinatesRecursive(capo.Value, crew);
        crew.Remove(root);
        return crew;
    }

    private static Entity? FindCrewLeader(Entity root)
    {
        if (root.Has<Rank>() && root.Ref<Rank>().Id >= RankId.Caporegime)
            return root;

        var current = root;
        while (true)
        {
            var bossRels = current.Get<SubordinateOf>(Match.Entity);
            if (bossRels.Length == 0)
                return null;

            current = bossRels[0].Target;

            if (current.Has<Rank>() && current.Ref<Rank>().Id >= RankId.Caporegime)
                return current;
        }
    }

    private static List<Entity> CollectCrimeFamily(Entity root)
    {
        // Climb SubordinateOf chain to the top (the Boss)
        var current = root;
        while (true)
        {
            var bossRels = current.Get<SubordinateOf>(Match.Entity);
            if (bossRels.Length == 0)
                break;
            current = bossRels[0].Target;
        }

        // Walk down from Boss, collecting the entire org tree
        var family = new List<Entity> { current };
        CollectSubordinatesRecursive(current, family);

        // Only made members (Soldier+)
        family.RemoveAll(e => !e.Has<Rank>() || e.Ref<Rank>().Id < RankId.Soldier);

        return family;
    }

    private static void CollectSubordinatesRecursive(Entity boss, List<Entity> results)
    {
        var subs = RelationQueries.CollectTargets<BossOf>(boss);
        foreach (var sub in subs)
        {
            results.Add(sub);
            CollectSubordinatesRecursive(sub, results);
        }
    }
}
