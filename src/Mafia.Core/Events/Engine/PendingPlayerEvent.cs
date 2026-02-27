using Mafia.Core.Context;
using Mafia.Core.Events.Definition;

namespace Mafia.Core.Events.Engine;

public class PendingPlayerEvent(
    EventDefinition definition,
    EntityScope scope,
    IReadOnlyList<EventOptionDefinition> visibleOptions)
{
    public EventDefinition Definition { get; } = definition;
    public EntityScope Scope { get; } = scope;
    public IReadOnlyList<EventOptionDefinition> VisibleOptions { get; } = visibleOptions;
}
