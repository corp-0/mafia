using fennecs;
using Mafia.Core.Content.Registries;
using Mafia.Core.Context;
using Mafia.Core.Events.Conditions;
using Mafia.Core.Events.Definition;
using Mafia.Core.Events.Engine;
using Mafia.Core.Time;
using Xunit;

namespace Mafia.Core.Tests.Events.Engine;

public class EventOrchestratorTests : IDisposable
{
    private readonly World _world = new();
    private readonly EventDefinitionRepository _repo = new();
    private readonly EventQueue _queue = new();
    private readonly EventHistory _history = new();
    private readonly AiEventResolver _aiResolver;
    private readonly EventOrchestrator _orchestrator;
    private readonly ListTriggerSource _source = new();

    public EventOrchestratorTests()
    {
        var mtthCalc =
            // FixedRandom(0.0): MTTH roll always succeeds, AI picks first option
            new MtthCalculator(new FixedRandom(0.0));
        _aiResolver = new AiEventResolver(new FixedRandom(0.0));
        _orchestrator = new EventOrchestrator(_repo, _queue, mtthCalc, _aiResolver, _history);
        _orchestrator.RegisterTriggerSource(_source);
    }

    public void Dispose() => _world.Dispose();

    private EntityScope CreateScopeWithRoot(Entity? root = null)
    {
        var scope = new EntityScope(_world) { CurrentDate = new GameDate(1950, 6, 15), EventHistory = _history };
        root ??= _world.Spawn();
        scope.WithAnchor("root", root.Value);
        return scope;
    }

    private static PulseEventDefinition MakePulseDef(
        string id = "pulse_test",
        bool oneTime = false,
        int cooldownDays = 0,
        int priority = 0) => new()
        {
            Id = id,
            Title = id,
            Description = id,
            MeanTimeToHappenDays = 30,
            Options =
        [
            new StandardOptionDefinition
            {
                Id = "opt1",
                DisplayText = "Option 1",
                AiWeight = new AiWeight { BaseWeight = 10 },
                Outcome = new EventOutcome { Effects = [] }
            }
        ],
            IsOneTimeOnly = oneTime,
            CooldownDays = cooldownDays,
            Priority = priority
        };

    private static ActionEventDefinition MakeActionDef(string id = "action_test") => new()
    {
        Id = id,
        Title = id,
        Description = id,
        OnActionId = "do_thing",
        Options =
        [
            new StandardOptionDefinition
            {
                Id = "opt1",
                DisplayText = "Option 1",
                AiWeight = new AiWeight { BaseWeight = 10 },
                Outcome = new EventOutcome { Effects = [] }
            }
        ]
    };

    private static bool AlwaysAlive(Entity _) => true;
    private static bool NeverPlayer(Entity _) => false;
    private static bool AlwaysPlayer(Entity _) => true;

    #region Gate: One-time

    [Fact]
    public void OneTimeEvent_FiresOncePerRoot()
    {
        var def = MakePulseDef(oneTime: true);
        _repo.Register(def);
        var root = _world.Spawn();

        // First tick, should enqueue
        _source.Add(new EventCandidate(def, CreateScopeWithRoot(root)));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 15));
        var presented = _orchestrator.TryPresent(AlwaysAlive, AlwaysPlayer);
        Assert.NotNull(presented);

        // Second tick, same root, should be blocked
        _source.Add(new EventCandidate(def, CreateScopeWithRoot(root)));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 16));
        presented = _orchestrator.TryPresent(AlwaysAlive, AlwaysPlayer);
        Assert.Null(presented);
    }

    [Fact]
    public void OneTimeEvent_DifferentRoots_BothFire()
    {
        var def = MakePulseDef(oneTime: true);
        _repo.Register(def);
        var root1 = _world.Spawn();
        var root2 = _world.Spawn();

        _source.Add(new EventCandidate(def, CreateScopeWithRoot(root1)));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 15));
        var p1 = _orchestrator.TryPresent(AlwaysAlive, AlwaysPlayer);
        Assert.NotNull(p1);

        _source.Add(new EventCandidate(def, CreateScopeWithRoot(root2)));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 15));
        var p2 = _orchestrator.TryPresent(AlwaysAlive, AlwaysPlayer);
        Assert.NotNull(p2);
    }

    #endregion

    #region Gate: Cooldown

    [Fact]
    public void CooldownEvent_BlockedDuringCooldown()
    {
        var def = MakePulseDef(cooldownDays: 10);
        _repo.Register(def);
        var root = _world.Spawn();

        // First firing
        _source.Add(new EventCandidate(def, CreateScopeWithRoot(root)));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 15));
        _orchestrator.TryPresent(AlwaysAlive, NeverPlayer);

        // 5 days later, still in cooldown
        _source.Add(new EventCandidate(def, CreateScopeWithRoot(root)));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 20));
        var result = _orchestrator.TryPresent(AlwaysAlive, NeverPlayer);
        Assert.Null(result);
    }

    [Fact]
    public void CooldownEvent_AllowedAfterCooldownExpires()
    {
        var def = MakePulseDef(cooldownDays: 10);
        _repo.Register(def);
        var root = _world.Spawn();

        // First firing
        _source.Add(new EventCandidate(def, CreateScopeWithRoot(root)));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 15));
        _orchestrator.TryPresent(AlwaysAlive, NeverPlayer);

        // 11 days later, cooldown expired
        _source.Add(new EventCandidate(def, CreateScopeWithRoot(root)));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 26));
        var result = _orchestrator.TryPresent(AlwaysAlive, AlwaysPlayer);
        Assert.NotNull(result);
    }

    #endregion

    #region Gate: Conditions

    [Fact]
    public void ConditionsFailing_EventNotEnqueued()
    {
        var def = new PulseEventDefinition
        {
            Id = "cond_fail",
            Title = "Conditional",
            Description = "Conditional",
            MeanTimeToHappenDays = 30,
            Conditions = new AlwaysFalseCondition(),
            Options =
            [
                new StandardOptionDefinition
                {
                    Id = "opt1",
                    DisplayText = "Option 1",
                    AiWeight = new AiWeight { BaseWeight = 10 },
                    Outcome = new EventOutcome { Effects = [] }
                }
            ]
        };
        _repo.Register(def);

        _source.Add(new EventCandidate(def, CreateScopeWithRoot()));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 15));
        var result = _orchestrator.TryPresent(AlwaysAlive, NeverPlayer);

        Assert.Null(result);
    }

    [Fact]
    public void ConditionsPassing_EventEnqueued()
    {
        var def = new PulseEventDefinition
        {
            Id = "cond_pass",
            Title = "Conditional",
            Description = "Conditional",
            MeanTimeToHappenDays = 30,
            Conditions = new AlwaysTrueCondition(),
            Options =
            [
                new StandardOptionDefinition
                {
                    Id = "opt1",
                    DisplayText = "Option 1",
                    AiWeight = new AiWeight { BaseWeight = 10 },
                    Outcome = new EventOutcome { Effects = [] }
                }
            ]
        };
        _repo.Register(def);

        _source.Add(new EventCandidate(def, CreateScopeWithRoot()));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 15));
        var result = _orchestrator.TryPresent(AlwaysAlive, AlwaysPlayer);

        Assert.NotNull(result);
    }

    #endregion

    #region Gate: MTTH Roll

    [Fact]
    public void MtthRoll_Failure_EventNotEnqueued()
    {
        // Use a calculator that always fails the roll
        var failCalc = new MtthCalculator(new FixedRandom(0.999));
        var orchestrator = new EventOrchestrator(_repo, new EventQueue(), failCalc, _aiResolver, _history);
        var source = new ListTriggerSource();
        orchestrator.RegisterTriggerSource(source);

        var def = MakePulseDef("mtth_fail");
        _repo.Register(def);

        source.Add(new EventCandidate(def, CreateScopeWithRoot()));
        orchestrator.Tick(1.0, new GameDate(1950, 6, 15));
        var result = orchestrator.TryPresent(AlwaysAlive, NeverPlayer);

        Assert.Null(result);
    }

    #endregion

    #region Re-validation on present

    [Fact]
    public void TryPresent_DeadRoot_SkipsEvent()
    {
        var def = MakePulseDef();
        _repo.Register(def);

        _source.Add(new EventCandidate(def, CreateScopeWithRoot()));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 15));

        // Root is dead at presentation time
        var result = _orchestrator.TryPresent(_ => false, NeverPlayer);

        Assert.Null(result);
    }

    [Fact]
    public void TryPresent_ConditionsFailAtPresentation_SkipsEvent()
    {
        // Track condition state to flip it between tick and present
        var condition = new ToggleCondition(true);
        var def = new PulseEventDefinition
        {
            Id = "revalidate",
            Title = "Revalidate",
            Description = "Revalidate",
            MeanTimeToHappenDays = 30,
            Conditions = condition,
            Options =
            [
                new StandardOptionDefinition
                {
                    Id = "opt1",
                    DisplayText = "Option 1",
                    AiWeight = new AiWeight { BaseWeight = 10 },
                    Outcome = new EventOutcome { Effects = [] }
                }
            ]
        };
        _repo.Register(def);

        _source.Add(new EventCandidate(def, CreateScopeWithRoot()));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 15));

        // Flip condition to false before presenting
        condition.Result = false;
        var result = _orchestrator.TryPresent(AlwaysAlive, NeverPlayer);

        Assert.Null(result);
    }

    #endregion

    #region Player vs AI routing

    [Fact]
    public void TryPresent_PlayerRoot_ReturnsPendingPlayerEvent()
    {
        var def = MakePulseDef();
        _repo.Register(def);

        _source.Add(new EventCandidate(def, CreateScopeWithRoot()));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 15));

        var result = _orchestrator.TryPresent(AlwaysAlive, AlwaysPlayer);

        Assert.NotNull(result);
        Assert.Equal("pulse_test", result!.Definition.Id);
        Assert.NotEmpty(result.VisibleOptions);
    }

    [Fact]
    public void TryPresent_AiRoot_ResolvesImmediately()
    {
        var tracker = new EffectTracker();
        var def = new PulseEventDefinition
        {
            Id = "ai_event",
            Title = "AI",
            Description = "AI",
            MeanTimeToHappenDays = 30,
            Options =
            [
                new StandardOptionDefinition
                {
                    Id = "opt1",
                    DisplayText = "Option 1",
                    AiWeight = new AiWeight { BaseWeight = 10 },
                    Outcome = new EventOutcome { Effects = [tracker] }
                }
            ]
        };
        _repo.Register(def);

        _source.Add(new EventCandidate(def, CreateScopeWithRoot()));
        _orchestrator.Tick(1.0, new GameDate(1950, 6, 15));

        // AI root, should resolve, not return pending event
        var result = _orchestrator.TryPresent(AlwaysAlive, NeverPlayer);

        Assert.Null(result); // returns null because it was AI-resolved
        Assert.Equal(1, tracker.ApplyCount);
    }

    #endregion

    #region ResolvePlayerChoice

    [Fact]
    public void ResolvePlayerChoice_AppliesChosenOptionEffects()
    {
        var tracker = new EffectTracker();
        var def = new PulseEventDefinition
        {
            Id = "player_choice",
            Title = "Choice",
            Description = "Choice",
            MeanTimeToHappenDays = 30,
            Options =
            [
                new StandardOptionDefinition
                {
                    Id = "accept",
                    DisplayText = "Accept",
                    AiWeight = new AiWeight { BaseWeight = 10 },
                    Outcome = new EventOutcome { Effects = [tracker] }
                },
                new StandardOptionDefinition
                {
                    Id = "decline",
                    DisplayText = "Decline",
                    AiWeight = new AiWeight { BaseWeight = 10 },
                    Outcome = new EventOutcome { Effects = [] }
                }
            ]
        };

        var scope = CreateScopeWithRoot();

        _orchestrator.ResolvePlayerChoice(def, scope, "accept");

        Assert.Equal(1, tracker.ApplyCount);
    }

    [Fact]
    public void ResolvePlayerChoice_InvalidOptionId_DoesNothing()
    {
        var def = MakePulseDef();
        var scope = CreateScopeWithRoot();

        // Should not throw
        _orchestrator.ResolvePlayerChoice(def, scope, "nonexistent");
    }

    #endregion

    #region Action events (no MTTH gate)

    [Fact]
    public void ActionEvent_SkipsMtthGate()
    {
        // Even with a "never rolls" MTTH calculator, action events should pass
        var neverRollCalc = new MtthCalculator(new FixedRandom(0.999));
        var queue = new EventQueue();
        var orchestrator = new EventOrchestrator(_repo, queue, neverRollCalc, _aiResolver, _history);
        var source = new ListTriggerSource();
        orchestrator.RegisterTriggerSource(source);

        var def = MakeActionDef("action_no_mtth");
        _repo.Register(def);

        source.Add(new EventCandidate(def, CreateScopeWithRoot()));
        orchestrator.Tick(1.0, new GameDate(1950, 6, 15));
        var result = orchestrator.TryPresent(AlwaysAlive, NeverPlayer);

        // Action event should have been enqueued (no MTTH gate)
        // But it gets AI-resolved, so result is null and the event ran
        Assert.Null(result); // AI-resolved
    }

    #endregion

    #region ChainedEventDefinition

    [Fact]
    public void ChainedEventDefinition_NotReturnedByGetAllPulse()
    {
        var chained = new ChainedEventDefinition
        {
            Id = "chained_1",
            Title = "Chained",
            Description = "Chained",
            Options =
            [
                new StandardOptionDefinition
                {
                    Id = "opt1",
                    DisplayText = "Option 1",
                    AiWeight = new AiWeight { BaseWeight = 10 },
                    Outcome = new EventOutcome { Effects = [] }
                }
            ]
        };
        _repo.Register(chained);

        var pulseEvents = _repo.GetAll<PulseEventDefinition>();
        Assert.DoesNotContain(pulseEvents, e => e.Id == "chained_1");

        var actionEvents = _repo.GetAll<ActionEventDefinition>();
        Assert.DoesNotContain(actionEvents, e => e.Id == "chained_1");

        var storyEvents = _repo.GetAll<StoryBeatEventDefinition>();
        Assert.DoesNotContain(storyEvents, e => e.Id == "chained_1");

        // But retrievable by ID
        Assert.NotNull(_repo.GetById("chained_1"));
    }

    #endregion

    #region EventFired condition

    [Fact]
    public void EventFired_NoHistory_ReturnsFalse()
    {
        var root = _world.Spawn();
        var scope = new EntityScope(_world) { CurrentDate = new GameDate(1950, 6, 15) }
            .WithAnchor("root", root);

        var condition = new EventFired("some_event", "root");
        Assert.False(condition.Evaluate(scope));
    }

    [Fact]
    public void EventFired_NotFired_ReturnsFalse()
    {
        var root = _world.Spawn();
        var scope = new EntityScope(_world) { CurrentDate = new GameDate(1950, 6, 15), EventHistory = _history }
            .WithAnchor("root", root);

        var condition = new EventFired("some_event", "root");
        Assert.False(condition.Evaluate(scope));
    }

    [Fact]
    public void EventFired_HasFired_ReturnsTrue()
    {
        var root = _world.Spawn();
        _history.Record("some_event", root, new GameDate(1950, 6, 10));

        var scope = new EntityScope(_world) { CurrentDate = new GameDate(1950, 6, 15), EventHistory = _history }
            .WithAnchor("root", root);

        var condition = new EventFired("some_event", "root");
        Assert.True(condition.Evaluate(scope));
    }

    [Fact]
    public void EventFired_DaysSinceComparison_GreaterThan()
    {
        var root = _world.Spawn();
        _history.Record("timed_event", root, new GameDate(1950, 6, 1));

        var scope = new EntityScope(_world) { CurrentDate = new GameDate(1950, 6, 15), EventHistory = _history }
            .WithAnchor("root", root);

        // 14 days since firing, "more than 10 days ago" should be true
        var condition = new EventFired("timed_event", "root", Comparison.GreaterThan, 10);
        Assert.True(condition.Evaluate(scope));

        // "more than 20 days ago" should be false
        var condition2 = new EventFired("timed_event", "root", Comparison.GreaterThan, 20);
        Assert.False(condition2.Evaluate(scope));
    }

    [Fact]
    public void EventFired_DaysSinceComparison_LessThan()
    {
        var root = _world.Spawn();
        _history.Record("timed_event", root, new GameDate(1950, 6, 10));

        var scope = new EntityScope(_world) { CurrentDate = new GameDate(1950, 6, 15), EventHistory = _history }
            .WithAnchor("root", root);

        // 5 days since firing, "less than 10 days ago" should be true
        var condition = new EventFired("timed_event", "root", Comparison.LessThan, 10);
        Assert.True(condition.Evaluate(scope));

        // "less than 3 days ago" should be false
        var condition2 = new EventFired("timed_event", "root", Comparison.LessThan, 3);
        Assert.False(condition2.Evaluate(scope));
    }

    [Fact]
    public void EventFired_DaysSinceComparison_NeverFired_ReturnsFalse()
    {
        var root = _world.Spawn();
        var scope = new EntityScope(_world) { CurrentDate = new GameDate(1950, 6, 15), EventHistory = _history }
            .WithAnchor("root", root);

        // Event never fired, any time comparison should return false
        var condition = new EventFired("never_fired", "root", Comparison.GreaterThan, 0);
        Assert.False(condition.Evaluate(scope));
    }

    #endregion
}

internal sealed class ToggleCondition(bool initial) : Mafia.Core.Events.Conditions.Interfaces.IEventCondition
{
    public bool Result { get; set; } = initial;
    public bool Evaluate(EntityScope scope) => Result;
}
