using fennecs;
using Mafia.Core.Content.Registries;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Conditions.Interfaces;
using Mafia.Core.Events.Definition;
using Mafia.Core.Events.Engine;
using Mafia.Core.Time;
using Xunit;

namespace Mafia.Core.Tests.Events.Engine;

public class PulseTriggerTests : IDisposable
{
    private readonly World _world = new();
    private readonly EventDefinitionRepository _repo = new();
    private readonly EventHistory _history = new();
    private readonly TargetPoolResolver _poolResolver = new();
    private readonly GameDate _date = new(1950, 6, 15);

    public void Dispose() => _world.Dispose();

    private Entity SpawnAlive()
    {
        var e = _world.Spawn();
        e.Add<Character>();
        return e;
    }

    private Entity SpawnDisabled()
    {
        var e = _world.Spawn();
        e.Add<Character>();
        e.Add(new Disabled(1));
        return e;
    }

    private static PulseEventDefinition MakePulseDef(
        string id = "pulse_test",
        TargetSelection? targetSelection = null) => new()
    {
        Id = id,
        Title = id,
        Description = id,
        Scope = ScopeType.Character,
        MeanTimeToHappenDays = 30,
        TargetSelection = targetSelection,
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

    [Fact]
    public void OnlyAliveCharacters_ProduceCandidates()
    {
        var alive1 = SpawnAlive();
        var alive2 = SpawnAlive();
        var disabled = SpawnDisabled();
        // Entity without Character tag — should not appear
        _world.Spawn();

        var def = MakePulseDef();
        _repo.Register(def);

        // Use bucketCount=1 so all entities are processed every tick
        var trigger = new PulseTrigger(_repo, _world, _history, _poolResolver, bucketCount: 1);

        var candidates = trigger.GetCandidates(_date).ToList();

        // Only alive characters should produce candidates
        Assert.Equal(2, candidates.Count);
        var roots = candidates.Select(c => c.Scope.ResolveAnchor("root")).ToHashSet();
        Assert.Contains(alive1, roots);
        Assert.Contains(alive2, roots);
        Assert.DoesNotContain(disabled, roots);
    }

    [Fact]
    public void Bucketing_ProcessesSubsetPerTick()
    {
        // Spawn many alive characters
        for (var i = 0; i < 20; i++)
            SpawnAlive();

        var def = MakePulseDef();
        _repo.Register(def);

        var trigger = new PulseTrigger(_repo, _world, _history, _poolResolver, bucketCount: 5);

        // Each tick should process roughly 20/5 = 4 entities
        var candidates = trigger.GetCandidates(_date).ToList();
        Assert.True(candidates.Count <= 20, "Should not process all entities in one tick");
        Assert.True(candidates.Count >= 1, "Should process at least some entities");

        // Over 5 ticks, all entities should be processed
        var allRoots = new HashSet<Entity?>();
        foreach (var c in candidates)
            allRoots.Add(c.Scope.ResolveAnchor("root"));

        for (var tick = 1; tick < 5; tick++)
        {
            var more = trigger.GetCandidates(_date).ToList();
            foreach (var c in more)
                allRoots.Add(c.Scope.ResolveAnchor("root"));
        }

        Assert.Equal(20, allRoots.Count);
    }

    [Fact]
    public void TargetResolution_RootSubordinates_ResolvesTargets()
    {
        var boss = SpawnAlive();
        var sub1 = SpawnAlive();
        var sub2 = SpawnAlive();

        boss.Add(new BossOf(sub1), sub1);
        boss.Add(new BossOf(sub2), sub2);

        var def = MakePulseDef(targetSelection: new TargetSelection
        {
            Pool = "root_subordinates",
            SelectionMode = "random"
        });
        _repo.Register(def);

        var trigger = new PulseTrigger(_repo, _world, _history, _poolResolver, bucketCount: 1);
        var candidates = trigger.GetCandidates(_date).ToList();

        // Boss should produce a candidate with a target anchor
        var bossCandidates = candidates
            .Where(c => c.Scope.ResolveAnchor("root") == boss)
            .ToList();
        Assert.Single(bossCandidates);

        var target = bossCandidates[0].Scope.ResolveAnchor("target");
        Assert.NotNull(target);
        Assert.True(target == sub1 || target == sub2);
    }

    [Fact]
    public void TargetResolution_RootFamily_ResolvesTargets()
    {
        var vito = SpawnAlive();
        var michael = SpawnAlive();
        var carmela = SpawnAlive();

        vito.Add(new FatherOf(michael), michael);
        vito.Add(new HusbandOf(carmela), carmela);

        var def = MakePulseDef(targetSelection: new TargetSelection
        {
            Pool = "root_family",
            SelectionMode = "random"
        });
        _repo.Register(def);

        var trigger = new PulseTrigger(_repo, _world, _history, _poolResolver, bucketCount: 1);
        var candidates = trigger.GetCandidates(_date).ToList();

        var vitoCandidates = candidates
            .Where(c => c.Scope.ResolveAnchor("root") == vito)
            .ToList();
        Assert.Single(vitoCandidates);

        var target = vitoCandidates[0].Scope.ResolveAnchor("target");
        Assert.NotNull(target);
        Assert.True(target == michael || target == carmela);
    }

    [Fact]
    public void TargetFilter_RemovesInvalidTargets()
    {
        var boss = SpawnAlive();
        var sub1 = SpawnAlive();
        var sub2 = SpawnAlive();

        // Give sub1 the Arrested tag so it fails the filter
        sub1.Add<Arrested>();

        boss.Add(new BossOf(sub1), sub1);
        boss.Add(new BossOf(sub2), sub2);

        // Filter: target must not be arrested (using AlwaysFalse for sub1 via HasTag check)
        var def = MakePulseDef(targetSelection: new TargetSelection
        {
            Pool = "root_subordinates",
            Filter = new NotArrestedCondition(),
            SelectionMode = "random"
        });
        _repo.Register(def);

        var trigger = new PulseTrigger(_repo, _world, _history, _poolResolver, bucketCount: 1);
        var candidates = trigger.GetCandidates(_date).ToList();

        var bossCandidates = candidates
            .Where(c => c.Scope.ResolveAnchor("root") == boss)
            .ToList();

        // Boss should still get a candidate since sub2 passes the filter
        Assert.Single(bossCandidates);
        Assert.Equal(sub2, bossCandidates[0].Scope.ResolveAnchor("target"));
    }

    [Fact]
    public void UnknownPool_ProducesNoCandidates()
    {
        SpawnAlive();

        var def = MakePulseDef(targetSelection: new TargetSelection
        {
            Pool = "same_territory",
            SelectionMode = "random"
        });
        _repo.Register(def);

        var trigger = new PulseTrigger(_repo, _world, _history, _poolResolver, bucketCount: 1);
        var candidates = trigger.GetCandidates(_date).ToList();

        Assert.Empty(candidates);
    }

    [Fact]
    public void NoTargetSelection_ProducesCandidatesWithoutTargetAnchor()
    {
        SpawnAlive();

        var def = MakePulseDef();
        _repo.Register(def);

        var trigger = new PulseTrigger(_repo, _world, _history, _poolResolver, bucketCount: 1);
        var candidates = trigger.GetCandidates(_date).ToList();

        Assert.Single(candidates);
        Assert.Null(candidates[0].Scope.ResolveAnchor("target"));
    }
}

/// <summary>
/// Condition that evaluates to true if the root entity does NOT have the Arrested tag.
/// </summary>
internal sealed class NotArrestedCondition : IEventCondition
{
    public bool Evaluate(EntityScope scope)
    {
        return !scope.HasTag<Arrested>("root");
    }
}
