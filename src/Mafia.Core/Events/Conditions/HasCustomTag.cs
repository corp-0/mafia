using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Events.Conditions.Interfaces;

namespace Mafia.Core.Events.Conditions;

public sealed class HasCustomTag(string normalizedTag, string path) : IEventCondition
{
    public bool Evaluate(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return false;
        if (!entity.Has<CustomTags>()) return false;
        return entity.Ref<CustomTags>().Contains(normalizedTag);
    }
}
