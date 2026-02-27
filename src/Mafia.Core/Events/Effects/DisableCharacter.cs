using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Events.Effects.Interfaces;

namespace Mafia.Core.Events.Effects;

public class DisableCharacter<T>(string path) : IEventEffect where T : struct, IDisableReason
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return;
        if (!entity.TryAddComponent<T>()) return;

        var disabled = entity.GetComponent<Disabled>();
        if (disabled is { } current)
            entity.Ref<Disabled>() = new Disabled(current.Count + 1);
        else
            entity.TryAddComponent(new Disabled(1));
    }
}
