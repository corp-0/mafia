using Mafia.Core.Context;
using Mafia.Core.Events.Conditions.Interfaces;
using Mafia.Core.Time;

namespace Mafia.Core.Events.Conditions;

public sealed class DateRange(GameDate earliest, GameDate latest) : IEventCondition
{
    public bool Evaluate(EntityScope context) =>
        context.CurrentDate >= earliest && context.CurrentDate <= latest;
}