using fennecs;
using Mafia.Core.Content.Registries;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Events.Definition;
using Mafia.Core.Events.Engine;
using Mafia.Core.Time;
using Xunit;

namespace Mafia.Core.Tests.Events.Engine;

public class StoryBeatTriggerTests : IDisposable
{
    private readonly World _world = new();
    private readonly EventDefinitionRepository _repo = new();
    private readonly EventHistory _history = new();

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

    private static StoryBeatEventDefinition MakeStoryDef(
        string id = "story_test",
        GameDate? storyDate = null,
        bool oneTime = false) => new()
    {
        Id = id,
        Title = id,
        Description = id,
        Scope = ScopeType.Character,
        StoryDate = storyDate ?? new GameDate(1950, 6, 1),
        IsOneTimeOnly = oneTime,
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
    public void FutureDate_NoCandidates()
    {
        SpawnAlive();

        var def = MakeStoryDef(storyDate: new GameDate(1960, 1, 1));
        _repo.Register(def);

        var trigger = new StoryBeatTrigger(_repo, _world, _history);
        var candidates = trigger.GetCandidates(new GameDate(1950, 6, 15)).ToList();

        Assert.Empty(candidates);
    }

    [Fact]
    public void PastDate_ProducesCandidates()
    {
        var alive = SpawnAlive();

        var def = MakeStoryDef(storyDate: new GameDate(1950, 1, 1));
        _repo.Register(def);

        var trigger = new StoryBeatTrigger(_repo, _world, _history);
        var candidates = trigger.GetCandidates(new GameDate(1950, 6, 15)).ToList();

        Assert.Single(candidates);
        Assert.Equal(alive, candidates[0].Scope.ResolveAnchor("root"));
    }

    [Fact]
    public void ExactDate_ProducesCandidates()
    {
        SpawnAlive();

        var date = new GameDate(1950, 6, 15);
        var def = MakeStoryDef(storyDate: date);
        _repo.Register(def);

        var trigger = new StoryBeatTrigger(_repo, _world, _history);
        var candidates = trigger.GetCandidates(date).ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void OnlyAliveCharacters_ProduceCandidates()
    {
        var alive = SpawnAlive();
        SpawnDisabled();
        _world.Spawn(); // no Character tag

        var def = MakeStoryDef();
        _repo.Register(def);

        var trigger = new StoryBeatTrigger(_repo, _world, _history);
        var candidates = trigger.GetCandidates(new GameDate(1950, 6, 15)).ToList();

        Assert.Single(candidates);
        Assert.Equal(alive, candidates[0].Scope.ResolveAnchor("root"));
    }

    [Fact]
    public void OneTimeStoryBeat_SkipsAlreadyFiredEntities()
    {
        var entity = SpawnAlive();
        var other = SpawnAlive();

        var def = MakeStoryDef(oneTime: true);
        _repo.Register(def);

        // Record that the event already fired for 'entity'
        _history.Record(def.Id, entity, new GameDate(1950, 5, 1));

        var trigger = new StoryBeatTrigger(_repo, _world, _history);
        var candidates = trigger.GetCandidates(new GameDate(1950, 6, 15)).ToList();

        // Only 'other' should produce a candidate
        Assert.Single(candidates);
        Assert.Equal(other, candidates[0].Scope.ResolveAnchor("root"));
    }

    [Fact]
    public void MultipleAliveCharacters_EachGetsCandidates()
    {
        var a = SpawnAlive();
        var b = SpawnAlive();
        var c = SpawnAlive();

        var def = MakeStoryDef();
        _repo.Register(def);

        var trigger = new StoryBeatTrigger(_repo, _world, _history);
        var candidates = trigger.GetCandidates(new GameDate(1950, 6, 15)).ToList();

        Assert.Equal(3, candidates.Count);
        var roots = candidates.Select(x => x.Scope.ResolveAnchor("root")).ToHashSet();
        Assert.Contains(a, roots);
        Assert.Contains(b, roots);
        Assert.Contains(c, roots);
    }
}
