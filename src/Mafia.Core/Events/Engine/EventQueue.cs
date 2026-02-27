using Mafia.Core.Context;
using Mafia.Core.Events.Definition;
using Mafia.Core.Time;

namespace Mafia.Core.Events.Engine;

public class QualifiedEvent(EventDefinition definition, EntityScope scope, int priority, GameDate queuedDate)
{
    public EventDefinition Definition { get; } = definition;
    public EntityScope Scope { get; } = scope;
    public int Priority { get; } = priority;
    public GameDate QueuedDate { get; } = queuedDate;
}

public class EventQueue
{
    private readonly PriorityQueue<QualifiedEvent, int> _queue = new();
    private int _presentedThisTick;

    public int MaxEventsPerTick { get; set; } = 2;

    public void Enqueue(QualifiedEvent qualifiedEvent)
    {
        // Negate priority so higher priority values are dequeued first
        _queue.Enqueue(qualifiedEvent, -qualifiedEvent.Priority);
    }

    public QualifiedEvent? Dequeue()
    {
        if (_queue.Count == 0) return null;
        _presentedThisTick++;
        return _queue.Dequeue();
    }

    public bool HasPendingEvents => _queue.Count > 0;

    public bool CanPresentMore => _presentedThisTick < MaxEventsPerTick && HasPendingEvents;

    public int PendingCount => _queue.Count;

    public void ResetTickCounter() => _presentedThisTick = 0;
}
