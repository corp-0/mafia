using Mafia.Core.Context;
using Mafia.Core.Events.Conditions.Interfaces;

namespace Mafia.Core.Events.Conditions;

public class AllOf(params IEventCondition[] conditions): IEventCondition
{
    public bool Evaluate(EntityScope context)
    {
        return conditions.All(c => c.Evaluate(context));
    }
}

public class AnyOf(params IEventCondition[] conditions) : IEventCondition
{
    public bool Evaluate(EntityScope context)
    {
        return conditions.Any(c => c.Evaluate(context));
    }
}

public class NoneOf(params IEventCondition[] conditions) : IEventCondition
{
    public bool Evaluate(EntityScope context)
    {
        return conditions.All(c => !c.Evaluate(context));
    }
}