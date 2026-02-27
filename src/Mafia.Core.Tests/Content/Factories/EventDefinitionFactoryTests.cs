using FluentAssertions;
using Mafia.Core.Content.Factories;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.Core.Events.Conditions;
using Mafia.Core.Events.Definition;
using Mafia.Core.Time;
using Xunit;

namespace Mafia.Core.Tests.Content.Factories;

public class EventDefinitionFactoryTests
{
    // == Helpers ==

    private static EventDto MakeMinimalPulseDto() => new()
    {
        Id = "evt_test",
        TitleKey = "Test Event",
        DescriptionKey = "A test event.",
        TriggerType = "pulse",
        MeanTimeToHappenDays = 30.0,
        Options = [MakeMinimalOptionDto()],
    };

    private static OptionDto MakeMinimalOptionDto() => new()
    {
        Id = "opt_a",
        DisplayTextKey = "Do something",
        Outcome = new OptionOutcomeDto
        {
            ResolutionTextKey = "You did it.",
            Effects = [new EffectDto { Type = "modify_stat", Stat = "stress", Path = "root", Amount = 5 }],
        },
    };

    // ═══════════════════════════════════════════════
    //  Trigger dispatch
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_PulseTrigger_ReturnsPulseEventDefinition()
    {
        var dto = MakeMinimalPulseDto();
        var result = EventDefinitionFactory.Create(dto);
        result.Should().BeOfType<PulseEventDefinition>();
        ((PulseEventDefinition)result).MeanTimeToHappenDays.Should().Be(30.0);
    }

    [Fact]
    public void Create_OnActionTrigger_ReturnsActionEventDefinition()
    {
        var dto = MakeMinimalPulseDto();
        dto.TriggerType = "on_action";
        dto.OnActionId = "action_extort";
        dto.MeanTimeToHappenDays = null;

        var result = EventDefinitionFactory.Create(dto);
        result.Should().BeOfType<ActionEventDefinition>();
        ((ActionEventDefinition)result).OnActionId.Should().Be("action_extort");
    }

    [Fact]
    public void Create_StoryBeatTrigger_ReturnsStoryBeatEventDefinition()
    {
        var dto = MakeMinimalPulseDto();
        dto.TriggerType = "story_beat";
        dto.StoryDate = "1930-06-15";
        dto.MeanTimeToHappenDays = null;

        var result = EventDefinitionFactory.Create(dto);
        result.Should().BeOfType<StoryBeatEventDefinition>();
        ((StoryBeatEventDefinition)result).StoryDate.Should().Be(new GameDate(1930, 6, 15));
    }

    [Fact]
    public void Create_NullTrigger_ReturnsChainedEventDefinition()
    {
        var dto = MakeMinimalPulseDto();
        dto.TriggerType = null;
        dto.MeanTimeToHappenDays = null;

        var result = EventDefinitionFactory.Create(dto);
        result.Should().BeOfType<ChainedEventDefinition>();
    }

    [Fact]
    public void Create_ExplicitChainedTrigger_ReturnsChainedEventDefinition()
    {
        var dto = MakeMinimalPulseDto();
        dto.TriggerType = "chained";
        dto.MeanTimeToHappenDays = null;

        var result = EventDefinitionFactory.Create(dto);
        result.Should().BeOfType<ChainedEventDefinition>();
    }

    [Fact]
    public void Create_UnknownTrigger_ThrowsArgumentException()
    {
        var dto = MakeMinimalPulseDto();
        dto.TriggerType = "telekinesis";

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown trigger type*");
    }

    // ═══════════════════════════════════════════════
    //  Common fields
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_MapsIdTitleDescription()
    {
        var dto = MakeMinimalPulseDto();
        dto.Id = "evt_greeting";
        dto.TitleKey = "Hello";
        dto.DescriptionKey = "World";

        var result = EventDefinitionFactory.Create(dto);
        result.Id.Should().Be("evt_greeting");
        result.TitleKey.Should().Be("Hello");
        result.DescriptionKey.Should().Be("World");
    }

    [Theory]
    [InlineData(null, ScopeType.Character)]
    [InlineData("character", ScopeType.Character)]
    [InlineData("location", ScopeType.Location)]
    [InlineData("territory", ScopeType.Location)]
    [InlineData("relationship", ScopeType.Relationship)]
    [InlineData("global", ScopeType.Global)]
    public void Create_ScopeType_MapsCorrectly(string? input, ScopeType expected)
    {
        var dto = MakeMinimalPulseDto();
        dto.ScopeType = input;

        var result = EventDefinitionFactory.Create(dto);
        result.Scope.Should().Be(expected);
    }

    [Fact]
    public void Create_Flags_MapCorrectly()
    {
        var dto = MakeMinimalPulseDto();
        dto.IsOneTimeOnly = true;
        dto.CooldownDays = 14;
        dto.Priority = 5;

        var result = EventDefinitionFactory.Create(dto);
        result.IsOneTimeOnly.Should().BeTrue();
        result.CooldownDays.Should().Be(14);
        result.Priority.Should().Be(5);
    }

    // ═══════════════════════════════════════════════
    //  Conditions folding
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_NullConditions_ResultsInNull()
    {
        var dto = MakeMinimalPulseDto();
        dto.Conditions = null;

        var result = EventDefinitionFactory.Create(dto);
        result.Conditions.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyConditions_ResultsInNull()
    {
        var dto = MakeMinimalPulseDto();
        dto.Conditions = [];

        var result = EventDefinitionFactory.Create(dto);
        result.Conditions.Should().BeNull();
    }

    [Fact]
    public void Create_SingleCondition_NotWrappedInAllOf()
    {
        var dto = MakeMinimalPulseDto();
        dto.Conditions = [new ConditionDto { Type = "has_tag", Tag = "underboss", Path = "root" }];

        var result = EventDefinitionFactory.Create(dto);
        result.Conditions.Should().NotBeNull();
        result.Conditions.Should().NotBeOfType<AllOf>();
    }

    [Fact]
    public void Create_MultipleConditions_WrappedInAllOf()
    {
        var dto = MakeMinimalPulseDto();
        dto.Conditions =
        [
            new ConditionDto { Type = "has_tag", Tag = "underboss", Path = "root" },
            new ConditionDto { Type = "stat_threshold", Stat = "muscle", Path = "root", Comparison = "gte", Value = 5 },
        ];

        var result = EventDefinitionFactory.Create(dto);
        result.Conditions.Should().BeOfType<AllOf>();
    }

    // ═══════════════════════════════════════════════
    //  Options
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_StandardOption_NullType_Defaults()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options = [MakeMinimalOptionDto()];
        dto.Options[0].Type = null;

        var result = EventDefinitionFactory.Create(dto);
        result.Options.Should().HaveCount(1);
        result.Options[0].Should().BeOfType<StandardOptionDefinition>();
    }

    [Fact]
    public void Create_StandardOption_ExplicitType()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options = [MakeMinimalOptionDto()];
        dto.Options[0].Type = "standard";

        var result = EventDefinitionFactory.Create(dto);
        result.Options[0].Should().BeOfType<StandardOptionDefinition>();
    }

    [Fact]
    public void Create_SkillCheckOption_MapsFields()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options =
        [
            new OptionDto
            {
                Type = "skill_check",
                Id = "opt_sc",
                DisplayTextKey = "Try to intimidate",
                StatPath = "root",
                StatName = "muscle",
                Difficulty = 10,
                Success = new OptionOutcomeDto { ResolutionTextKey = "Success!", Effects = [] },
                Failure = new OptionOutcomeDto { ResolutionTextKey = "Failure!", Effects = [] },
            }
        ];

        var result = EventDefinitionFactory.Create(dto);
        var opt = result.Options[0].Should().BeOfType<SkillCheckOptionDefinition>().Subject;
        opt.StatPath.Should().Be("root");
        opt.StatName.Should().Be("muscle");
        opt.Difficulty.Should().Be(10);
        opt.Success.ResolutionTextKey.Should().Be("Success!");
        opt.Failure.ResolutionTextKey.Should().Be("Failure!");
    }

    [Fact]
    public void Create_RandomOption_MapsWeightedOutcomes()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options =
        [
            new OptionDto
            {
                Type = "random",
                Id = "opt_r",
                DisplayTextKey = "Roll the dice",
                Outcomes =
                [
                    new WeightedOutcomeDto { Weight = 3, ResolutionTextKey = "Good", Effects = [] },
                    new WeightedOutcomeDto { Weight = 1, ResolutionTextKey = "Bad", Effects = [] },
                ],
            }
        ];

        var result = EventDefinitionFactory.Create(dto);
        var opt = result.Options[0].Should().BeOfType<RandomOptionDefinition>().Subject;
        opt.Outcomes.Should().HaveCount(2);
        opt.Outcomes[0].Weight.Should().Be(3);
        opt.Outcomes[1].Weight.Should().Be(1);
    }

    [Fact]
    public void Create_UnknownOptionType_ThrowsArgumentException()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options = [new OptionDto { Type = "telepathy", Id = "x", DisplayTextKey = "x", Outcome = new() }];

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown option type*");
    }

    [Fact]
    public void Create_NoOptions_ThrowsArgumentException()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options = null;

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*at least one option*");
    }

    [Fact]
    public void Create_EmptyOptions_ThrowsArgumentException()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options = [];

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*at least one option*");
    }

    // ═══════════════════════════════════════════════
    //  Outcomes
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_Outcome_MapsResolutionText()
    {
        var dto = MakeMinimalPulseDto();
        var result = EventDefinitionFactory.Create(dto);
        var opt = (StandardOptionDefinition)result.Options[0];
        opt.Outcome.ResolutionTextKey.Should().Be("You did it.");
    }

    [Fact]
    public void Create_Outcome_MapsEffects()
    {
        var dto = MakeMinimalPulseDto();
        var result = EventDefinitionFactory.Create(dto);
        var opt = (StandardOptionDefinition)result.Options[0];
        opt.Outcome.Effects.Should().HaveCount(1);
    }

    [Fact]
    public void Create_Outcome_NullEffects_ReturnsEmptyList()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options =
        [
            new OptionDto
            {
                Id = "opt",
                DisplayTextKey = "x",
                Outcome = new OptionOutcomeDto { ResolutionTextKey = "ok", Effects = null },
            }
        ];

        var result = EventDefinitionFactory.Create(dto);
        var opt = (StandardOptionDefinition)result.Options[0];
        opt.Outcome.Effects.Should().BeEmpty();
    }

    [Fact]
    public void Create_WeightedOutcome_MapsWeight()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options =
        [
            new OptionDto
            {
                Type = "random",
                Id = "opt",
                DisplayTextKey = "x",
                Outcomes = [new WeightedOutcomeDto { Weight = 7, ResolutionTextKey = "Lucky", Effects = [] }],
            }
        ];

        var result = EventDefinitionFactory.Create(dto);
        var opt = (RandomOptionDefinition)result.Options[0];
        opt.Outcomes[0].Weight.Should().Be(7);
        opt.Outcomes[0].ResolutionTextKey.Should().Be("Lucky");
    }

    [Fact]
    public void Create_StandardOption_MissingOutcome_Throws()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options = [new OptionDto { Id = "x", DisplayTextKey = "x", Outcome = null }];

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*Outcome*");
    }

    [Fact]
    public void Create_SkillCheckOption_MissingSuccess_Throws()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options =
        [
            new OptionDto
            {
                Type = "skill_check",
                Id = "x", DisplayTextKey = "x",
                StatPath = "root", StatName = "muscle", Difficulty = 10,
                Success = null,
                Failure = new OptionOutcomeDto { Effects = [] },
            }
        ];

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*Success*");
    }

    [Fact]
    public void Create_SkillCheckOption_MissingFailure_Throws()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options =
        [
            new OptionDto
            {
                Type = "skill_check",
                Id = "x", DisplayTextKey = "x",
                StatPath = "root", StatName = "muscle", Difficulty = 10,
                Success = new OptionOutcomeDto { Effects = [] },
                Failure = null,
            }
        ];

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*Failure*");
    }

    [Fact]
    public void Create_RandomOption_MissingOutcomes_Throws()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options = [new OptionDto { Type = "random", Id = "x", DisplayTextKey = "x", Outcomes = null }];

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*Outcomes*");
    }

    // ═══════════════════════════════════════════════
    //  AiWeight
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_NullAiWeight_DefaultsToBaseWeight1()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options![0].AiWeight = null;

        var result = EventDefinitionFactory.Create(dto);
        result.Options[0].AiWeight.BaseWeight.Should().Be(1);
        result.Options[0].AiWeight.Modifiers.Should().BeEmpty();
    }

    [Fact]
    public void Create_AiWeight_MapsBaseAndModifiers()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options![0].AiWeight = new AiWeightDto
        {
            Base = 5,
            Modifiers =
            [
                new AiWeightModifierDto
                {
                    Trait = "underboss",
                    Add = 3,
                },
            ],
        };

        var result = EventDefinitionFactory.Create(dto);
        result.Options[0].AiWeight.BaseWeight.Should().Be(5);
        result.Options[0].AiWeight.Modifiers.Should().HaveCount(1);
        result.Options[0].AiWeight.Modifiers[0].Add.Should().Be(3);
    }

    [Fact]
    public void Create_AiWeightModifier_Trait_ExpandsToHasTag()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options![0].AiWeight = new AiWeightDto
        {
            Base = 1,
            Modifiers = [new AiWeightModifierDto { Trait = "underboss", Add = 2 }],
        };

        var result = EventDefinitionFactory.Create(dto);
        result.Options[0].AiWeight.Modifiers[0].Condition.Should().NotBeNull();
    }

    [Fact]
    public void Create_AiWeightModifier_StatThreshold_AutoSetsType()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options![0].AiWeight = new AiWeightDto
        {
            Base = 1,
            Modifiers =
            [
                new AiWeightModifierDto
                {
                    StatThreshold = new ConditionDto { Stat = "muscle", Path = "root", Comparison = "gte", Value = 5 },
                    Add = 4,
                },
            ],
        };

        var result = EventDefinitionFactory.Create(dto);
        result.Options[0].AiWeight.Modifiers[0].Add.Should().Be(4);
        result.Options[0].AiWeight.Modifiers[0].Condition.Should().NotBeNull();
    }

    [Fact]
    public void Create_AiWeightModifier_GenericCondition_TakesPriority()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options![0].AiWeight = new AiWeightDto
        {
            Base = 1,
            Modifiers =
            [
                new AiWeightModifierDto
                {
                    Condition = new ConditionDto { Type = "has_tag", Tag = "consigliere", Path = "root" },
                    Trait = "underboss", // should be ignored
                    Add = 10,
                },
            ],
        };

        var result = EventDefinitionFactory.Create(dto);
        result.Options[0].AiWeight.Modifiers[0].Add.Should().Be(10);
        result.Options[0].AiWeight.Modifiers[0].Condition.Should().NotBeNull();
    }

    [Fact]
    public void Create_AiWeightModifier_NoCondition_Throws()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options![0].AiWeight = new AiWeightDto
        {
            Base = 1,
            Modifiers = [new AiWeightModifierDto { Add = 5 }],
        };

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*Condition*Trait*StatThreshold*");
    }

    // ═══════════════════════════════════════════════
    //  TargetSelection
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_TargetSelection_MapsPoolAndMode()
    {
        var dto = MakeMinimalPulseDto();
        dto.TargetSelection = new TargetSelectionDto
        {
            Pool = "root_subordinates",
            SelectionMode = "highest_stat:respect",
        };

        var result = (PulseEventDefinition)EventDefinitionFactory.Create(dto);
        result.TargetSelection.Should().NotBeNull();
        result.TargetSelection!.Pool.Should().Be("root_subordinates");
        result.TargetSelection.SelectionMode.Should().Be("highest_stat:respect");
    }

    [Fact]
    public void Create_TargetSelection_DefaultSelectionMode_IsRandom()
    {
        var dto = MakeMinimalPulseDto();
        dto.TargetSelection = new TargetSelectionDto
        {
            Pool = "same_territory",
        };

        var result = (PulseEventDefinition)EventDefinitionFactory.Create(dto);
        result.TargetSelection!.SelectionMode.Should().Be("random");
    }

    [Fact]
    public void Create_TargetSelection_RequiredTrait_ExpandsToHasTag()
    {
        var dto = MakeMinimalPulseDto();
        dto.TargetSelection = new TargetSelectionDto
        {
            Pool = "root_subordinates",
            RequiredTrait = "underboss",
        };

        var result = (PulseEventDefinition)EventDefinitionFactory.Create(dto);
        result.TargetSelection!.Filter.Should().NotBeNull();
    }

    [Fact]
    public void Create_TargetSelection_Filter_TakesPriorityOverRequiredTrait()
    {
        var dto = MakeMinimalPulseDto();
        dto.TargetSelection = new TargetSelectionDto
        {
            Pool = "root_subordinates",
            Filter = new ConditionDto { Type = "stat_threshold", Stat = "muscle", Path = "root", Comparison = "gte", Value = 5 },
            RequiredTrait = "underboss", // should be ignored
        };

        var result = (PulseEventDefinition)EventDefinitionFactory.Create(dto);
        result.TargetSelection!.Filter.Should().NotBeNull();
    }

    [Fact]
    public void Create_TargetSelection_NoFilter_NullFilter()
    {
        var dto = MakeMinimalPulseDto();
        dto.TargetSelection = new TargetSelectionDto
        {
            Pool = "root_subordinates",
        };

        var result = (PulseEventDefinition)EventDefinitionFactory.Create(dto);
        result.TargetSelection!.Filter.Should().BeNull();
    }

    [Fact]
    public void Create_NoTargetSelection_NullOnDefinition()
    {
        var dto = MakeMinimalPulseDto();
        dto.TargetSelection = null;

        var result = (PulseEventDefinition)EventDefinitionFactory.Create(dto);
        result.TargetSelection.Should().BeNull();
    }

    // ═══════════════════════════════════════════════
    //  MtthModifier
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_MtthModifiers_MapsFactorAndCondition()
    {
        var dto = MakeMinimalPulseDto();
        dto.MtthModifiers =
        [
            new MtthModifierDto
            {
                Factor = 0.5,
                Condition = new ConditionDto { Type = "has_tag", Tag = "underboss", Path = "root" },
            },
        ];

        var result = (PulseEventDefinition)EventDefinitionFactory.Create(dto);
        result.MtthModifiers.Should().HaveCount(1);
        result.MtthModifiers[0].Factor.Should().Be(0.5);
        result.MtthModifiers[0].Condition.Should().NotBeNull();
    }

    [Fact]
    public void Create_MtthModifiers_NullList_DefaultsToEmpty()
    {
        var dto = MakeMinimalPulseDto();
        dto.MtthModifiers = null;

        var result = (PulseEventDefinition)EventDefinitionFactory.Create(dto);
        result.MtthModifiers.Should().BeEmpty();
    }

    [Fact]
    public void Create_MtthModifier_MissingCondition_Throws()
    {
        var dto = MakeMinimalPulseDto();
        dto.MtthModifiers = [new MtthModifierDto { Factor = 0.5, Condition = null }];

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*Condition*");
    }

    // ═══════════════════════════════════════════════
    //  Visibility conditions
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_OptionVisibilityConditions_Present_Maps()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options![0].VisibilityConditions = new ConditionDto
        {
            Type = "has_tag",
            Tag = "underboss",
            Path = "root",
        };

        var result = EventDefinitionFactory.Create(dto);
        result.Options[0].VisibilityConditions.Should().NotBeNull();
    }

    [Fact]
    public void Create_OptionVisibilityConditions_Null_IsNull()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options![0].VisibilityConditions = null;

        var result = EventDefinitionFactory.Create(dto);
        result.Options[0].VisibilityConditions.Should().BeNull();
    }

    // ═══════════════════════════════════════════════
    //  Option id & display text
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_Option_MapsIdAndDisplayText()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options![0].Id = "opt_custom";
        dto.Options![0].DisplayTextKey = "Custom text";

        var result = EventDefinitionFactory.Create(dto);
        result.Options[0].Id.Should().Be("opt_custom");
        result.Options[0].DisplayTextKey.Should().Be("Custom text");
    }

    // ═══════════════════════════════════════════════
    //  Pulse-specific missing fields
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_Pulse_MissingMtth_Throws()
    {
        var dto = MakeMinimalPulseDto();
        dto.MeanTimeToHappenDays = null;

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*MeanTimeToHappenDays*");
    }

    [Fact]
    public void Create_OnAction_MissingOnActionId_Throws()
    {
        var dto = MakeMinimalPulseDto();
        dto.TriggerType = "on_action";
        dto.OnActionId = null;

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*OnActionId*");
    }

    [Fact]
    public void Create_StoryBeat_MissingStoryDate_Throws()
    {
        var dto = MakeMinimalPulseDto();
        dto.TriggerType = "story_beat";
        dto.StoryDate = null;

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*StoryDate*");
    }

    // ═══════════════════════════════════════════════
    //  SkillCheck missing fields
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_SkillCheck_MissingStatPath_Throws()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options =
        [
            new OptionDto
            {
                Type = "skill_check",
                Id = "x", DisplayTextKey = "x",
                StatPath = null, StatName = "muscle", Difficulty = 10,
                Success = new OptionOutcomeDto { Effects = [] },
                Failure = new OptionOutcomeDto { Effects = [] },
            }
        ];

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*StatPath*");
    }

    [Fact]
    public void Create_SkillCheck_MissingStatName_Throws()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options =
        [
            new OptionDto
            {
                Type = "skill_check",
                Id = "x", DisplayTextKey = "x",
                StatPath = "root", StatName = null, Difficulty = 10,
                Success = new OptionOutcomeDto { Effects = [] },
                Failure = new OptionOutcomeDto { Effects = [] },
            }
        ];

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*StatName*");
    }

    [Fact]
    public void Create_SkillCheck_MissingDifficulty_Throws()
    {
        var dto = MakeMinimalPulseDto();
        dto.Options =
        [
            new OptionDto
            {
                Type = "skill_check",
                Id = "x", DisplayTextKey = "x",
                StatPath = "root", StatName = "muscle", Difficulty = null,
                Success = new OptionOutcomeDto { Effects = [] },
                Failure = new OptionOutcomeDto { Effects = [] },
            }
        ];

        var act = () => EventDefinitionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*Difficulty*");
    }
}
