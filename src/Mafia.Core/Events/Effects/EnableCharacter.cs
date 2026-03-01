using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Extensions;

namespace Mafia.Core.Events.Effects;

public class EnableCharacter<T>(string path) : IEventEffect where T : struct, IDisableReason
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return;
        if (!entity.TryRemoveComponent<T>()) return;

        var disabled = entity.GetComponent<Disabled>();
        if (disabled is not { } current) return;

        if (current.Count <= 1)
            entity.TryRemoveComponent<Disabled>();
        else
            entity.Ref<Disabled>() = new Disabled(current.Count - 1);
    }
}
