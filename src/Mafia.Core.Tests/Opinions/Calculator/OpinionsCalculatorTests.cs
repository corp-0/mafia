using fennecs;
using FluentAssertions;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Conditions.Interfaces;
using Mafia.Core.Opinions;
using Mafia.Core.Time;
using Xunit;

namespace Mafia.Core.Tests.Opinions.Calculator;

public class OpinionCalculatorTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    private Entity SpawnEntity() => _world.Spawn();

    private static OpinionRuleDefinition Rule(string id, int modifier, string tooltipKey, IEventCondition condition) =>
        new()
        {
            Id = id,
            Modifier = modifier,
            TooltipKey = tooltipKey,
            Conditions = condition
        };

    #region Simple maths

    [Fact]
    public void Opinion_CanGoNegative()
    {
        var rules = new[] { Rule("dislikes_you", -11, "opinion.dislikes_you", new AlwaysTrue()) };
        var calc = new OpinionCalculator(_world, rules);
        var evaluator = SpawnEntity();
        var target = SpawnEntity();
        int score = calc.Calculate(evaluator, target, out _);

        score.Should().Be(OpinionCalculator.BASE_OPINION + -11);
        (score < 0).Should().BeTrue();
    }

    #endregion

    #region Passive Rules

    [Fact]
    public void NoRules_NoMemories_ReturnsZero()
    {
        var calc = new OpinionCalculator(_world, []);
        var evaluator = SpawnEntity();
        var target = SpawnEntity();

        int score = calc.Calculate(evaluator, target, out var tooltips);

        score.Should().Be(OpinionCalculator.BASE_OPINION);
        tooltips.Should().BeEmpty();
    }

    [Fact]
    public void PassiveRule_ConditionMet_AddsModifier()
    {
        var rules = new[] { Rule("likes_everyone", 10, "opinion.likes_everyone", new AlwaysTrue()) };
        var calc = new OpinionCalculator(_world, rules);
        var evaluator = SpawnEntity();
        var target = SpawnEntity();

        int score = calc.Calculate(evaluator, target, out var tooltips);

        score.Should().Be(OpinionCalculator.BASE_OPINION + 10);
        tooltips.Should().HaveCount(1);
        tooltips[0].Key.Should().Be("opinion.likes_everyone");
        tooltips[0].Args["sign"].Should().Be("+");
        tooltips[0].Args["amount"].Should().Be("10");
    }

    [Fact]
    public void PassiveRule_ConditionNotMet_NoEffect()
    {
        var rules = new[] { Rule("hates_everyone", -20, "opinion.hates", new AlwaysFalse()) };
        var calc = new OpinionCalculator(_world, rules);
        var evaluator = SpawnEntity();
        var target = SpawnEntity();

        int score = calc.Calculate(evaluator, target, out var tooltips);

        score.Should().Be(OpinionCalculator.BASE_OPINION);
        tooltips.Should().BeEmpty();
    }

    [Fact]
    public void MultiplePassiveRules_Stack()
    {
        var rules = new[]
        {
            Rule("a", 10, "opinion.a", new AlwaysTrue()),
            Rule("b", -5, "opinion.b", new AlwaysTrue()),
            Rule("c", 20, "opinion.c", new AlwaysFalse())
        };
        var calc = new OpinionCalculator(_world, rules);
        var evaluator = SpawnEntity();
        var target = SpawnEntity();

        int score = calc.Calculate(evaluator, target, out var tooltips);

        score.Should().Be(OpinionCalculator.BASE_OPINION + 5);
        tooltips.Count.Should().Be(2);
    }

    #endregion

    #region Memories

    [Fact]
    public void Memory_AddsToScore()
    {
        var calc = new OpinionCalculator(_world, []);
        var evaluator = SpawnEntity();
        var target = SpawnEntity();

        evaluator.Add(new MemoriesOf(target)
        {
            Memories =
            [
                new OpinionMemory
                {
                    DefinitionId = "betrayed_me",
                    Amount = -30,
                    ExpiresOn = new GameDate(1930, 6, 1)
                }
            ]
        }, target);

        int score = calc.Calculate(evaluator, target, out var tooltips);

        score.Should().Be(OpinionCalculator.BASE_OPINION + -30);
        tooltips.Should().HaveCount(1);
        tooltips[0].Key.Should().Be("betrayed_me");
        tooltips[0].Args["sign"].Should().Be("");
        tooltips[0].Args["amount"].Should().Be("30");
    }

    [Fact]
    public void MultipleMemories_SameTarget_Stack()
    {
        var calc = new OpinionCalculator(_world, []);
        var evaluator = SpawnEntity();
        var target = SpawnEntity();

        evaluator.Add(new MemoriesOf(target)
        {
            Memories =
            [
                new OpinionMemory { DefinitionId = "betrayed_me", Amount = -30, ExpiresOn = new GameDate(1930, 6, 1) },
                new OpinionMemory { DefinitionId = "saved_my_life", Amount = 40, ExpiresOn = new GameDate(1932, 1, 1) }
            ]
        }, target);

        int score = calc.Calculate(evaluator, target, out var tooltips);

        score.Should().Be(OpinionCalculator.BASE_OPINION + 10);
        tooltips.Count.Should().Be(2);
    }

    [Fact]
    public void NoMemories_NoEffect()
    {
        var calc = new OpinionCalculator(_world, []);
        var evaluator = SpawnEntity();
        var target = SpawnEntity();

        int score = calc.Calculate(evaluator, target, out var tooltips);

        score.Should().Be(OpinionCalculator.BASE_OPINION);
        tooltips.Should().BeEmpty();
    }

    [Fact]
    public void MemoriesAboutDifferentTarget_NotIncluded()
    {
        var calc = new OpinionCalculator(_world, []);
        var evaluator = SpawnEntity();
        var target = SpawnEntity();
        var other = SpawnEntity();

        evaluator.Add(new MemoriesOf(other)
        {
            Memories = [new OpinionMemory { DefinitionId = "grudge", Amount = -50, ExpiresOn = new GameDate(1930, 1, 1) }]
        }, other);

        int score = calc.Calculate(evaluator, target, out var tooltips);

        score.Should().Be(OpinionCalculator.BASE_OPINION);
        tooltips.Should().BeEmpty();
    }

    #endregion

    #region Combined

    [Fact]
    public void PassiveRules_And_Memories_CombineCorrectly()
    {
        var rules = new[] { Rule("likes_everyone", 15, "opinion.likes", new AlwaysTrue()) };
        var calc = new OpinionCalculator(_world, rules);
        var evaluator = SpawnEntity();
        var target = SpawnEntity();

        evaluator.Add(new MemoriesOf(target)
        {
            Memories = [new OpinionMemory { DefinitionId = "insulted_me", Amount = -10, ExpiresOn = new GameDate(1930, 1, 1) }]
        }, target);

        int score = calc.Calculate(evaluator, target, out var tooltips);

        score.Should().Be(OpinionCalculator.BASE_OPINION + 5);
        tooltips.Count.Should().Be(2);
    }

    #endregion

    private class AlwaysTrue : IEventCondition
    {
        public bool Evaluate(EntityScope scope) => true;
    }

    private class AlwaysFalse : IEventCondition
    {
        public bool Evaluate(EntityScope scope) => false;
    }
}
