using Mafia.Core.Events.Definition;

namespace Mafia.Core.Content.Registries;

public sealed class EventDefinitionRepository : IEventDefinitionRepository
{
    private readonly Dictionary<string, EventDefinition> _byId = new();
    private readonly List<PulseEventDefinition> _pulseEvents = [];
    private readonly Dictionary<string, List<ActionEventDefinition>> _byActionId = new();
    private readonly List<StoryBeatEventDefinition> _storyBeatEvents = [];

    public void Register(EventDefinition definition)
    {
        if (_byId.TryGetValue(definition.Id, out var existing))
            RemoveFromIndexes(existing);

        _byId[definition.Id] = definition;
        AddToIndexes(definition);
    }

    public IReadOnlyList<T> GetAll<T>() where T : EventDefinition
    {
        if (typeof(T) == typeof(PulseEventDefinition))
            return (IReadOnlyList<T>)_pulseEvents;
        if (typeof(T) == typeof(StoryBeatEventDefinition))
            return (IReadOnlyList<T>)_storyBeatEvents;
        if (typeof(T) == typeof(ActionEventDefinition))
            return (IReadOnlyList<T>)_byActionId.Values.SelectMany(v => v).ToList();

        return _byId.Values.OfType<T>().ToList();
    }

    public IReadOnlyList<ActionEventDefinition> GetByActionId(string actionId) =>
        _byActionId.TryGetValue(actionId, out var list) ? list : [];

    public EventDefinition? GetById(string id) =>
        _byId.GetValueOrDefault(id);

    private void AddToIndexes(EventDefinition definition)
    {
        switch (definition)
        {
            case PulseEventDefinition pulse:
                _pulseEvents.Add(pulse);
                break;
            case ActionEventDefinition action:
                if (!_byActionId.TryGetValue(action.OnActionId, out var list))
                {
                    list = [];
                    _byActionId[action.OnActionId] = list;
                }
                list.Add(action);
                break;
            case StoryBeatEventDefinition story:
                _storyBeatEvents.Add(story);
                break;
        }
    }

    private void RemoveFromIndexes(EventDefinition definition)
    {
        switch (definition)
        {
            case PulseEventDefinition pulse:
                _pulseEvents.Remove(pulse);
                break;
            case ActionEventDefinition action:
                if (_byActionId.TryGetValue(action.OnActionId, out var list))
                {
                    list.Remove(action);
                    if (list.Count == 0)
                        _byActionId.Remove(action.OnActionId);
                }
                break;
            case StoryBeatEventDefinition story:
                _storyBeatEvents.Remove(story);
                break;
        }
    }
}
