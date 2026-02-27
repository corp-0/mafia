using Mafia.Core.Context;
using Mafia.Core.Events.Conditions.Interfaces;

namespace Mafia.Core.Events.Conditions;

public sealed class HasTagCondition<TTag>(string path) : IEventCondition
    where TTag : struct
{
    public bool Evaluate(EntityScope context) => context.HasTag<TTag>(path);
}