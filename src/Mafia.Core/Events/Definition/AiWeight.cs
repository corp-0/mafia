using Mafia.Core.Context;
using Mafia.Core.Events.Conditions.Interfaces;

namespace Mafia.Core.Events.Definition;

/// <summary>
/// AI weight for an event option. Base weight plus conditional modifiers.
/// </summary>
public sealed class AiWeight
{
    public required int BaseWeight { get; init; }
    public IReadOnlyList<AiWeightModifier> Modifiers { get; init; } = [];

    public int Calculate(EntityScope scope)
    {
        var weight = BaseWeight;
        foreach (var mod in Modifiers)
        {
            if (mod.Condition.Evaluate(scope))
                weight += mod.Add;
        }

        return Math.Max(0, weight);
    }
}

public sealed class AiWeightModifier
{
    public required IEventCondition Condition { get; init; }
    public required int Add { get; init; }
}
