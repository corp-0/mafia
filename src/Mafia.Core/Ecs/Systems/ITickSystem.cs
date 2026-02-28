using Mafia.Core.Events.Engine;
using Mafia.Core.Time;

namespace Mafia.Core.Ecs.Systems;

public interface ITickSystem
{
    void Tick(GameDate currentDate, IActionTrigger actionTrigger);
}
