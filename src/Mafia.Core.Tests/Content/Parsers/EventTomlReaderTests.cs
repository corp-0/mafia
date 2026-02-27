using FluentAssertions;
using Mafia.Core.Content.Parsers;
using Mafia.Core.Events.Conditions;
using Mafia.Core.Events.Definition;
using Mafia.Core.Time;
using Xunit;

namespace Mafia.Core.Tests.Content.Parsers;

public class EventTomlReaderTests
{
    // ═══════════════════════════════════════════════
    //  Snake-case converter
    // ═══════════════════════════════════════════════

    [Theory]
    [InlineData("Id", "id")]
    [InlineData("PathA", "path_a")]
    [InlineData("MeanTimeToHappenDays", "mean_time_to_happen_days")]
    [InlineData("DisplayText", "display_text")]
    [InlineData("IsOneTimeOnly", "is_one_time_only")]
    public void PascalToSnakeCase_ConvertsCorrectly(string input, string expected)
    {
        TomlOptions.PascalToSnakeCase(input).Should().Be(expected);
    }

    // ═══════════════════════════════════════════════
    //  Pulse event full roundtrip
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_PulseEvent_ReturnsCorrectDefinition()
    {
        const string toml = """
            id = "soldier_skimming"
            title_key = "Skimming Off the Top"
            description_key = "One of your soldiers has been skimming."
            trigger_type = "pulse"
            mean_time_to_happen_days = 730.0
            is_one_time_only = false
            cooldown_days = 90
            priority = 3

            [[conditions]]
            type = "has_tag"
            tag = "underboss"
            path = "root"

            [[options]]
            id = "opt_punish"
            display_text_key = "Punish the soldier"

            [options.outcome]
            resolution_text_key = "You made an example of him."

            [[options.outcome.effects]]
            type = "modify_stat"
            stat = "stress"
            path = "root"
            amount = 10
            """;

        var result = EventTomlReader.Read(toml);

        result.Should().BeOfType<PulseEventDefinition>();
        var pulse = (PulseEventDefinition)result;
        pulse.Id.Should().Be("soldier_skimming");
        pulse.TitleKey.Should().Be("Skimming Off the Top");
        pulse.DescriptionKey.Should().Be("One of your soldiers has been skimming.");
        pulse.MeanTimeToHappenDays.Should().Be(730.0);
        pulse.IsOneTimeOnly.Should().BeFalse();
        pulse.CooldownDays.Should().Be(90);
        pulse.Priority.Should().Be(3);
        pulse.Conditions.Should().NotBeNull();
        pulse.Options.Should().HaveCount(1);
        var opt = pulse.Options[0].Should().BeOfType<StandardOptionDefinition>().Subject;
        opt.Id.Should().Be("opt_punish");
        opt.DisplayTextKey.Should().Be("Punish the soldier");
        opt.Outcome.ResolutionTextKey.Should().Be("You made an example of him.");
        opt.Outcome.Effects.Should().HaveCount(1);
    }

    // ═══════════════════════════════════════════════
    //  Action event
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_ActionEvent_ReturnsActionEventDefinition()
    {
        const string toml = """
            id = "extort_result"
            title_key = "Extortion Result"
            description_key = "The extortion is complete."
            trigger_type = "on_action"
            on_action_id = "action_extort"

            [[options]]
            id = "opt_ok"
            display_text_key = "Acknowledge"

            [options.outcome]
            resolution_text_key = "Done."
            """;

        var result = EventTomlReader.Read(toml);

        result.Should().BeOfType<ActionEventDefinition>();
        ((ActionEventDefinition)result).OnActionId.Should().Be("action_extort");
    }

    // ═══════════════════════════════════════════════
    //  Story beat
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_StoryBeat_ReturnsStoryBeatEventDefinitionWithDate()
    {
        const string toml = """
            id = "apalachin_meeting"
            title_key = "The Apalachin Meeting"
            description_key = "A historic gathering."
            trigger_type = "story_beat"
            story_date = "1957-11-14"

            [[options]]
            id = "opt_attend"
            display_text_key = "Attend the meeting"

            [options.outcome]
            resolution_text_key = "You attended."
            """;

        var result = EventTomlReader.Read(toml);

        result.Should().BeOfType<StoryBeatEventDefinition>();
        var storyBeat = (StoryBeatEventDefinition)result;
        storyBeat.StoryDate.Should().Be(new GameDate(1957, 11, 14));
    }

    // ═══════════════════════════════════════════════
    //  Chained event (no trigger_type)
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_NoTriggerType_ReturnsChainedEventDefinition()
    {
        const string toml = """
            id = "followup_event"
            title_key = "Follow-up"
            description_key = "A chained event."

            [[options]]
            id = "opt_ok"
            display_text_key = "OK"

            [options.outcome]
            resolution_text_key = "Acknowledged."
            """;

        var result = EventTomlReader.Read(toml);

        result.Should().BeOfType<ChainedEventDefinition>();
    }

    // ═══════════════════════════════════════════════
    //  Multiple conditions → AllOf wrapping
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_MultipleConditions_WrappedInAllOf()
    {
        const string toml = """
            id = "evt_cond"
            title_key = "Conditional"
            description_key = "Has conditions."
            trigger_type = "pulse"
            mean_time_to_happen_days = 365.0

            [[conditions]]
            type = "has_tag"
            tag = "underboss"
            path = "root"

            [[conditions]]
            type = "stat_threshold"
            stat = "muscle"
            path = "root"
            comparison = "gte"
            value = 5

            [[options]]
            id = "opt_a"
            display_text_key = "Go"

            [options.outcome]
            resolution_text_key = "Done."
            """;

        var result = EventTomlReader.Read(toml);

        result.Conditions.Should().BeOfType<AllOf>();
    }

    // ═══════════════════════════════════════════════
    //  Flat option fields (resolution_text + effects)
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_FlatOptionFields_ResolutionTextAndEffects()
    {
        const string toml = """
            id = "evt_flat"
            title_key = "Flat Options"
            description_key = "Uses flat option fields."
            trigger_type = "pulse"
            mean_time_to_happen_days = 100.0

            [[options]]
            id = "opt_flat"
            display_text_key = "Do it"
            resolution_text_key = "You did it."

            [[options.effects]]
            type = "modify_stat"
            stat = "stress"
            path = "root"
            amount = -5
            """;

        var result = EventTomlReader.Read(toml);

        var opt = result.Options[0].Should().BeOfType<StandardOptionDefinition>().Subject;
        opt.Outcome.ResolutionTextKey.Should().Be("You did it.");
        opt.Outcome.Effects.Should().HaveCount(1);
    }

    // ═══════════════════════════════════════════════
    //  Skill check option
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_SkillCheckOption_MapsAllFields()
    {
        const string toml = """
            id = "evt_sc"
            title_key = "Skill Check"
            description_key = "Test your nerve."
            trigger_type = "pulse"
            mean_time_to_happen_days = 200.0

            [[options]]
            type = "skill_check"
            id = "opt_bluff"
            display_text_key = "Try to bluff"
            stat_path = "root"
            stat_name = "charm"
            difficulty = 10

            [options.success]
            resolution_text_key = "You fooled them."

            [options.failure]
            resolution_text_key = "They saw right through you."

            [[options.failure.effects]]
            type = "modify_stat"
            stat = "stress"
            path = "root"
            amount = 15
            """;

        var result = EventTomlReader.Read(toml);

        var opt = result.Options[0].Should().BeOfType<SkillCheckOptionDefinition>().Subject;
        opt.StatPath.Should().Be("root");
        opt.StatName.Should().Be("charm");
        opt.Difficulty.Should().Be(10);
        opt.Success.ResolutionTextKey.Should().Be("You fooled them.");
        opt.Failure.ResolutionTextKey.Should().Be("They saw right through you.");
        opt.Failure.Effects.Should().HaveCount(1);
    }

    // ═══════════════════════════════════════════════
    //  Random option
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_RandomOption_MapsWeightedOutcomes()
    {
        const string toml = """
            id = "evt_random"
            title_key = "Roll the Dice"
            description_key = "A random event."
            trigger_type = "pulse"
            mean_time_to_happen_days = 180.0

            [[options]]
            type = "random"
            id = "opt_gamble"
            display_text_key = "Take the gamble"

            [[options.outcomes]]
            weight = 3
            resolution_text_key = "Lucky break!"

            [[options.outcomes]]
            weight = 1
            resolution_text_key = "Bad luck."

            [[options.outcomes.effects]]
            type = "modify_stat"
            stat = "stress"
            path = "root"
            amount = 20
            """;

        var result = EventTomlReader.Read(toml);

        var opt = result.Options[0].Should().BeOfType<RandomOptionDefinition>().Subject;
        opt.Outcomes.Should().HaveCount(2);
        opt.Outcomes[0].Weight.Should().Be(3);
        opt.Outcomes[0].ResolutionTextKey.Should().Be("Lucky break!");
        opt.Outcomes[1].Weight.Should().Be(1);
        opt.Outcomes[1].ResolutionTextKey.Should().Be("Bad luck.");
        opt.Outcomes[1].Effects.Should().HaveCount(1);
    }

    // ═══════════════════════════════════════════════
    //  MTTH modifiers
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_MtthModifiers_MapsCorrectly()
    {
        const string toml = """
            id = "evt_mtth"
            title_key = "MTTH Test"
            description_key = "Has MTTH modifiers."
            trigger_type = "pulse"
            mean_time_to_happen_days = 365.0

            [[mtth_modifiers]]
            factor = 0.5
            [mtth_modifiers.condition]
            type = "has_tag"
            tag = "underboss"
            path = "root"

            [[options]]
            id = "opt_a"
            display_text_key = "OK"

            [options.outcome]
            resolution_text_key = "Done."
            """;

        var result = EventTomlReader.Read(toml);

        var pulse = (PulseEventDefinition)result;
        pulse.MtthModifiers.Should().HaveCount(1);
        pulse.MtthModifiers[0].Factor.Should().Be(0.5);
        pulse.MtthModifiers[0].Condition.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════
    //  Target selection
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_TargetSelection_MapsPoolAndMode()
    {
        const string toml = """
            id = "evt_target"
            title_key = "Target Test"
            description_key = "Has target selection."
            trigger_type = "pulse"
            mean_time_to_happen_days = 365.0

            [target_selection]
            pool = "root_subordinates"
            selection_mode = "random"

            [[options]]
            id = "opt_a"
            display_text_key = "OK"

            [options.outcome]
            resolution_text_key = "Done."
            """;

        var result = EventTomlReader.Read(toml);

        var pulse = (PulseEventDefinition)result;
        pulse.TargetSelection.Should().NotBeNull();
        pulse.TargetSelection!.Pool.Should().Be("root_subordinates");
        pulse.TargetSelection.SelectionMode.Should().Be("random");
    }

    // ═══════════════════════════════════════════════
    //  AI weight modifiers
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_AiWeight_TraitShorthand()
    {
        const string toml = """
            id = "evt_ai"
            title_key = "AI Test"
            description_key = "Has AI weights."
            trigger_type = "pulse"
            mean_time_to_happen_days = 365.0

            [[options]]
            id = "opt_a"
            display_text_key = "Do it"

            [options.outcome]
            resolution_text_key = "Done."

            [options.ai_weight]
            base = 5

            [[options.ai_weight.modifiers]]
            trait = "underboss"
            add = 3
            """;

        var result = EventTomlReader.Read(toml);

        result.Options[0].AiWeight.BaseWeight.Should().Be(5);
        result.Options[0].AiWeight.Modifiers.Should().HaveCount(1);
        result.Options[0].AiWeight.Modifiers[0].Add.Should().Be(3);
        result.Options[0].AiWeight.Modifiers[0].Condition.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════
    //  Visibility conditions
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_VisibilityConditions_MapsCorrectly()
    {
        const string toml = """
            id = "evt_vis"
            title_key = "Visibility Test"
            description_key = "Has visibility conditions."
            trigger_type = "pulse"
            mean_time_to_happen_days = 365.0

            [[options]]
            id = "opt_a"
            display_text_key = "Secret option"

            [options.visibility_conditions]
            type = "has_tag"
            tag = "underboss"
            path = "root"

            [options.outcome]
            resolution_text_key = "Done."
            """;

        var result = EventTomlReader.Read(toml);

        result.Options[0].VisibilityConditions.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════
    //  Deserialize DTO-level check
    // ═══════════════════════════════════════════════

    [Fact]
    public void Deserialize_ReturnsDtoWithCorrectFields()
    {
        const string toml = """
            id = "test"
            title_key = "Test"
            description_key = "Desc"
            trigger_type = "pulse"
            mean_time_to_happen_days = 60.0

            [[options]]
            id = "opt"
            display_text_key = "Go"
            resolution_text_key = "Done."
            """;

        var dto = EventTomlReader.Deserialize(toml);

        dto.Id.Should().Be("test");
        dto.TriggerType.Should().Be("pulse");
        dto.MeanTimeToHappenDays.Should().Be(60.0);
        dto.Options.Should().HaveCount(1);
        dto.Options![0].ResolutionTextKey.Should().Be("Done.");
    }

    // ═══════════════════════════════════════════════
    //  Invalid TOML
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_InvalidToml_Throws()
    {
        const string toml = """
            id = "broken
            this is not valid toml [[[
            """;

        var act = () => EventTomlReader.Read(toml);

        act.Should().Throw<Exception>();
    }

    // ═══════════════════════════════════════════════
    //  Multiple options in one event
    // ═══════════════════════════════════════════════

    [Fact]
    public void Read_MultipleOptions_AllMapped()
    {
        const string toml = """
            id = "evt_multi"
            title_key = "Multi"
            description_key = "Multiple options."
            trigger_type = "pulse"
            mean_time_to_happen_days = 365.0

            [[options]]
            id = "opt_a"
            display_text_key = "Option A"
            resolution_text_key = "Chose A."

            [[options]]
            id = "opt_b"
            display_text_key = "Option B"
            resolution_text_key = "Chose B."
            """;

        var result = EventTomlReader.Read(toml);

        result.Options.Should().HaveCount(2);
        result.Options[0].Id.Should().Be("opt_a");
        result.Options[1].Id.Should().Be("opt_b");
    }
}
