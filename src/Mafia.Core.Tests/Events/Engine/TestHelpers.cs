using Mafia.Core.Context;
using Mafia.Core.Events.Conditions.Interfaces;
using Mafia.Core.Events.Engine;
using Mafia.Core.Time;

namespace Mafia.Core.Tests.Events.Engine;

internal sealed class AlwaysTrueCondition : IEventCondition
{
    public bool Evaluate(EntityScope context) => true;
}

internal sealed class AlwaysFalseCondition : IEventCondition
{
    public bool Evaluate(EntityScope context) => false;
}

/// <summary>
/// A Random subclass that returns a fixed value for NextDouble().
/// For Next(min, max), returns min (lowest possible value).
/// </summary>
internal sealed class FixedRandom(double fixedValue) : Random
{
    public override double NextDouble() => fixedValue;

    public override int Next(int minValue, int maxValue) => minValue;

    public override int Next(int maxValue) => 0;
}

/// <summary>
/// A simple trigger source backed by a list of candidates.
/// </summary>
internal sealed class ListTriggerSource : IEventTriggerSource
{
    private readonly List<EventCandidate> _candidates = [];

    public void Add(EventCandidate candidate) => _candidates.Add(candidate);

    public IEnumerable<EventCandidate> GetCandidates(GameDate currentDate)
    {
        var result = _candidates.ToList();
        _candidates.Clear();
        return result;
    }
}

internal sealed class NullActionTrigger : IActionTrigger
{
    public void OnAction(string actionId, EntityScope scope) { }
}

internal sealed class RecordingActionTrigger : IActionTrigger
{
    public List<(string ActionId, EntityScope Scope)> Invocations { get; } = [];

    public void OnAction(string actionId, EntityScope scope) =>
        Invocations.Add((actionId, scope));
}
