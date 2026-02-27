using Mafia.Core.Events.Conditions.Interfaces;

namespace Mafia.Core.Events.Definition;

/// <summary>
/// Conditionally multiplies MTTH.
/// Factor &lt; 1.0 = faster, Factor &gt; 1.0 = slower.
/// </summary>
public sealed class MtthModifier
{
    public required IEventCondition Condition { get; init; }
    public required double Factor { get; init; }
}
