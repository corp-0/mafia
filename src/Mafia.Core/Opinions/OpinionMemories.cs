using Mafia.Core.Time;

namespace Mafia.Core.Opinions;

public record struct OpinionMemory
{
    public required string DefinitionId { get; init; }
    public required int Amount { get; init; }
    public required GameDate ExpiresOn { get; init; }
}