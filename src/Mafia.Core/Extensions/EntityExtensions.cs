using fennecs;
using Mafia.Core.Ecs.Relations.Interfaces;

namespace Mafia.Core.Extensions;

public static class EntityExtensions
{
    public static T? GetComponent<T>(this Entity entity) where T : struct
    {
        if (!entity.Has<T>()) return null;
        return entity.Ref<T>();
    }

    public static bool TryAddComponent<T>(this Entity entity, T data = default) where T : struct
    {
        if (entity.Has<T>()) return false;
        entity.Add(data);
        return true;
    }

    public static bool TryRemoveComponent<T>(this Entity entity) where T : struct
    {
        if (!entity.Has<T>()) return false;
        entity.Remove<T>();
        return true;
    }

    public static bool ModifyComponent<T>(this Entity entity, Func<T, T> transform, bool addIfMissing = false) where T : struct
    {
        if (!entity.Has<T>())
        {
            if (addIfMissing)
                entity.TryAddComponent<T>();
            else return false;
        }
        
        entity.Ref<T>() = transform(entity.Ref<T>());
        return true;
    }

    public static bool TryAddRelation<T>(this Entity from, Entity to) where T : struct, IRelation
    {
        if (from.Has<T>(to)) return false;
        from.Add(new T { Target = to }, to);
        return true;
    }

    public static bool TryRemoveRelation<T>(this Entity from, Entity to) where T : struct, IRelation
    {
        if (!from.Has<T>(to)) return false;
        from.Remove<T>(to);
        return true;
    }
}
