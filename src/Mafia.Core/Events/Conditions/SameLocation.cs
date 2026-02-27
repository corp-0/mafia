using Mafia.Core.Context;
using Mafia.Core.Events.Conditions.Interfaces;

namespace Mafia.Core.Events.Conditions;

public sealed class SameLocation(string pathA, string pathB) : IEventCondition
{
    public bool Evaluate(EntityScope context)
    {
        var locationA = context.Navigate($"{pathA}.location");
        var locationB = context.Navigate($"{pathB}.location");
        if (locationA is null || locationB is null) return false;
        return locationA.Value == locationB.Value;
    }
}