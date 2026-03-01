using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Extensions;

namespace Mafia.Core.WorldGen;

public class HouseholdFactory
{
    public static void Apply(HouseholdBlueprint blueprint, IReadOnlyDictionary<string, Entity> roster)
    {
        ValidateIds(blueprint, roster);

        foreach (var marriage in blueprint.Marriages)
            ApplyMarriage(marriage, roster);

        foreach (var parentage in blueprint.Parentages)
            ApplyParentage(parentage, roster);
    }

    private static void ValidateIds(HouseholdBlueprint blueprint, IReadOnlyDictionary<string, Entity> roster)
    {
        var allReferencedIds = new HashSet<string>(blueprint.MemberIds);

        foreach (var marriage in blueprint.Marriages)
        {
            allReferencedIds.Add(marriage.Spouse1Id);
            allReferencedIds.Add(marriage.Spouse2Id);
        }

        foreach (var parentage in blueprint.Parentages)
        {
            allReferencedIds.Add(parentage.Parent1Id);
            allReferencedIds.Add(parentage.Parent2Id);
            foreach (var childId in parentage.ChildrenIds)
                allReferencedIds.Add(childId);
        }

        var missing = allReferencedIds.Where(id => !roster.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            throw new KeyNotFoundException(
                $"Household '{blueprint.Id}' references unknown IDs: {string.Join(", ", missing)}");
    }

    private static void ApplyMarriage(Marriage marriage, IReadOnlyDictionary<string, Entity> roster)
    {
        var spouse1 = roster[marriage.Spouse1Id];
        var spouse2 = roster[marriage.Spouse2Id];

        spouse1.TryAddRelation<SpouseOf>(spouse2);
        spouse2.TryAddRelation<SpouseOf>(spouse1);
    }

    private static void ApplyParentage(Parentage parentage, IReadOnlyDictionary<string, Entity> roster)
    {
        var parent1 = roster[parentage.Parent1Id];
        var parent2 = roster[parentage.Parent2Id];

        foreach (var childId in parentage.ChildrenIds)
        {
            var child = roster[childId];

            parent1.TryAddRelation<ParentOf>(child);
            parent2.TryAddRelation<ParentOf>(child);
        }
    }
}
