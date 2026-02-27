using Mafia.Core.Events.Conditions.Interfaces;
using Mafia.Core.Events.Effects.Interfaces;

namespace Mafia.Core.Events.Definition;

/// <summary>
/// Base class for all event options.
/// </summary>
public abstract class EventOptionDefinition
{
    public required string Id { get; init; }
    public required string DisplayTextKey { get; init; }

    /// <summary>
    /// If set, this option is only visible when these conditions pass.
    /// Hidden options are invisible to both player and AI.
    /// </summary>
    public IEventCondition? VisibilityConditions { get; init; }

    public required AiWeight AiWeight { get; init; }
}

/// <summary>
/// The traditional deterministic option. Pick it, get the effects.
/// </summary>
public sealed class StandardOptionDefinition : EventOptionDefinition
{
    public required EventOutcome Outcome { get; init; }
}

/// <summary>
/// An option that rolls against a character's stat to determine the outcome.
/// </summary>
public sealed class SkillCheckOptionDefinition : EventOptionDefinition
{
    /// <summary>
    /// Dot-path to the entity whose stat is checked (e.g. "root").
    /// </summary>
    public required string StatPath { get; init; }

    /// <summary>
    /// Name of the stat to roll against (e.g. "nerve", "charm").
    /// </summary>
    public required string StatName { get; init; }

    /// <summary>
    /// The target number the 2d6 + stat roll must meet or exceed.
    /// </summary>
    public required int Difficulty { get; init; }

    public required EventOutcome Success { get; init; }
    public required EventOutcome Failure { get; init; }
}

/// <summary>
/// An option with multiple weighted outcomes resolved randomly.
/// </summary>
public sealed class RandomOptionDefinition : EventOptionDefinition
{
    public required IReadOnlyList<WeightedEventOutcome> Outcomes { get; init; }
}

/// <summary>
/// A bundle of text and effects for a specific outcome.
/// </summary>
public class EventOutcome
{
    public string? ResolutionTextKey { get; init; }
    public required IReadOnlyList<IEventEffect> Effects { get; init; }
}

/// <summary>
/// An outcome with a relative weight for random selection.
/// </summary>
public sealed class WeightedEventOutcome : EventOutcome
{
    public required int Weight { get; init; }
}
