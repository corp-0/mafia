using Mafia.Core.Context;

namespace Mafia.Core.Events.Conditions.Interfaces;

public interface IEventCondition
{
    bool Evaluate(EntityScope context);
}