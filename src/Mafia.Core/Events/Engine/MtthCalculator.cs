using Mafia.Core.Context;
using Mafia.Core.Events.Definition;

namespace Mafia.Core.Events.Engine;

public class MtthCalculator
{
    private readonly Random _random;

    public MtthCalculator(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public double CalculateEffective(double baseMtthDays, IReadOnlyList<MtthModifier> modifiers, EntityScope scope)
    {
        var effective = baseMtthDays;

        foreach (var modifier in modifiers)
        {
            if (modifier.Condition.Evaluate(scope))
                effective *= modifier.Factor;
        }

        return Math.Max(1.0, effective);
    }

    public bool Roll(double effectiveMtthDays, double tickDurationDays)
    {
        // P = 1 - e^(-dt/mtth)
        var probability = 1.0 - Math.Exp(-tickDurationDays / effectiveMtthDays);
        return _random.NextDouble() < probability;
    }
}
