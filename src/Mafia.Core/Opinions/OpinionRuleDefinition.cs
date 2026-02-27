using Mafia.Core.Events.Conditions.Interfaces;

namespace Mafia.Core.Opinions;

public sealed class OpinionRuleDefinition
{
    public required string Id { get; init; }
    public required int Modifier { get; init; }
    public required string TooltipKey { get; init; }
    public required IEventCondition Conditions { get; init; }
}