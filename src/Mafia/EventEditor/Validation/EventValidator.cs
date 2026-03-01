using Mafia.Core.Content.Parsers.Dtos;

namespace Mafia.EventEditor.Validation;

/// <summary>
/// Validates an EventDto and returns a list of error messages.
/// </summary>
public static class EventValidator
{
    public static List<string> Validate(EventDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Id))
            errors.Add("Event ID is required.");

        if (string.IsNullOrWhiteSpace(dto.TitleKey))
            errors.Add("Title key is required.");

        if (string.IsNullOrWhiteSpace(dto.DescriptionKey))
            errors.Add("Description key is required.");

        ValidateTriggerSpecifics(dto, errors);
        ValidateOptions(dto, errors);
        ValidateEventConditions(dto, errors);

        return errors;
    }

    private static void ValidateEventConditions(EventDto dto, List<string> errors)
    {
        if (dto.Conditions == null) return;
        for (var i = 0; i < dto.Conditions.Count; i++)
            ValidateCondition(dto.Conditions[i], $"Condition #{i + 1}", errors);
    }

    private static void ValidateOptions(EventDto dto, List<string> errors)
    {
        if (dto.Options == null || dto.Options.Count == 0)
        {
            errors.Add("At least one option is required.");
        }
        else
        {
            for (var i = 0; i < dto.Options.Count; i++)
            {
                OptionDto opt = ValidateOptionBasicFields(dto, errors, i, out var prefix);
                ValidateOptionSpecifics(errors, opt, prefix);
            }
        }
    }

    private static void ValidateOptionSpecifics(List<string> errors, OptionDto opt, string prefix)
    {
        switch (opt.Type)
        {
            case "skill_check":
                ValidateSkillCheckOptions(errors, opt, prefix);
                break;
            case "random":
                ValidateRandomOptions(errors, opt, prefix);
                break;
        }
    }

    private static void ValidateRandomOptions(List<string> errors, OptionDto opt, string prefix)
    {
        if (opt.Outcomes == null || opt.Outcomes.Count == 0)
            errors.Add($"{prefix}: Random option requires at least one weighted outcome.");
        else
        {
            for (var j = 0; j < opt.Outcomes.Count; j++)
            {
                if (opt.Outcomes[j].Weight <= 0)
                    errors.Add($"{prefix}, Outcome #{j + 1}: Weight must be > 0.");
            }
        }
    }

    private static void ValidateSkillCheckOptions(List<string> errors, OptionDto opt, string prefix)
    {
        if (string.IsNullOrWhiteSpace(opt.StatName))
            errors.Add($"{prefix}: Skill check requires stat_name.");
        if (opt.Difficulty is null or <= 0)
            errors.Add($"{prefix}: Skill check requires difficulty > 0.");
    }

    private static OptionDto ValidateOptionBasicFields(EventDto dto, List<string> errors, int i, out string prefix)
    {
        OptionDto option = dto.Options![i];
        prefix = $"Option #{i + 1}";

        if (string.IsNullOrWhiteSpace(option.Id))
            errors.Add($"{prefix}: ID is required.");

        if (string.IsNullOrWhiteSpace(option.DisplayTextKey))
            errors.Add($"{prefix}: Display text key is required.");
        return option;
    }

    private static void ValidateTriggerSpecifics(EventDto dto, List<string> errors)
    {
        switch (dto.TriggerType)
        {
            case "pulse":
                if (dto.MeanTimeToHappenDays is null or <= 0)
                    errors.Add("Pulse events require mean_time_to_happen_days > 0.");
                break;
            case "on_action":
                if (string.IsNullOrWhiteSpace(dto.OnActionId))
                    errors.Add("OnAction events require on_action_id.");
                break;
            case "story_beat":
                if (string.IsNullOrWhiteSpace(dto.StoryDate))
                    errors.Add("StoryBeat events require story_date (YYYY-MM-DD).");
                break;
        }
    }

    private static void ValidateCondition(ConditionDto cond, string prefix, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(cond.Type))
        {
            errors.Add($"{prefix}: Condition type is required.");
            return;
        }

        switch (cond.Type)
        {
            case "stat_threshold":
                ValidateStatThresholdConditions(cond, prefix, errors);
                break;
            case "has_tag":
                ValidateHasTagCondition(cond, prefix, errors);
                break;
            case "has_relationship":
                ValidateHasRelationCondition(cond, prefix, errors);
                break;
            case "has_minimum_rank":
                ValidateMinRankCondition(cond, prefix, errors);
                break;
            case "all_of" or "any_of" or "none_of":
                ValidateNestedConditions(cond, prefix, errors);
                break;
        }
    }

    private static void ValidateNestedConditions(ConditionDto cond, string prefix, List<string> errors)
    {
        if (cond.Conditions == null || cond.Conditions.Count == 0)
            errors.Add($"{prefix}: {cond.Type} requires at least one sub-condition.");
        else
        {
            for (var i = 0; i < cond.Conditions.Count; i++)
                ValidateCondition(cond.Conditions[i], $"{prefix}.{cond.Type}[{i}]", errors);
        }
    }

    private static void ValidateMinRankCondition(ConditionDto cond, string prefix, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(cond.Rank))
            errors.Add($"{prefix}: has_minimum_rank requires rank.");
    }

    private static void ValidateHasRelationCondition(ConditionDto cond, string prefix, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(cond.Kind))
            errors.Add($"{prefix}: has_relationship requires kind.");
    }

    private static void ValidateHasTagCondition(ConditionDto cond, string prefix, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(cond.Tag))
            errors.Add($"{prefix}: has_tag requires tag.");
    }

    private static void ValidateStatThresholdConditions(ConditionDto cond, string prefix, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(cond.Stat))
            errors.Add($"{prefix}: stat_threshold requires stat.");
        if (string.IsNullOrWhiteSpace(cond.Comparison))
            errors.Add($"{prefix}: stat_threshold requires comparison.");
    }
}
