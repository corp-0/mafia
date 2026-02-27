namespace Mafia.Core.Content.Parsers.Dtos;

/// <summary>
/// Flat DTO for TOML effect deserialization.
/// Discriminated by <see cref="Type"/>; each effect type uses a subset of fields.
/// </summary>
public class EffectDto
{
    public string? Type { get; set; }

    public string? Path { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Tag { get; set; }
    public string? Stat { get; set; }
    public int? Amount { get; set; }
    public string? Kind { get; set; }
    public string? Reason { get; set; }
    public string? EventId { get; set; }
}
