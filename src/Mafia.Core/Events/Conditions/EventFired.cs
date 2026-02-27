using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Events.Conditions.Interfaces;

namespace Mafia.Core.Events.Conditions;

/// <summary>
/// Checks whether a specific event has previously fired for a given entity.
/// Two modes:
/// - No comparison/days: just checks HasFired (boolean: "did this event ever fire?")
/// - With comparison + days: checks DaysSince(lastFired) against the threshold
/// </summary>
public sealed class EventFired(string eventId, string entityPath, Comparison? comparison = null, int? days = null) : IEventCondition
{
    public bool Evaluate(EntityScope context)
    {
        if (context.EventHistory is not { } history) return false;
        if (!context.TryNavigate(entityPath, out Entity entity)) return false;
        if (comparison is null || days is null)
        {
            // Simple mode: just check if the event has ever fired
            return history.HasFired(eventId, entity);
        }

        // Time-comparison mode
        var lastFired = history.LastFiredDate(eventId, entity);
        if (lastFired is null) return false;

        var daysSince = context.CurrentDate.DaysSince(lastFired.Value);

        return comparison.Value switch
        {
            Comparison.GreaterThan => daysSince > days.Value,
            Comparison.LessThan => daysSince < days.Value,
            Comparison.GreaterThanOrEqualTo => daysSince >= days.Value,
            Comparison.LessThanOrEqualTo => daysSince <= days.Value,
            Comparison.Equal => daysSince == days.Value,
            _ => false
        };
    }
}
