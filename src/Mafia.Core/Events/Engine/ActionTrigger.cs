using Mafia.Core.Content.Registries;
using Mafia.Core.Context;
using Mafia.Core.Events.Definition;
using Mafia.Core.Time;

namespace Mafia.Core.Events.Engine;

public interface IActionTrigger
{
    void OnAction(string actionId, EntityScope scope);
}

public class ActionTrigger(IEventDefinitionRepository definitions) : IActionTrigger, IEventTriggerSource
{
    private readonly List<EventCandidate> _pending = [];

    public void OnAction(string actionId, EntityScope scope)
    {
        var eventDefs = definitions.GetByActionId(actionId);
        foreach (ActionEventDefinition def in eventDefs)
        {
            _pending.Add(new EventCandidate(def, scope));
        }
    }

    public IEnumerable<EventCandidate> GetCandidates(GameDate currentDate)
    {
        var candidates = _pending.ToList();
        _pending.Clear();
        return candidates;
    }
}
