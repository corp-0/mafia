using Mafia.Core.Context;
using Mafia.Core.Events.Definition;
using Mafia.Core.Events.Engine;
using Mafia.Core.Time;
using fennecs;
using Xunit;

namespace Mafia.Core.Tests.Events.Engine;

public class EventQueueTests : IDisposable
{
    private readonly World _world = new();
    private readonly EventQueue _queue = new();

    public void Dispose() => _world.Dispose();

    private EntityScope CreateScope() => new(_world);

    private static PulseEventDefinition MakeDef(string id, int priority = 0) => new()
    {
        Id = id,
        Title = id,
        Description = id,
        MeanTimeToHappenDays = 30,
        Options = [],
        Priority = priority
    };

    private QualifiedEvent MakeQueued(string id, int priority = 0) =>
        new(MakeDef(id, priority), CreateScope(), priority, new GameDate(1950, 1, 1));

    #region Priority ordering

    [Fact]
    public void Dequeue_HigherPriorityFirst()
    {
        _queue.Enqueue(MakeQueued("low", 1));
        _queue.Enqueue(MakeQueued("high", 10));
        _queue.Enqueue(MakeQueued("medium", 5));

        var first = _queue.Dequeue();
        var second = _queue.Dequeue();
        var third = _queue.Dequeue();

        Assert.Equal("high", first!.Definition.Id);
        Assert.Equal("medium", second!.Definition.Id);
        Assert.Equal("low", third!.Definition.Id);
    }

    [Fact]
    public void Dequeue_EmptyQueue_ReturnsNull()
    {
        Assert.Null(_queue.Dequeue());
    }

    #endregion

    #region Tick counter

    [Fact]
    public void CanPresentMore_UnderLimit_ReturnsTrue()
    {
        _queue.Enqueue(MakeQueued("a"));
        _queue.ResetTickCounter();

        Assert.True(_queue.CanPresentMore);
    }

    [Fact]
    public void CanPresentMore_AtLimit_ReturnsFalse()
    {
        _queue.MaxEventsPerTick = 2;
        _queue.Enqueue(MakeQueued("a"));
        _queue.Enqueue(MakeQueued("b"));
        _queue.Enqueue(MakeQueued("c"));
        _queue.ResetTickCounter();

        _queue.Dequeue();
        _queue.Dequeue();

        Assert.False(_queue.CanPresentMore);
    }

    [Fact]
    public void ResetTickCounter_AllowsMoreDequeues()
    {
        _queue.MaxEventsPerTick = 1;
        _queue.Enqueue(MakeQueued("a"));
        _queue.Enqueue(MakeQueued("b"));
        _queue.ResetTickCounter();

        _queue.Dequeue();
        Assert.False(_queue.CanPresentMore);

        _queue.ResetTickCounter();
        Assert.True(_queue.CanPresentMore);
    }

    #endregion

    #region Pending count

    [Fact]
    public void PendingCount_ReflectsQueueSize()
    {
        Assert.Equal(0, _queue.PendingCount);

        _queue.Enqueue(MakeQueued("a"));
        _queue.Enqueue(MakeQueued("b"));
        Assert.Equal(2, _queue.PendingCount);

        _queue.Dequeue();
        Assert.Equal(1, _queue.PendingCount);
    }

    [Fact]
    public void HasPendingEvents_EmptyQueue_ReturnsFalse()
    {
        Assert.False(_queue.HasPendingEvents);
    }

    [Fact]
    public void HasPendingEvents_WithItems_ReturnsTrue()
    {
        _queue.Enqueue(MakeQueued("a"));

        Assert.True(_queue.HasPendingEvents);
    }

    #endregion
}
