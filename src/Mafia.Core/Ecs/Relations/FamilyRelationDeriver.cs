using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Identity;

namespace Mafia.Core.Ecs.Relations;

public static class FamilyRelationDeriver
{
    public static void DeriveAll(IEnumerable<Entity> entities)
    {
        var entityList = entities.ToList();

        // Pass 0: derive gendered relations from canonical ParentOf/SpouseOf
        foreach (var entity in entityList)
        {
            DeriveFromCanonicals(entity);
        }

        // Pass 1: full siblings from shared parents (via ParentOf)
        foreach (var entity in entityList)
        {
            DeriveFullSiblings(entity);
        }

        // Pass 2: half-siblings (must be wired before uncle/aunt/cousin derivation)
        foreach (var entity in entityList)
        {
            var parents = GetParents(entity);
            if (parents.Count > 0)
                DeriveHalfSiblings(entity, parents);
        }

        // Pass 3: extended relations (can now use full + half siblings)
        foreach (var entity in entityList)
        {
            var parents = GetParents(entity);
            if (parents.Count == 0) continue;

            DeriveGrandRelations(entity, parents);
            DeriveUncleAuntRelations(entity, parents);
            DeriveCousins(entity, parents);
        }
    }

    private static void DeriveFromCanonicals(Entity entity)
    {
        var sex = entity.Ref<Sex>();

        // SpouseOf → HusbandOf/WifeOf
        var spouses = RelationQueries.CollectTargets<SpouseOf>(entity);
        foreach (var spouse in spouses)
        {
            if (sex == Sex.Male)
                entity.TryAddRelation<HusbandOf>(spouse);
            else
                entity.TryAddRelation<WifeOf>(spouse);

            var spouseSex = spouse.Ref<Sex>();
            if (spouseSex == Sex.Male)
                spouse.TryAddRelation<HusbandOf>(entity);
            else
                spouse.TryAddRelation<WifeOf>(entity);
        }

        // ParentOf → FatherOf/MotherOf + SonOf/DaughterOf
        var children = RelationQueries.CollectTargets<ParentOf>(entity);
        foreach (var child in children)
        {
            if (sex == Sex.Male)
                entity.TryAddRelation<FatherOf>(child);
            else
                entity.TryAddRelation<MotherOf>(child);

            var childSex = child.Ref<Sex>();
            if (childSex == Sex.Male)
                child.TryAddRelation<SonOf>(entity);
            else
                child.TryAddRelation<DaughterOf>(entity);
        }
    }

    private static void DeriveFullSiblings(Entity entity)
    {
        var sex = entity.Ref<Sex>();

        // Find all parents of this entity (via ParentOf reverse lookup)
        var parents = GetParents(entity);
        if (parents.Count < 2) return;

        // Find children shared by both parents
        var parent1Children = new HashSet<Entity>(GetChildren(parents[0]));
        var parent2Children = new HashSet<Entity>(GetChildren(parents[1]));
        parent1Children.IntersectWith(parent2Children);

        foreach (var sibling in parent1Children)
        {
            if (sibling == entity) continue;

            if (sex == Sex.Male)
                entity.TryAddRelation<BrotherOf>(sibling);
            else
                entity.TryAddRelation<SisterOf>(sibling);

            var siblingSex = sibling.Ref<Sex>();
            if (siblingSex == Sex.Male)
                sibling.TryAddRelation<BrotherOf>(entity);
            else
                sibling.TryAddRelation<SisterOf>(entity);
        }
    }

    private static void DeriveHalfSiblings(Entity entity, List<Entity> parents)
    {
        var sex = entity.Ref<Sex>();

        foreach (var parent in parents)
        {
            foreach (var child in GetChildren(parent))
            {
                if (child == entity) continue;
                if (IsFullSibling(entity, child)) continue;

                if (sex == Sex.Male)
                    entity.TryAddRelation<HalfBrotherOf>(child);
                else
                    entity.TryAddRelation<HalfSisterOf>(child);

                var childGender = child.Ref<Sex>();
                if (childGender == Sex.Male)
                    child.TryAddRelation<HalfBrotherOf>(entity);
                else
                    child.TryAddRelation<HalfSisterOf>(entity);
            }
        }
    }

    private static void DeriveGrandRelations(Entity entity, List<Entity> parents)
    {
        var sex = entity.Ref<Sex>();

        foreach (var parent in parents)
        {
            foreach (var grandparent in GetParents(parent))
            {
                var gpSex = grandparent.Ref<Sex>();

                if (gpSex == Sex.Male)
                    grandparent.TryAddRelation<GrandfatherOf>(entity);
                else
                    grandparent.TryAddRelation<GrandmotherOf>(entity);

                if (sex == Sex.Male)
                    entity.TryAddRelation<GrandsonOf>(grandparent);
                else
                    entity.TryAddRelation<GranddaughterOf>(grandparent);
            }
        }
    }

    private static void DeriveUncleAuntRelations(Entity entity, List<Entity> parents)
    {
        var sex = entity.Ref<Sex>();

        foreach (var parent in parents)
        {
            foreach (var parentSibling in GetAllSiblings(parent))
            {
                var psSex = parentSibling.Ref<Sex>();

                if (psSex == Sex.Male)
                    parentSibling.TryAddRelation<UncleOf>(entity);
                else
                    parentSibling.TryAddRelation<AuntOf>(entity);

                if (sex == Sex.Male)
                    entity.TryAddRelation<NephewOf>(parentSibling);
                else
                    entity.TryAddRelation<NieceOf>(parentSibling);
            }
        }
    }

    private static void DeriveCousins(Entity entity, List<Entity> parents)
    {
        foreach (var parent in parents)
        {
            foreach (var parentSibling in GetAllSiblings(parent))
            {
                foreach (var cousin in GetChildren(parentSibling))
                {
                    if (cousin == entity) continue;

                    entity.TryAddRelation<CousinOf>(cousin);
                    cousin.TryAddRelation<CousinOf>(entity);
                }
            }
        }
    }

    private static bool IsFullSibling(Entity a, Entity b) =>
        a.Has<BrotherOf>(b) || a.Has<SisterOf>(b);

    private static List<Entity> GetParents(Entity entity)
    {
        // Query canonical ParentOf (reverse: who has ParentOf targeting this entity?)
        // plus legacy gendered relations for backward compat
        var parents = RelationQueries.CollectTargets<SonOf>(entity);
        parents.AddRange(RelationQueries.CollectTargets<DaughterOf>(entity));
        return parents;
    }

    private static List<Entity> GetChildren(Entity entity)
    {
        // Canonical: ParentOf targets
        var children = RelationQueries.CollectTargets<ParentOf>(entity);
        // Legacy gendered
        children.AddRange(RelationQueries.CollectTargets<FatherOf>(entity));
        children.AddRange(RelationQueries.CollectTargets<MotherOf>(entity));
        // Deduplicate
        return children.Distinct().ToList();
    }

    private static List<Entity> GetAllSiblings(Entity entity)
    {
        var siblings = RelationQueries.CollectTargets<BrotherOf>(entity);
        siblings.AddRange(RelationQueries.CollectTargets<SisterOf>(entity));
        siblings.AddRange(RelationQueries.CollectTargets<HalfBrotherOf>(entity));
        siblings.AddRange(RelationQueries.CollectTargets<HalfSisterOf>(entity));
        return siblings;
    }
}
