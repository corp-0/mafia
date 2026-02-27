using Mafia.Core.Context;
using Mafia.Core.Events.Definition;
using Mafia.Core.Events.Engine;
using fennecs;
using Xunit;

namespace Mafia.Core.Tests.Events.Engine;

public class MtthCalculatorTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    private EntityScope CreateScope() => new(_world);

    #region CalculateEffective

    [Fact]
    public void CalculateEffective_NoModifiers_ReturnsBase()
    {
        var calc = new MtthCalculator();
        var result = calc.CalculateEffective(30.0, [], CreateScope());

        Assert.Equal(30.0, result);
    }

    [Fact]
    public void CalculateEffective_PassingModifier_MultipliesFactor()
    {
        var calc = new MtthCalculator();
        var modifiers = new List<MtthModifier>
        {
            new()
            {
                Condition = new AlwaysTrueCondition(),
                Factor = 0.5
            }
        };

        var result = calc.CalculateEffective(30.0, modifiers, CreateScope());

        Assert.Equal(15.0, result);
    }

    [Fact]
    public void CalculateEffective_FailingModifier_IgnoresFactor()
    {
        var calc = new MtthCalculator();
        var modifiers = new List<MtthModifier>
        {
            new()
            {
                Condition = new AlwaysFalseCondition(),
                Factor = 0.5
            }
        };

        var result = calc.CalculateEffective(30.0, modifiers, CreateScope());

        Assert.Equal(30.0, result);
    }

    [Fact]
    public void CalculateEffective_MultipleModifiers_ChainsMultiplication()
    {
        var calc = new MtthCalculator();
        var modifiers = new List<MtthModifier>
        {
            new() { Condition = new AlwaysTrueCondition(), Factor = 0.5 },
            new() { Condition = new AlwaysTrueCondition(), Factor = 2.0 }
        };

        var result = calc.CalculateEffective(30.0, modifiers, CreateScope());

        Assert.Equal(30.0, result); // 30 * 0.5 * 2.0 = 30
    }

    [Fact]
    public void CalculateEffective_FloorAtOneDay()
    {
        var calc = new MtthCalculator();
        var modifiers = new List<MtthModifier>
        {
            new() { Condition = new AlwaysTrueCondition(), Factor = 0.001 }
        };

        var result = calc.CalculateEffective(1.0, modifiers, CreateScope());

        Assert.Equal(1.0, result);
    }

    #endregion

    #region Roll

    [Fact]
    public void Roll_WithZeroRandom_AlwaysSucceeds()
    {
        // Random that always returns 0.0, any probability > 0 should pass
        var calc = new MtthCalculator(new FixedRandom(0.0));

        Assert.True(calc.Roll(30.0, 1.0));
    }

    [Fact]
    public void Roll_WithHighRandom_AlwaysFails()
    {
        // Random that returns 0.999... only 100% probability would pass
        var calc = new MtthCalculator(new FixedRandom(0.999));

        Assert.False(calc.Roll(30.0, 1.0));
    }

    [Fact]
    public void Roll_ProbabilityMath_IsCorrect()
    {
        // P = 1 - e^(-1/30) ≈ 0.03278
        // Random returns 0.03 → should pass (0.03 < 0.03278)
        var calc = new MtthCalculator(new FixedRandom(0.03));
        Assert.True(calc.Roll(30.0, 1.0));

        // Random returns 0.04 → should fail (0.04 > 0.03278)
        calc = new MtthCalculator(new FixedRandom(0.04));
        Assert.False(calc.Roll(30.0, 1.0));
    }

    #endregion
}
