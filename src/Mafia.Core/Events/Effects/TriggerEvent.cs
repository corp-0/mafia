using Mafia.Core.Context;
using Mafia.Core.Events.Effects.Interfaces;

namespace Mafia.Core.Events.Effects;

public class TriggerEvent(string eventId, string? newRootPath = null) : IEventEffect
{
    public void Apply(EntityScope context)
    {
        if (newRootPath is null)
        {
            context.TriggerChainedEvent(eventId);
            return;
        }

        var root = context.Navigate(newRootPath);
        if (root is not { } entity) return;

        var newScope = new EntityScope(context.World) { EventHistory = context.EventHistory, CurrentDate = context.CurrentDate }
            .WithAnchor("root", entity);

        context.TriggerChainedEvent(eventId, newScope);
    }
}
