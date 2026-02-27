using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Events.Effects.Interfaces;

namespace Mafia.Core.Events.Effects;

public class DisableCharacter<T>(string path) : IEventEffect where T : struct
{
    public void Apply(EntityScope context)
    {
        if (!context.AddComponent<T>(path)) return;

        var disabled = context.GetComponent<Disabled>(path);
        if (disabled is { } current)
            context.SetComponent(path, new Disabled(current.Count + 1));
        else
            context.AddComponent(path, new Disabled(1));
    }
}
