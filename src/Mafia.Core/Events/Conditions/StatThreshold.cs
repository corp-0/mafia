using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Interfaces;
using Mafia.Core.Events.Conditions.Interfaces;
using Mafia.Core.Extensions;

namespace Mafia.Core.Events.Conditions;

public sealed class StatThreshold<TStat>(string path, Comparison comparison, int value) : IEventCondition
    where TStat : struct, IStatComponent
{
    public bool Evaluate(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return false;
        var stat = entity.GetComponent<TStat>();

        if (stat is null) return false;

        return comparison switch
        {
            Comparison.GreaterThan => stat.Value.Amount > value,
            Comparison.LessThan => stat.Value.Amount < value,
            Comparison.GreaterThanOrEqualTo => stat.Value.Amount >= value,
            Comparison.LessThanOrEqualTo => stat.Value.Amount <= value,
            Comparison.Equal => stat.Value.Amount == value,
            _ => false
        };
    }
}