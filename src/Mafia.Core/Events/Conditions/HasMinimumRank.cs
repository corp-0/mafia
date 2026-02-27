using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Events.Conditions.Interfaces;

namespace Mafia.Core.Events.Conditions;

public sealed class HasMinimumRank(string path, RankId minimum) : IEventCondition
{
    public bool Evaluate(EntityScope context)
    {
        var rank = context.GetRank(path);
        return rank is not null && rank.Value >= minimum;
    }
}
