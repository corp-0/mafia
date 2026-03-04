using Mafia.Core.Content.Parsers.Dtos;

namespace Mafia.EventEditor.Preview;

/// <summary>
/// Produces human-readable one-line descriptions of condition and effect DTOs
/// for the event preview panel.
/// </summary>
public static class DtoDescriber
{
    public static string DescribeCondition(ConditionDto c)
    {
        return c.Type?.ToLowerInvariant() switch
        {
            "stat_threshold" => $"{c.Path ?? "?"}.{c.Stat ?? "?"} {FmtCmp(c.Comparison)} {c.Value}",
            "has_tag" => $"{c.Path ?? "?"} has tag '{c.Tag}'",
            "has_minimum_rank" => $"{c.Path ?? "?"} rank >= {c.Rank}",
            "has_relationship" => $"{c.From ?? "?"} —[{c.Kind}]→ {c.To ?? "?"}",
            "same_location" => $"{c.PathA ?? "?"} same location as {c.PathB ?? "?"}",
            "date_range" => DescribeDateRange(c),
            "event_fired" => $"event '{c.EventId}' fired {FmtCmp(c.Comparison)} {c.Value} time(s)",
            "not" => c.Inner != null ? $"NOT ({DescribeCondition(c.Inner)})" : "NOT (?)",
            "all_of" => DescribeComposite("ALL OF", c.Conditions),
            "any_of" => DescribeComposite("ANY OF", c.Conditions),
            "none_of" => DescribeComposite("NONE OF", c.Conditions),
            _ => $"[unknown condition: {c.Type}]",
        };
    }

    public static string DescribeEffect(EffectDto e)
    {
        return e.Type?.ToLowerInvariant() switch
        {
            "modify_stat" => $"Modify {e.Path ?? "?"}.{e.Stat} by {FmtSigned(e.Amount)}",
            "set_stat" => $"Set {e.Path ?? "?"}.{e.Stat} to {e.Value}",
            "add_tag" => $"Add tag '{e.Tag}' to {e.Path ?? "?"}",
            "remove_tag" => $"Remove tag '{e.Tag}' from {e.Path ?? "?"}",
            "add_relationship" => $"Add [{e.Kind}] from {e.From ?? "?"} to {e.To ?? "?"}",
            "remove_relationship" => $"Remove [{e.Kind}] from {e.From ?? "?"} to {e.To ?? "?"}",
            "transfer_money" => $"Transfer {e.Amount} from {e.From ?? "?"} to {e.To ?? "?"}",
            "disable_character" => $"Disable {e.Path ?? "?"} ({e.Reason ?? "?"})",
            "enable_character" => $"Enable {e.Path ?? "?"}",
            "kill" => $"Kill {e.Path ?? "?"}",
            "arrest" => $"Arrest {e.Path ?? "?"}",
            "trigger_event" => $"Trigger event '{e.EventId}'",
            "change_rank" => $"Change {e.Path ?? "?"} rank to {e.Rank}",
            "add_memory" => $"Add memory '{e.MemoryId}' to {e.Path ?? "?"}" +
                            (e.ExpiresInDays.HasValue ? $" (expires in {e.ExpiresInDays}d)" : ""),
            "remove_memory" => $"Remove memory '{e.MemoryId}' from {e.Path ?? "?"}",
            "change_nickname" => $"Change {e.Path ?? "?"} nickname to '{e.Nickname}'",
            "add_expense" => $"Add expense '{e.LabelKey}' ({e.Category}, {e.Amount}) to {e.Path ?? "?"}",
            "flee" => $"Flee {e.Path ?? "?"}",
            _ => $"[unknown effect: {e.Type}]",
        };
    }

    private static string DescribeDateRange(ConditionDto c)
    {
        var parts = new List<string>();
        if (c.From != null) parts.Add($"from {c.From}");
        if (c.To != null) parts.Add($"to {c.To}");
        return parts.Count > 0 ? $"date {string.Join(" ", parts)}" : "date range (unspecified)";
    }

    private static string DescribeComposite(string label, List<ConditionDto>? children)
    {
        if (children == null || children.Count == 0)
            return $"{label}: (empty)";

        var described = children.Select(DescribeCondition);
        return $"{label}: [{string.Join(", ", described)}]";
    }

    private static string FmtCmp(string? comparison) => comparison?.ToLowerInvariant() switch
    {
        "greater_than" or "gt" => ">",
        "greater_than_or_equal" or "gte" => ">=",
        "less_than" or "lt" => "<",
        "less_than_or_equal" or "lte" => "<=",
        "equal" or "eq" => "==",
        "not_equal" or "neq" => "!=",
        _ => comparison ?? "?",
    };

    private static string FmtSigned(int? amount) =>
        amount switch
        {
            null => "?",
            >= 0 => $"+{amount}",
            _ => amount.ToString()!,
        };
}
