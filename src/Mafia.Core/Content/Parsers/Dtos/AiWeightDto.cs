namespace Mafia.Core.Content.Parsers.Dtos;

public class AiWeightDto
{
    public int Base { get; set; }
    public List<AiWeightModifierDto>? Modifiers { get; set; }
}

public class AiWeightModifierDto
{
    public string? Trait { get; set; }
    public ConditionDto? StatThreshold { get; set; }
    public int Add { get; set; }
}
