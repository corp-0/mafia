namespace Mafia.Core.Content.Parsers.Dtos;

public class OpinionRuleDto
{
    public string Id { get; set; } = "";
    public int Modifier { get; set; }
    public string TooltipKey { get; set; } = "";

    public ConditionDto Conditions { get; set; } = new();
}