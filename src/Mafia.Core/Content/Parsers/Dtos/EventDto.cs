namespace Mafia.Core.Content.Parsers.Dtos;

public class EventDto
{
    // == Common fields ==
    public string Id { get; set; } = "";
    public string TitleKey { get; set; } = "";
    public string DescriptionKey { get; set; } = "";
    public string? TriggerType { get; set; }   // "pulse", "on_action", "story_beat"
    public string? ScopeType { get; set; }     // "character", "territory", "relationship", "global"
    public bool IsOneTimeOnly { get; set; }
    public int CooldownDays { get; set; }
    public int Priority { get; set; }

    public List<ConditionDto>? Conditions { get; set; }
    public List<OptionDto>? Options { get; set; }

    // == Pulse-specific ==
    public double? MeanTimeToHappenDays { get; set; }
    public List<MtthModifierDto>? MtthModifiers { get; set; }
    public TargetSelectionDto? TargetSelection { get; set; }

    // == Action-specific ==
    public string? OnActionId { get; set; }

    // == StoryBeat-specific ==
    public string? StoryDate { get; set; }
}