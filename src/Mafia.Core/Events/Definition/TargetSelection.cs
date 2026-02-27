using Mafia.Core.Events.Conditions.Interfaces;

namespace Mafia.Core.Events.Definition;

/// <summary>
/// Defines how the trigger source should find the "target" entity
/// for character-scoped events.
/// </summary>
public sealed class TargetSelection
{
    /// <summary>
    /// Where to look for targets.
    /// "root_subordinates" = subordinates of root
    /// "root_family"       = members of root's family
    /// "same_territory"    = characters in root's territory
    /// </summary>
    public required string Pool { get; init; }

    /// <summary>
    /// Filter conditions on the target.
    /// </summary>
    public IEventCondition? Filter { get; init; }

    /// <summary>
    /// How to pick from valid targets.
    /// "random" = pick one at random
    /// "highest_stat:respect" = pick the one with highest respect
    /// </summary>
    public string SelectionMode { get; init; } = "random";
}
