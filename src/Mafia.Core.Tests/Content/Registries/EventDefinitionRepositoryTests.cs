using Mafia.Core.Content.Registries;
using Mafia.Core.Events.Definition;
using Mafia.Core.Time;
using Xunit;

namespace Mafia.Core.Tests.Content.Registries;

public class EventDefinitionRepositoryTests
{
    private readonly EventDefinitionRepository _repo = new();

    private static PulseEventDefinition MakePulse(string id = "pulse_1") => new()
    {
        Id = id,
        Title = "Test Pulse",
        Description = "A pulse event",
        MeanTimeToHappenDays = 30,
        Options = []
    };

    private static ActionEventDefinition MakeAction(string id = "action_1", string actionId = "do_thing") => new()
    {
        Id = id,
        Title = "Test Action",
        Description = "An action event",
        OnActionId = actionId,
        Options = []
    };

    private static StoryBeatEventDefinition MakeStoryBeat(string id = "story_1") => new()
    {
        Id = id,
        Title = "Test Story",
        Description = "A story beat",
        StoryDate = new GameDate(1950, 1, 1),
        Options = []
    };

    #region Register / GetById

    [Fact]
    public void Register_StoresEventById()
    {
        var pulse = MakePulse();
        _repo.Register(pulse);

        Assert.Same(pulse, _repo.GetById("pulse_1"));
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        Assert.Null(_repo.GetById("nonexistent"));
    }

    [Fact]
    public void Register_DuplicateId_Throws()
    {
        _repo.Register(MakePulse("dup"));

        Assert.Throws<ArgumentException>(() => _repo.Register(MakePulse("dup")));
    }

    #endregion

    #region GetAll<T>

    [Fact]
    public void GetAll_PulseEventDefinition_ReturnsOnlyPulseEvents()
    {
        _repo.Register(MakePulse("p1"));
        _repo.Register(MakePulse("p2"));
        _repo.Register(MakeAction("a1"));

        var pulses = _repo.GetAll<PulseEventDefinition>();

        Assert.Equal(2, pulses.Count);
    }

    [Fact]
    public void GetAll_ActionEventDefinition_ReturnsOnlyActionEvents()
    {
        _repo.Register(MakeAction("a1", "act1"));
        _repo.Register(MakeAction("a2", "act2"));
        _repo.Register(MakePulse("p1"));

        var actions = _repo.GetAll<ActionEventDefinition>();

        Assert.Equal(2, actions.Count);
    }

    [Fact]
    public void GetAll_StoryBeatEventDefinition_ReturnsOnlyStoryBeatEvents()
    {
        _repo.Register(MakeStoryBeat("s1"));
        _repo.Register(MakePulse("p1"));

        var stories = _repo.GetAll<StoryBeatEventDefinition>();

        Assert.Single(stories);
    }

    [Fact]
    public void GetAll_NoEventsRegistered_ReturnsEmptyList()
    {
        var pulses = _repo.GetAll<PulseEventDefinition>();

        Assert.Empty(pulses);
    }

    #endregion

    #region GetByActionId

    [Fact]
    public void GetByActionId_ReturnsMatchingEvents()
    {
        _repo.Register(MakeAction("a1", "attack"));
        _repo.Register(MakeAction("a2", "attack"));
        _repo.Register(MakeAction("a3", "defend"));

        var attack = _repo.GetByActionId("attack");

        Assert.Equal(2, attack.Count);
    }

    [Fact]
    public void GetByActionId_UnknownAction_ReturnsEmptyList()
    {
        var result = _repo.GetByActionId("nonexistent");

        Assert.Empty(result);
    }

    #endregion
}
