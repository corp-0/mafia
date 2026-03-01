namespace Mafia.Core.Content.Parsers.Dtos;

/// <summary>
/// Flat DTO for TOML effect deserialization.
/// Discriminated by <see cref="Type"/>; each effect type uses a subset of fields.
/// </summary>
public class EffectDto
{
    public string? Type { get; set; }

    // == Shared: entity targeting ==
    public string? Path { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }

    // == AddTag, RemoveTag ==
    public string? Tag { get; set; }

    // == ModifyStat, SetStat ==
    public string? Stat { get; set; }

    // == ModifyStat, TransferMoney, AddMemory ==
    public int? Amount { get; set; }

    // == SetStat ==
    public int? Value { get; set; }

    // == AddRelationship, RemoveRelationship ==
    public string? Kind { get; set; }

    // == DisableCharacter, EnableCharacter ==
    public string? Reason { get; set; }

    // == TriggerEvent ==
    public string? EventId { get; set; }

    // == ChangeRank ==
    public string? Rank { get; set; }

    // == AddMemory, RemoveMemory ==
    public string? MemoryId { get; set; }

    // == AddMemory ==
    public int? ExpiresInDays { get; set; }

    // == ChangeNickname ==
    public string? Nickname { get; set; }

    // == AddExpense ==
    public string? Category { get; set; }
    public string? LabelKey { get; set; }
}
