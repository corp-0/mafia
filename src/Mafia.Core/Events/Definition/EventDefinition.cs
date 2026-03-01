using Mafia.Core.Context;
using Mafia.Core.Events.Conditions.Interfaces;
using Mafia.Core.Time;

namespace Mafia.Core.Events.Definition;

public enum ScopeType
{
    Character,
    Location,
    Relationship,
    Global
}

public enum TriggerType
{
    Chained,
    Pulse,
    OnAction,
    StoryBeat,
}

public enum OptionType
{
    Standard,
    SkillCheck,
    Random,
}

public enum EventPresentation
{
    /// <summary>Pauses the simulation and forces the player to respond.</summary>
    Popup,
    /// <summary>Appears in a feed. Player can respond at their leisure.</summary>
    Notification
}

/// <summary>
/// Base class for all event definitions. Contains fields that
/// every event needs regardless of the trigger type.
/// </summary>
public abstract class EventDefinition
{
    public required string Id { get; init; }
    public required string TitleKey { get; init; }
    public required string DescriptionKey { get; init; }
    public ScopeType Scope { get; init; }
    
    /// <summary>
    /// Condition tree. All conditions must pass for the event to fire.
    /// Null means always valid.
    /// </summary>
    public IEventCondition? Conditions { get; init; }
    
    /// <summary>
    /// The options the player or AI can choose from.
    /// </summary>
    public required IReadOnlyList<EventOptionDefinition> Options { get; init; }
    
    /// <summary>
    /// If true, fires only once per root character ever.
    /// </summary>
    public bool IsOneTimeOnly { get; init; }
    
    /// <summary>
    /// Minimum game-days between firings per root character.
    /// </summary>
    public int CooldownDays { get; init; }
    
    /// <summary>
    /// Queue priority. Higher number = presented first.
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// How this event is presented to the player.
    /// Defaults to Popup (pauses game, requires response).
    /// </summary>
    public EventPresentation Presentation { get; init; } = EventPresentation.Popup;

    /// <summary>
    /// How the trigger source should find the "target" entity.
    /// Null if no target is needed.
    /// </summary>
    public TargetSelection? TargetSelection { get; init; }

    /// <summary>
    /// Returns the subset of options whose visibility conditions pass in the given scope.
    /// </summary>
    public List<EventOptionDefinition> GetVisibleOptions(EntityScope scope)
    {
        var visible = new List<EventOptionDefinition>();
        foreach (var option in Options)
        {
            if (option.VisibilityConditions is null || option.VisibilityConditions.Evaluate(scope))
                visible.Add(option);
        }
        return visible;
    }
}

/// <summary>
/// Fires periodically via the simulation tick.
/// Uses MTTH to control probability per tick.
/// This is the "random event" type. Most events are this.
/// </summary>
public class PulseEventDefinition: EventDefinition
{
    /// <summary>
    /// Average game-days before this event fires once conditions are met.
    /// </summary>
    public required double MeanTimeToHappenDays { get; init; }
    
    /// <summary>
    /// Conditional multipliers on the MTTH.
    /// Factor < 1.0 = faster, Factor > 1.0 = slower.
    /// </summary>
    public IReadOnlyList<MtthModifier> MtthModifiers { get; init; } = [];
    
}

/// <summary>
/// Fires when a specific player or AI action is taken.
/// Example: ordering a hit, visiting a business, making a deal.
/// </summary>
public class ActionEventDefinition : EventDefinition
{
    /// <summary>
    /// Which action triggers candidacy.
    /// Must match the action ID published by game systems.
    /// </summary>
    public required string OnActionId { get; init; }
}

/// <summary>
/// Fires at a specific game date. Used for scenarios and campaigns.
/// Example: "On November 14, 1957, the Apalachin Meeting."
/// </summary>
public class StoryBeatEventDefinition : EventDefinition
{
    /// <summary>
    /// The game date when this event becomes a candidate.
    /// </summary>
    public required GameDate StoryDate { get; init; }
}

/// <summary>
/// An event that is never autonomously triggered.
/// Only reachable via TriggerEvent effects (chained from other events).
/// </summary>
public class ChainedEventDefinition : EventDefinition { }