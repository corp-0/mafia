namespace Mafia.Core.Content.Parsers.Dtos;

/// <summary>
/// Flat DTO for TOML condition deserialization.
/// Discriminated by <see cref="Type"/>; each condition type uses a subset of fields.
/// </summary>
public class ConditionDto
{
    public string? Type { get; set; }

    // == Shared: entity targeting ==
    public string? Path { get; set; }

    // == SameLocation ==
    public string? PathA { get; set; }
    public string? PathB { get; set; }

    // == HasTagCondition ==
    public string? Tag { get; set; }

    // == StatThreshold, EventFired ==
    public string? Comparison { get; set; }
    public int? Value { get; set; }

    // == StatThreshold ==
    public string? Stat { get; set; }

    // == HasMinimumRank ==
    public string? Rank { get; set; }

    // == HasRelationship ==
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Kind { get; set; }

    // == EventFired ==
    public string? EventId { get; set; }

    // == Composite: AllOf, AnyOf, NoneOf ==
    public List<ConditionDto>? Conditions { get; set; }
    public ConditionDto? Inner { get; set; }
}
