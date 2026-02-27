namespace Mafia.Core.Content.Parsers.Dtos;

public class OptionDto
{
    public string? Type { get; set; }          // null/"default" = deterministic, "skill_check", "random", etc.
    public string Id { get; set; } = "";
    public string DisplayText { get; set; } = "";

    public ConditionDto? VisibilityConditions { get; set; }
    public AiWeightDto? AiWeight { get; set; }

    // ── Deterministic: single outcome ──
    public OptionOutcomeDto? Outcome { get; set; }

    // ── Skill check ──
    public string? StatPath { get; set; }
    public string? StatName { get; set; }
    public int? Difficulty { get; set; }
    public OptionOutcomeDto? Success { get; set; }
    public OptionOutcomeDto? Failure { get; set; }

    // ── Random: weighted outcomes ──
    public List<WeightedOutcomeDto>? Outcomes { get; set; }
}
