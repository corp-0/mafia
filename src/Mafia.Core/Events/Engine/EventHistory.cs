using fennecs;
using Mafia.Core.Time;

namespace Mafia.Core.Events.Engine;

public class EventHistory : IEventHistory
{
    private readonly Dictionary<string, Dictionary<Entity, GameDate>> _records = new();

    public void Record(string eventId, Entity rootEntity, GameDate date)
    {
        if (!_records.TryGetValue(eventId, out var entityMap))
        {
            entityMap = new Dictionary<Entity, GameDate>();
            _records[eventId] = entityMap;
        }
        entityMap[rootEntity] = date;
    }

    public bool HasFired(string eventId, Entity rootEntity)
    {
        return _records.TryGetValue(eventId, out var entityMap) && entityMap.ContainsKey(rootEntity);
    }

    public GameDate? LastFiredDate(string eventId, Entity rootEntity)
    {
        if (_records.TryGetValue(eventId, out var entityMap) &&
            entityMap.TryGetValue(rootEntity, out var date))
        {
            return date;
        }
        return null;
    }
}
