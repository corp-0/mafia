using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Events.Definition;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Events.Engine;
using Xunit;

namespace Mafia.Core.Tests.Events.Engine;

public class AiEventResolverTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    private EntityScope CreateScopeWithRoot()
    {
        var scope = new EntityScope(_world);
        var root = _world.Spawn();
        root.Add(new Muscle(5));
        root.Add(new Nerve(7));
        root.Add(new Brains(3));
        root.Add(new Charm(8));
        root.Add(new Instinct(6));
        scope.WithAnchor("root", root);
        return scope;
    }

    private static StandardOptionDefinition MakeStandardOption(string id, int weight = 10) => new()
    {
        Id = id,
        DisplayText = id,
        AiWeight = new AiWeight { BaseWeight = weight },
        Outcome = new EventOutcome { Effects = [] }
    };

    #region Visibility filtering

    [Fact]
    public void GetVisibleOptions_NoConditions_AllVisible()
    {
        var scope = CreateScopeWithRoot();
        var def = MakeDefWithOptions(MakeStandardOption("a"), MakeStandardOption("b"));

        var visible = def.GetVisibleOptions(scope);

        Assert.Equal(2, visible.Count);
    }

    [Fact]
    public void GetVisibleOptions_FailingCondition_Filtered()
    {
        var scope = CreateScopeWithRoot();
        var def = MakeDefWithOptions(
            MakeStandardOption("visible"),
            new StandardOptionDefinition
            {
                Id = "hidden",
                DisplayText = "hidden",
                AiWeight = new AiWeight { BaseWeight = 10 },
                VisibilityConditions = new AlwaysFalseCondition(),
                Outcome = new EventOutcome { Effects = [] }
            });

        var visible = def.GetVisibleOptions(scope);

        Assert.Single(visible);
        Assert.Equal("visible", visible[0].Id);
    }

    private static PulseEventDefinition MakeDefWithOptions(params EventOptionDefinition[] options) => new()
    {
        Id = "visibility_test",
        Title = "Visibility",
        Description = "Visibility",
        MeanTimeToHappenDays = 30,
        Options = options
    };

    #endregion

    #region Weighted selection

    [Fact]
    public void Resolve_SelectsFromVisibleOptions()
    {
        // Fixed random always returns 0, so picks the first option
        var resolver = new AiEventResolver(new FixedRandom(0.0));
        var tracker = new EffectTracker();
        var scope = CreateScopeWithRoot();

        var def = new PulseEventDefinition
        {
            Id = "test",
            Title = "Test",
            Description = "Test",
            MeanTimeToHappenDays = 30,
            Options =
            [
                new StandardOptionDefinition
                {
                    Id = "first",
                    DisplayText = "First",
                    AiWeight = new AiWeight { BaseWeight = 10 },
                    Outcome = new EventOutcome { Effects = [tracker] }
                },
                MakeStandardOption("second", 5)
            ]
        };

        resolver.Resolve(def, scope);

        Assert.Equal(1, tracker.ApplyCount);
    }

    [Fact]
    public void Resolve_AllZeroWeight_NoEffectsApplied()
    {
        var resolver = new AiEventResolver(new FixedRandom(0.0));
        var tracker = new EffectTracker();
        var scope = CreateScopeWithRoot();

        var def = new PulseEventDefinition
        {
            Id = "test",
            Title = "Test",
            Description = "Test",
            MeanTimeToHappenDays = 30,
            Options =
            [
                new StandardOptionDefinition
                {
                    Id = "zero",
                    DisplayText = "Zero",
                    AiWeight = new AiWeight { BaseWeight = 0 },
                    Outcome = new EventOutcome { Effects = [tracker] }
                }
            ]
        };

        resolver.Resolve(def, scope);

        Assert.Equal(0, tracker.ApplyCount);
    }

    #endregion

    #region Standard outcome

    [Fact]
    public void ApplyOptionOutcome_Standard_AppliesEffects()
    {
        var resolver = new AiEventResolver();
        var tracker = new EffectTracker();
        var scope = CreateScopeWithRoot();

        var option = new StandardOptionDefinition
        {
            Id = "opt",
            DisplayText = "opt",
            AiWeight = new AiWeight { BaseWeight = 10 },
            Outcome = new EventOutcome { Effects = [tracker] }
        };

        resolver.ApplyOptionOutcome(option, scope);

        Assert.Equal(1, tracker.ApplyCount);
    }

    #endregion

    #region Skill check outcome

    [Fact]
    public void ApplyOptionOutcome_SkillCheck_Success()
    {
        // FixedRandom: Next(1,7) returns 1, so 2d6 = 1+1 = 2. With muscle=5, total=7.
        // Difficulty 7 → success (>= 7)
        var resolver = new AiEventResolver(new FixedRandom(0.0));
        var successTracker = new EffectTracker();
        var failTracker = new EffectTracker();
        var scope = CreateScopeWithRoot();

        var option = new SkillCheckOptionDefinition
        {
            Id = "skill",
            DisplayText = "skill",
            AiWeight = new AiWeight { BaseWeight = 10 },
            StatPath = "root",
            StatName = "muscle",
            Difficulty = 7,
            Success = new EventOutcome { Effects = [successTracker] },
            Failure = new EventOutcome { Effects = [failTracker] }
        };

        resolver.ApplyOptionOutcome(option, scope);

        Assert.Equal(1, successTracker.ApplyCount);
        Assert.Equal(0, failTracker.ApplyCount);
    }

    [Fact]
    public void ApplyOptionOutcome_SkillCheck_Failure()
    {
        // FixedRandom: 2d6 = 1+1 = 2. muscle=5, total=7.
        // Difficulty 8 → failure (7 < 8)
        var resolver = new AiEventResolver(new FixedRandom(0.0));
        var successTracker = new EffectTracker();
        var failTracker = new EffectTracker();
        var scope = CreateScopeWithRoot();

        var option = new SkillCheckOptionDefinition
        {
            Id = "skill",
            DisplayText = "skill",
            AiWeight = new AiWeight { BaseWeight = 10 },
            StatPath = "root",
            StatName = "muscle",
            Difficulty = 8,
            Success = new EventOutcome { Effects = [successTracker] },
            Failure = new EventOutcome { Effects = [failTracker] }
        };

        resolver.ApplyOptionOutcome(option, scope);

        Assert.Equal(0, successTracker.ApplyCount);
        Assert.Equal(1, failTracker.ApplyCount);
    }

    #endregion

    #region Random outcome

    [Fact]
    public void ApplyOptionOutcome_Random_SelectsWeightedOutcome()
    {
        // FixedRandom: Next(totalWeight) returns 0 → first outcome
        var resolver = new AiEventResolver(new FixedRandom(0.0));
        var tracker1 = new EffectTracker();
        var tracker2 = new EffectTracker();
        var scope = CreateScopeWithRoot();

        var option = new RandomOptionDefinition
        {
            Id = "random",
            DisplayText = "random",
            AiWeight = new AiWeight { BaseWeight = 10 },
            Outcomes =
            [
                new WeightedEventOutcome { Weight = 1, Effects = [tracker1] },
                new WeightedEventOutcome { Weight = 9, Effects = [tracker2] }
            ]
        };

        resolver.ApplyOptionOutcome(option, scope);

        Assert.Equal(1, tracker1.ApplyCount);
        Assert.Equal(0, tracker2.ApplyCount);
    }

    #endregion

    #region Stat resolution

    [Fact]
    public void GetStatValue_ResolvesAllAttributes()
    {
        var scope = CreateScopeWithRoot();

        Assert.Equal(5, AiEventResolver.GetStatValue("muscle", "root", scope));
        Assert.Equal(7, AiEventResolver.GetStatValue("nerve", "root", scope));
        Assert.Equal(3, AiEventResolver.GetStatValue("brains", "root", scope));
        Assert.Equal(8, AiEventResolver.GetStatValue("charm", "root", scope));
        Assert.Equal(6, AiEventResolver.GetStatValue("instinct", "root", scope));
    }

    [Fact]
    public void GetStatValue_UnknownStat_ReturnsZero()
    {
        var scope = CreateScopeWithRoot();

        Assert.Equal(0, AiEventResolver.GetStatValue("nonexistent", "root", scope));
    }

    [Fact]
    public void GetStatValue_CaseInsensitive()
    {
        var scope = CreateScopeWithRoot();

        Assert.Equal(5, AiEventResolver.GetStatValue("MUSCLE", "root", scope));
        Assert.Equal(5, AiEventResolver.GetStatValue("Muscle", "root", scope));
    }

    #endregion
}

internal sealed class EffectTracker : IEventEffect
{
    public int ApplyCount { get; private set; }
    public void Apply(EntityScope scope) => ApplyCount++;
}
