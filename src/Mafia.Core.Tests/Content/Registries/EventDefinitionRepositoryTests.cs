using FluentAssertions;
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
        TitleKey = "Test Pulse",
        DescriptionKey = "A pulse event",
        MeanTimeToHappenDays = 30,
        Options = []
    };

    private static ActionEventDefinition MakeAction(string id = "action_1", string actionId = "do_thing") => new()
    {
        Id = id,
        TitleKey = "Test Action",
        DescriptionKey = "An action event",
        OnActionId = actionId,
        Options = []
    };

    private static StoryBeatEventDefinition MakeStoryBeat(string id = "story_1") => new()
    {
        Id = id,
        TitleKey = "Test Story",
        DescriptionKey = "A story beat",
        StoryDate = new GameDate(1950, 1, 1),
        Options = []
    };

    #region Register / GetById

    [Fact]
    public void Register_StoresEventById()
    {
        var pulse = MakePulse();
        _repo.Register(pulse);

        _repo.GetById("pulse_1").Should().BeSameAs(pulse);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        _repo.GetById("nonexistent").Should().BeNull();
    }

    [Fact]
    public void Register_DuplicateId_UpsertsAndReplacesOldEntry()
    {
        var original = MakePulse("dup");
        _repo.Register(original);

        var replacement = new PulseEventDefinition
        {
            Id = "dup",
            TitleKey = "Replaced",
            DescriptionKey = "Replaced event",
            MeanTimeToHappenDays = 99,
            Options = []
        };
        _repo.Register(replacement);

        _repo.GetById("dup").Should().BeSameAs(replacement);
        _repo.GetAll<PulseEventDefinition>().Should().HaveCount(1);
    }

    [Fact]
    public void Register_Upsert_RemovesOldFromActionIndex()
    {
        var original = MakeAction("a1", "attack");
        _repo.Register(original);

        var replacement = new ActionEventDefinition
        {
            Id = "a1",
            TitleKey = "Replaced",
            DescriptionKey = "Replaced",
            OnActionId = "defend",
            Options = []
        };
        _repo.Register(replacement);

        _repo.GetByActionId("attack").Should().BeEmpty();
        _repo.GetByActionId("defend").Should().HaveCount(1);
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

        pulses.Count.Should().Be(2);
    }

    [Fact]
    public void GetAll_ActionEventDefinition_ReturnsOnlyActionEvents()
    {
        _repo.Register(MakeAction("a1", "act1"));
        _repo.Register(MakeAction("a2", "act2"));
        _repo.Register(MakePulse("p1"));

        var actions = _repo.GetAll<ActionEventDefinition>();

        actions.Count.Should().Be(2);
    }

    [Fact]
    public void GetAll_StoryBeatEventDefinition_ReturnsOnlyStoryBeatEvents()
    {
        _repo.Register(MakeStoryBeat("s1"));
        _repo.Register(MakePulse("p1"));

        var stories = _repo.GetAll<StoryBeatEventDefinition>();

        stories.Should().HaveCount(1);
    }

    [Fact]
    public void GetAll_NoEventsRegistered_ReturnsEmptyList()
    {
        var pulses = _repo.GetAll<PulseEventDefinition>();

        pulses.Should().BeEmpty();
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

        attack.Count.Should().Be(2);
    }

    [Fact]
    public void GetByActionId_UnknownAction_ReturnsEmptyList()
    {
        var result = _repo.GetByActionId("nonexistent");

        result.Should().BeEmpty();
    }

    #endregion
}
