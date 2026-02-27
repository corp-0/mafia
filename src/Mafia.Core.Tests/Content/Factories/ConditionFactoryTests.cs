using FluentAssertions;
using Mafia.Core.Content.Factories;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Conditions;
using Xunit;

namespace Mafia.Core.Tests.Content.Factories;

public class ConditionFactoryTests
{
    [Fact]
    public void Create_StatThreshold_ReturnsStatThresholdCondition()
    {
        var dto = new ConditionDto { Type = "stat_threshold", Stat = "muscle", Path = "root", Comparison = "gte", Value = 5 };
        var condition = ConditionFactory.Create(dto);
        condition.Should().BeOfType<StatThreshold<Muscle>>();
    }

    [Fact]
    public void Create_HasTag_ReturnsHasTagCondition()
    {
        var dto = new ConditionDto { Type = "has_tag", Tag = "underboss", Path = "root" };
        var condition = ConditionFactory.Create(dto);
        condition.Should().BeOfType<HasTagCondition<Underboss>>();
    }

    [Fact]
    public void Create_HasRelationship_ReturnsHasRelationshipCondition()
    {
        var dto = new ConditionDto { Type = "has_relationship", Kind = "father_of", From = "root", To = "target" };
        var condition = ConditionFactory.Create(dto);
        condition.Should().BeOfType<HasRelationship<FatherOf>>();
    }

    [Fact]
    public void Create_HasMinimumRank_ReturnsHasMinimumRankCondition()
    {
        var dto = new ConditionDto { Type = "has_minimum_rank", Path = "root", Rank = "caporegime" };
        var condition = ConditionFactory.Create(dto);
        condition.Should().BeOfType<HasMinimumRank>();
    }

    [Fact]
    public void Create_SameLocation_ReturnsSameLocationCondition()
    {
        var dto = new ConditionDto { Type = "same_location", PathA = "root", PathB = "target" };
        var condition = ConditionFactory.Create(dto);
        condition.Should().BeOfType<SameLocation>();
    }

    [Fact]
    public void Create_EventFired_ReturnsEventFiredCondition()
    {
        var dto = new ConditionDto { Type = "event_fired", EventId = "evt_betrayal", Path = "root" };
        var condition = ConditionFactory.Create(dto);
        condition.Should().BeOfType<EventFired>();
    }

    [Fact]
    public void Create_EventFired_WithComparison_ReturnsEventFiredCondition()
    {
        var dto = new ConditionDto
        {
            Type = "event_fired", EventId = "evt_betrayal", Path = "root",
            Comparison = "gte", Value = 30
        };
        var condition = ConditionFactory.Create(dto);
        condition.Should().BeOfType<EventFired>();
    }

    [Fact]
    public void Create_AllOf_ReturnsCompositeWithChildren()
    {
        var dto = new ConditionDto
        {
            Type = "all_of",
            Conditions =
            [
                new ConditionDto { Type = "has_tag", Tag = "underboss", Path = "root" },
                new ConditionDto { Type = "stat_threshold", Stat = "muscle", Path = "root", Comparison = "gt", Value = 3 }
            ]
        };
        var condition = ConditionFactory.Create(dto);
        condition.Should().BeOfType<AllOf>();
    }

    [Fact]
    public void Create_AnyOf_ReturnsComposite()
    {
        var dto = new ConditionDto
        {
            Type = "any_of",
            Conditions =
            [
                new ConditionDto { Type = "has_tag", Tag = "consigliere", Path = "root" },
                new ConditionDto { Type = "has_minimum_rank", Path = "root", Rank = "boss" }
            ]
        };
        var condition = ConditionFactory.Create(dto);
        condition.Should().BeOfType<AnyOf>();
    }

    [Fact]
    public void Create_NoneOf_ReturnsComposite()
    {
        var dto = new ConditionDto
        {
            Type = "none_of",
            Conditions =
            [
                new ConditionDto { Type = "has_tag", Tag = "underboss", Path = "root" }
            ]
        };
        var condition = ConditionFactory.Create(dto);
        condition.Should().BeOfType<NoneOf>();
    }

    [Fact]
    public void Create_UnknownType_ThrowsArgumentException()
    {
        var dto = new ConditionDto { Type = "unknown_condition" };
        var act = () => ConditionFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown condition type*");
    }

    [Fact]
    public void Create_HasTag_UnknownTag_ReturnsHasCustomTag()
    {
        var dto = new ConditionDto { Type = "has_tag", Tag = "nonexistent", Path = "root" };
        var condition = ConditionFactory.Create(dto);
        condition.Should().BeOfType<HasCustomTag>();
    }

    [Theory]
    [InlineData("gt", Comparison.GreaterThan)]
    [InlineData(">", Comparison.GreaterThan)]
    [InlineData("greater_than", Comparison.GreaterThan)]
    [InlineData("lt", Comparison.LessThan)]
    [InlineData("<", Comparison.LessThan)]
    [InlineData("less_than", Comparison.LessThan)]
    [InlineData("gte", Comparison.GreaterThanOrEqualTo)]
    [InlineData(">=", Comparison.GreaterThanOrEqualTo)]
    [InlineData("greater_than_or_equal_to", Comparison.GreaterThanOrEqualTo)]
    [InlineData("lte", Comparison.LessThanOrEqualTo)]
    [InlineData("<=", Comparison.LessThanOrEqualTo)]
    [InlineData("less_than_or_equal_to", Comparison.LessThanOrEqualTo)]
    [InlineData("eq", Comparison.Equal)]
    [InlineData("==", Comparison.Equal)]
    [InlineData("equal", Comparison.Equal)]
    public void ParseComparison_AllVariants_ReturnCorrectEnum(string input, Comparison expected)
    {
        ConditionFactory.ParseComparison(input).Should().Be(expected);
    }

    [Fact]
    public void ParseComparison_UnknownValue_ThrowsArgumentException()
    {
        var act = () => ConditionFactory.ParseComparison("bogus");
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown comparison*");
    }

    [Fact]
    public void Create_SnakeCase_Stat_NormalizesCorrectly()
    {
        // Testing that underscores are stripped for normalization
        var dto = new ConditionDto { Type = "stat_threshold", Stat = "notoriety", Path = "root", Comparison = "eq", Value = 50 };
        var condition = ConditionFactory.Create(dto);
        condition.Should().BeOfType<StatThreshold<Mafia.Core.Ecs.Components.State.Notoriety>>();
    }
}
