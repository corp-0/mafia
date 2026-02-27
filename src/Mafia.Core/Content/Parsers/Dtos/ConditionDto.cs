namespace Mafia.Core.Content.Parsers.Dtos;

/// <summary>
/// Flat DTO for TOML condition deserialization.
/// Discriminated by <see cref="Type"/>; each condition type uses a subset of fields.
/// </summary>
public class ConditionDto
{
    public string? Type { get; set; }

    // Leaf fields. Each condition type uses a subset
    public string? Path { get; set; }
    public string? PathA { get; set; }
    public string? PathB { get; set; }
    public string? Tag { get; set; }
    public string? Stat { get; set; }
    public string? Comparison { get; set; }
    public int? Value { get; set; }
    public string? Rank { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Kind { get; set; }

    // Composite fields
    public List<ConditionDto>? Conditions { get; set; }
    public ConditionDto? Inner { get; set; }
}
