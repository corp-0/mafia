using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Events.Effects.Interfaces;

namespace Mafia.Core.Events.Effects;

public class EnableCharacter<T>(string path) : IEventEffect where T : struct
{
    public void Apply(EntityScope context)
    {
        if (!context.RemoveComponent<T>(path)) return;

        var disabled = context.GetComponent<Disabled>(path);
        if (disabled is not { } current) return;

        if (current.Count <= 1)
            context.RemoveComponent<Disabled>(path);
        else
            context.SetComponent(path, new Disabled(current.Count - 1));
    }
}
