using Mafia.Core.Context;
using Mafia.Core.Events.Definition;
using Mafia.Core.Time;

namespace Mafia.Core.Events.Engine;

public interface IEventTriggerSource
{
    IEnumerable<EventCandidate> GetCandidates(GameDate currentDate);
}

public class EventCandidate(EventDefinition definition, EntityScope scope)
{
    public EventDefinition Definition { get; } = definition;
    public EntityScope Scope { get; } = scope;
}
