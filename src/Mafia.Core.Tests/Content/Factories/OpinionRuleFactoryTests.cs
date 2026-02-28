using FluentAssertions;
using Mafia.Core.Content.Factories;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.Core.Events.Conditions;
using Mafia.Core.Events.Conditions.Interfaces;
using Xunit;

namespace Mafia.Core.Tests.Content.Factories;

public class OpinionRuleFactoryTests
{
    private static OpinionRuleDto MakeMinimalDto() => new()
    {
        Id = "opinion_same_family",
        Modifier = 20,
        TooltipKey = "opinion.same_family",
        Conditions = new ConditionDto { Type = "has_tag", Tag = "family_member", Path = "target" },
    };

    // ═══════════════════════════════════════════════
    //  Field mapping
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_MapsId()
    {
        var result = OpinionRuleFactory.Create(MakeMinimalDto());
        result.Id.Should().Be("opinion_same_family");
    }

    [Fact]
    public void Create_MapsModifier()
    {
        var result = OpinionRuleFactory.Create(MakeMinimalDto());
        result.Modifier.Should().Be(20);
    }

    [Fact]
    public void Create_MapsTooltipKey()
    {
        var result = OpinionRuleFactory.Create(MakeMinimalDto());
        result.TooltipKey.Should().Be("opinion.same_family");
    }

    [Fact]
    public void Create_MapsCondition()
    {
        var result = OpinionRuleFactory.Create(MakeMinimalDto());
        result.Conditions.Should().NotBeNull();
        result.Conditions.Should().BeAssignableTo<IEventCondition>();
    }

    [Fact]
    public void Create_NegativeModifier_MapsCorrectly()
    {
        var dto = MakeMinimalDto();
        dto.Modifier = -15;

        var result = OpinionRuleFactory.Create(dto);
        result.Modifier.Should().Be(-15);
    }

    // ═══════════════════════════════════════════════
    //  Composite conditions
    // ═══════════════════════════════════════════════

    [Fact]
    public void Create_CompositeCondition_MapsCorrectly()
    {
        var dto = MakeMinimalDto();
        dto.Conditions = new ConditionDto
        {
            Type = "all_of",
            Conditions =
            [
                new ConditionDto { Type = "has_tag", Tag = "underboss", Path = "root" },
                new ConditionDto { Type = "stat_threshold", Stat = "muscle", Path = "root", Comparison = "gte", Value = 5 },
            ],
        };

        var result = OpinionRuleFactory.Create(dto);
        result.Conditions.Should().BeOfType<AllOf>();
    }
}
