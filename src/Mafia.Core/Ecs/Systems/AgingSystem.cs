using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Events.Engine;
using Mafia.Core.Time;

namespace Mafia.Core.Ecs.Systems;

public class AgingSystem(World world): ITickSystem
{
    public void Tick(GameDate currentDate, IActionTrigger actionTrigger)
    {
        var stream = world.Query<Age, BirthDay>().Stream();
        stream.For((in Entity entity, ref Age age, ref BirthDay birthDay) =>
        {
            if (currentDate.Month == birthDay.Month && currentDate.Day == birthDay.Day)
            {
                age.Amount += 1;
                actionTrigger.OnAction("birthday", new EntityScope(world)
                    .WithAnchor("root", entity));
            }
        });
    }
}