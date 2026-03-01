using Humanizer;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.Core.Events.Conditions;
using Mafia.Core.Events.Conditions.Interfaces;
using Mafia.Core.Events.Definition;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Time;

namespace Mafia.Core.Content.Factories;

public static class EventDefinitionFactory
{
    public static EventDefinition Create(EventDto dto)
    {
        var conditions = FoldConditions(dto.Conditions);
        var options = dto.Options is not { Count: > 0 }
            ? throw new ArgumentException("EventDto must have at least one option.")
            : dto.Options.Select(CreateOption).ToArray();

        return ParseTomlEnum(dto.TriggerType, TriggerType.Chained) switch
        {
            TriggerType.Chained => new ChainedEventDefinition
            {
                Id = dto.Id,
                TitleKey = dto.TitleKey,
                DescriptionKey = dto.DescriptionKey,
                Scope = ParseTomlEnum(dto.ScopeType, ScopeType.Character),
                Conditions = conditions,
                Options = options,
                IsOneTimeOnly = dto.IsOneTimeOnly,
                CooldownDays = dto.CooldownDays,
                Priority = dto.Priority,
                Presentation = ParseTomlEnum(dto.Presentation, EventPresentation.Popup),
                TargetSelection = dto.TargetSelection is not null
                    ? CreateTargetSelection(dto.TargetSelection)
                    : null,
            },
            TriggerType.Pulse => new PulseEventDefinition
            {
                Id = dto.Id,
                TitleKey = dto.TitleKey,
                DescriptionKey = dto.DescriptionKey,
                Scope = ParseTomlEnum(dto.ScopeType, ScopeType.Character),
                Conditions = conditions,
                Options = options,
                IsOneTimeOnly = dto.IsOneTimeOnly,
                CooldownDays = dto.CooldownDays,
                Priority = dto.Priority,
                Presentation = ParseTomlEnum(dto.Presentation, EventPresentation.Popup),
                MeanTimeToHappenDays = dto.MeanTimeToHappenDays
                                       ?? throw new ArgumentException("PulseEventDefinition requires MeanTimeToHappenDays."),
                MtthModifiers = dto.MtthModifiers?.Select(CreateMtthModifier).ToArray() ?? [],
                TargetSelection = dto.TargetSelection is not null
                    ? CreateTargetSelection(dto.TargetSelection)
                    : null,
            },
            TriggerType.OnAction => new ActionEventDefinition
            {
                Id = dto.Id,
                TitleKey = dto.TitleKey,
                DescriptionKey = dto.DescriptionKey,
                Scope = ParseTomlEnum(dto.ScopeType, ScopeType.Character),
                Conditions = conditions,
                Options = options,
                IsOneTimeOnly = dto.IsOneTimeOnly,
                CooldownDays = dto.CooldownDays,
                Priority = dto.Priority,
                Presentation = ParseTomlEnum(dto.Presentation, EventPresentation.Popup),
                OnActionId = dto.OnActionId
                             ?? throw new ArgumentException("ActionEventDefinition requires OnActionId."),
                TargetSelection = dto.TargetSelection is not null
                    ? CreateTargetSelection(dto.TargetSelection)
                    : null,
            },
            TriggerType.StoryBeat => new StoryBeatEventDefinition
            {
                Id = dto.Id,
                TitleKey = dto.TitleKey,
                DescriptionKey = dto.DescriptionKey,
                Scope = ParseTomlEnum(dto.ScopeType, ScopeType.Character),
                Conditions = conditions,
                Options = options,
                IsOneTimeOnly = dto.IsOneTimeOnly,
                CooldownDays = dto.CooldownDays,
                Priority = dto.Priority,
                Presentation = ParseTomlEnum(dto.Presentation, EventPresentation.Popup),
                StoryDate = GameDate.Parse(dto.StoryDate
                                           ?? throw new ArgumentException("StoryBeatEventDefinition requires StoryDate.")),
                TargetSelection = dto.TargetSelection is not null
                    ? CreateTargetSelection(dto.TargetSelection)
                    : null,
            },
            var unknown => throw new ArgumentException($"Unknown trigger type: '{dto.TriggerType}'")
        };
    }

    private static TEnum ParseTomlEnum<TEnum>(string? raw, TEnum defaultValue) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        var pascalized = raw.Pascalize();

        if (Enum.TryParse<TEnum>(pascalized, out var result))
            return result;

        throw new ArgumentException($"Unknown {typeof(TEnum).Name}: '{raw}'");
    }

    private static IEventCondition? FoldConditions(List<ConditionDto>? conditions)
    {
        if (conditions is not { Count: > 0 })
            return null;

        if (conditions.Count == 1)
            return ConditionFactory.Create(conditions[0]);

        return new AllOf(conditions.Select(ConditionFactory.Create).ToArray());
    }

    private static EventOptionDefinition CreateOption(OptionDto dto)
    {
        return ParseTomlEnum(dto.Type, OptionType.Standard) switch
        {
            OptionType.Standard => new StandardOptionDefinition
            {
                Id = dto.Id,
                DisplayTextKey = dto.DisplayTextKey,
                VisibilityConditions = dto.VisibilityConditions is not null
                    ? ConditionFactory.Create(dto.VisibilityConditions)
                    : null,
                AiWeight = CreateAiWeight(dto.AiWeight),
                Outcome = CreateOutcome(dto.Outcome ?? new OptionOutcomeDto
                {
                    ResolutionTextKey = dto.ResolutionTextKey
                                        ?? throw new ArgumentException("Standard option requires an Outcome or ResolutionText."),
                    Effects = dto.Effects,
                }),
            },
            OptionType.SkillCheck => new SkillCheckOptionDefinition
            {
                Id = dto.Id,
                DisplayTextKey = dto.DisplayTextKey,
                VisibilityConditions = dto.VisibilityConditions is not null
                    ? ConditionFactory.Create(dto.VisibilityConditions)
                    : null,
                AiWeight = CreateAiWeight(dto.AiWeight),
                StatPath = dto.StatPath
                           ?? throw new ArgumentException("SkillCheck option requires StatPath."),
                StatName = dto.StatName
                           ?? throw new ArgumentException("SkillCheck option requires StatName."),
                Difficulty = dto.Difficulty
                             ?? throw new ArgumentException("SkillCheck option requires Difficulty."),
                Success = CreateOutcome(dto.Success
                                        ?? throw new ArgumentException("SkillCheck option requires Success outcome.")),
                Failure = CreateOutcome(dto.Failure
                                        ?? throw new ArgumentException("SkillCheck option requires Failure outcome.")),
            },
            OptionType.Random => new RandomOptionDefinition
            {
                Id = dto.Id,
                DisplayTextKey = dto.DisplayTextKey,
                VisibilityConditions = dto.VisibilityConditions is not null
                    ? ConditionFactory.Create(dto.VisibilityConditions)
                    : null,
                AiWeight = CreateAiWeight(dto.AiWeight),
                Outcomes = dto.Outcomes?.Select(CreateWeightedOutcome).ToArray()
                           ?? throw new ArgumentException("Random option requires Outcomes."),
            },
            var unknown => throw new ArgumentException($"Unknown option type: '{dto.Type}'")
        };
    }

    private static EventOutcome CreateOutcome(OptionOutcomeDto dto)
    {
        return new EventOutcome
        {
            ResolutionTextKey = dto.ResolutionTextKey,
            Effects = dto.Effects?.Select(EffectFactory.Create).ToArray()
                      ?? [],
        };
    }

    private static WeightedEventOutcome CreateWeightedOutcome(WeightedOutcomeDto dto)
    {
        return new WeightedEventOutcome
        {
            ResolutionTextKey = dto.ResolutionTextKey,
            Effects = dto.Effects?.Select(EffectFactory.Create).ToArray()
                      ?? Array.Empty<IEventEffect>(),
            Weight = dto.Weight,
        };
    }

    private static AiWeight CreateAiWeight(AiWeightDto? dto)
    {
        if (dto is null)
            return new AiWeight { BaseWeight = 1 };

        return new AiWeight
        {
            BaseWeight = dto.Base,
            Modifiers = dto.Modifiers?.Select(CreateAiWeightModifier).ToArray() ?? [],
        };
    }

    private static AiWeightModifier CreateAiWeightModifier(AiWeightModifierDto dto)
    {
        var condition = ResolveAiWeightCondition(dto);
        return new AiWeightModifier
        {
            Condition = condition,
            Add = dto.Add,
        };
    }

    private static IEventCondition ResolveAiWeightCondition(AiWeightModifierDto dto)
    {
        if (dto.Condition is not null)
            return ConditionFactory.Create(dto.Condition);

        if (dto.Trait is not null)
            return ConditionFactory.Create(new ConditionDto
            {
                Type = "has_tag",
                Tag = dto.Trait,
                Path = "root",
            });

        if (dto.StatThreshold is not null)
        {
            var st = dto.StatThreshold;
            st.Type ??= "stat_threshold";
            return ConditionFactory.Create(st);
        }

        throw new ArgumentException("AiWeightModifier must have a Condition, Trait, or StatThreshold.");
    }

    private static MtthModifier CreateMtthModifier(MtthModifierDto dto)
    {
        return new MtthModifier
        {
            Condition = dto.Condition is not null
                ? ConditionFactory.Create(dto.Condition)
                : throw new ArgumentException("MtthModifier requires a Condition."),
            Factor = dto.Factor,
        };
    }

    private static TargetSelection CreateTargetSelection(TargetSelectionDto dto)
    {
        return new TargetSelection
        {
            Pool = dto.Pool ?? throw new ArgumentException("TargetSelection requires a Pool."),
            Filter = ResolveTargetFilter(dto),
            SelectionMode = dto.SelectionMode ?? "random",
        };
    }

    private static IEventCondition? ResolveTargetFilter(TargetSelectionDto dto)
    {
        if (dto.Filter is not null)
            return ConditionFactory.Create(dto.Filter);

        if (dto.RequiredTrait is not null)
            return ConditionFactory.Create(new ConditionDto
            {
                Type = "has_tag",
                Tag = dto.RequiredTrait,
                Path = "root",
            });

        return null;
    }

}
