using fennecs;
using Mafia.Core.Time;

namespace Mafia.Core.Events.Engine;

public interface IEventHistory
{
    void Record(string eventId, Entity rootEntity, GameDate date);
    bool HasFired(string eventId, Entity rootEntity);
    GameDate? LastFiredDate(string eventId, Entity rootEntity);
}
