using fennecs;
using FluentAssertions;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Events.Definition;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Events.Engine;
using Microsoft.Extensions.Logging.Abstractions;
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
        DisplayTextKey = id,
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

        visible.Count.Should().Be(2);
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
                DisplayTextKey = "hidden",
                AiWeight = new AiWeight { BaseWeight = 10 },
                VisibilityConditions = new AlwaysFalseCondition(),
                Outcome = new EventOutcome { Effects = [] }
            });

        var visible = def.GetVisibleOptions(scope);

        visible.Should().HaveCount(1);
        visible[0].Id.Should().Be("visible");
    }

    private static PulseEventDefinition MakeDefWithOptions(params EventOptionDefinition[] options) => new()
    {
        Id = "visibility_test",
        TitleKey = "Visibility",
        DescriptionKey = "Visibility",
        MeanTimeToHappenDays = 30,
        Options = options
    };

    #endregion

    #region Weighted selection

    [Fact]
    public void Resolve_SelectsFromVisibleOptions()
    {
        // Fixed random always returns 0, so picks the first option
        var resolver = new AiEventResolver(NullLogger<AiEventResolver>.Instance, new FixedRandom(0.0));
        var tracker = new EffectTracker();
        var scope = CreateScopeWithRoot();

        var def = new PulseEventDefinition
        {
            Id = "test",
            TitleKey = "Test",
            DescriptionKey = "Test",
            MeanTimeToHappenDays = 30,
            Options =
            [
                new StandardOptionDefinition
                {
                    Id = "first",
                    DisplayTextKey = "First",
                    AiWeight = new AiWeight { BaseWeight = 10 },
                    Outcome = new EventOutcome { Effects = [tracker] }
                },
                MakeStandardOption("second", 5)
            ]
        };

        resolver.Resolve(def, scope);

        tracker.ApplyCount.Should().Be(1);
    }

    [Fact]
    public void Resolve_AllZeroWeight_NoEffectsApplied()
    {
        var resolver = new AiEventResolver(NullLogger<AiEventResolver>.Instance, new FixedRandom(0.0));
        var tracker = new EffectTracker();
        var scope = CreateScopeWithRoot();

        var def = new PulseEventDefinition
        {
            Id = "test",
            TitleKey = "Test",
            DescriptionKey = "Test",
            MeanTimeToHappenDays = 30,
            Options =
            [
                new StandardOptionDefinition
                {
                    Id = "zero",
                    DisplayTextKey = "Zero",
                    AiWeight = new AiWeight { BaseWeight = 0 },
                    Outcome = new EventOutcome { Effects = [tracker] }
                }
            ]
        };

        resolver.Resolve(def, scope);

        tracker.ApplyCount.Should().Be(0);
    }

    #endregion

    #region Standard outcome

    [Fact]
    public void ApplyOptionOutcome_Standard_AppliesEffects()
    {
        var resolver = new AiEventResolver(NullLogger<AiEventResolver>.Instance);
        var tracker = new EffectTracker();
        var scope = CreateScopeWithRoot();

        var option = new StandardOptionDefinition
        {
            Id = "opt",
            DisplayTextKey = "opt",
            AiWeight = new AiWeight { BaseWeight = 10 },
            Outcome = new EventOutcome { Effects = [tracker] }
        };

        resolver.ApplyOptionOutcome(option, scope);

        tracker.ApplyCount.Should().Be(1);
    }

    #endregion

    #region Skill check outcome

    [Fact]
    public void ApplyOptionOutcome_SkillCheck_Success()
    {
        // FixedRandom: Next(1,7) returns 1, so 2d6 = 1+1 = 2. With muscle=5, total=7.
        // Difficulty 7 → success (>= 7)
        var resolver = new AiEventResolver(NullLogger<AiEventResolver>.Instance, new FixedRandom(0.0));
        var successTracker = new EffectTracker();
        var failTracker = new EffectTracker();
        var scope = CreateScopeWithRoot();

        var option = new SkillCheckOptionDefinition
        {
            Id = "skill",
            DisplayTextKey = "skill",
            AiWeight = new AiWeight { BaseWeight = 10 },
            StatPath = "root",
            StatName = "muscle",
            Difficulty = 7,
            Success = new EventOutcome { Effects = [successTracker] },
            Failure = new EventOutcome { Effects = [failTracker] }
        };

        resolver.ApplyOptionOutcome(option, scope);

        successTracker.ApplyCount.Should().Be(1);
        failTracker.ApplyCount.Should().Be(0);
    }

    [Fact]
    public void ApplyOptionOutcome_SkillCheck_Failure()
    {
        // FixedRandom: 2d6 = 1+1 = 2. muscle=5, total=7.
        // Difficulty 8 → failure (7 < 8)
        var resolver = new AiEventResolver(NullLogger<AiEventResolver>.Instance, new FixedRandom(0.0));
        var successTracker = new EffectTracker();
        var failTracker = new EffectTracker();
        var scope = CreateScopeWithRoot();

        var option = new SkillCheckOptionDefinition
        {
            Id = "skill",
            DisplayTextKey = "skill",
            AiWeight = new AiWeight { BaseWeight = 10 },
            StatPath = "root",
            StatName = "muscle",
            Difficulty = 8,
            Success = new EventOutcome { Effects = [successTracker] },
            Failure = new EventOutcome { Effects = [failTracker] }
        };

        resolver.ApplyOptionOutcome(option, scope);

        successTracker.ApplyCount.Should().Be(0);
        failTracker.ApplyCount.Should().Be(1);
    }

    #endregion

    #region Random outcome

    [Fact]
    public void ApplyOptionOutcome_Random_SelectsWeightedOutcome()
    {
        // FixedRandom: Next(totalWeight) returns 0 → first outcome
        var resolver = new AiEventResolver(NullLogger<AiEventResolver>.Instance, new FixedRandom(0.0));
        var tracker1 = new EffectTracker();
        var tracker2 = new EffectTracker();
        var scope = CreateScopeWithRoot();

        var option = new RandomOptionDefinition
        {
            Id = "random",
            DisplayTextKey = "random",
            AiWeight = new AiWeight { BaseWeight = 10 },
            Outcomes =
            [
                new WeightedEventOutcome { Weight = 1, Effects = [tracker1] },
                new WeightedEventOutcome { Weight = 9, Effects = [tracker2] }
            ]
        };

        resolver.ApplyOptionOutcome(option, scope);

        tracker1.ApplyCount.Should().Be(1);
        tracker2.ApplyCount.Should().Be(0);
    }

    #endregion

    #region Stat resolution

    [Fact]
    public void GetStatValue_ResolvesAllAttributes()
    {
        var scope = CreateScopeWithRoot();

        AiEventResolver.GetStatValue("muscle", "root", scope).Should().Be(5);
        AiEventResolver.GetStatValue("nerve", "root", scope).Should().Be(7);
        AiEventResolver.GetStatValue("brains", "root", scope).Should().Be(3);
        AiEventResolver.GetStatValue("charm", "root", scope).Should().Be(8);
        AiEventResolver.GetStatValue("instinct", "root", scope).Should().Be(6);
    }

    [Fact]
    public void GetStatValue_UnknownStat_ReturnsZero()
    {
        var scope = CreateScopeWithRoot();

        AiEventResolver.GetStatValue("nonexistent", "root", scope).Should().Be(0);
    }

    [Fact]
    public void GetStatValue_CaseInsensitive()
    {
        var scope = CreateScopeWithRoot();

        AiEventResolver.GetStatValue("MUSCLE", "root", scope).Should().Be(5);
        AiEventResolver.GetStatValue("Muscle", "root", scope).Should().Be(5);
    }

    #endregion
}

internal sealed class EffectTracker : IEventEffect
{
    public int ApplyCount { get; private set; }
    public void Apply(EntityScope scope) => ApplyCount++;
}
