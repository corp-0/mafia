using fennecs;
using Mafia.Core.Events.Engine;
using Mafia.Core.Time;

namespace Mafia.Core.Context;

public sealed partial class EntityScope(World thisWorld)
{
    private readonly Dictionary<string, Entity> _anchors = new();

    public World World => thisWorld;

    public event Action<string, EntityScope?>? ChainedEventTriggered;

    /// <summary>
    /// The current game date at the time of evaluation.
    /// Set by the orchestrator when constructing the scope.
    /// </summary>
    public GameDate CurrentDate { get; init; }

    /// <summary>
    /// Shared event history for querying past event firings.
    /// Set by the orchestrator when constructing the scope.
    /// </summary>
    public IEventHistory? EventHistory { get; init; }


    public EntityScope WithAnchor(string name, Entity entity)
    {
        _anchors[name] = entity;
        return this;
    }

    public Entity? ResolveAnchor(string name)
    {
        return _anchors.TryGetValue(name, out var entity) ? entity : null;
    }

    public bool TryNavigate(string dottedPath, out Entity entity)
    {
        var result = Navigate(dottedPath);
        if (result is { } e)
        {
            entity = e;
            return true;
        }
        entity = default;
        return false;
    }

    public bool TryNavigate(string fromPath, string toPath, out Entity from, out Entity to)
    {
        to = default;
        return TryNavigate(fromPath, out from) && TryNavigate(toPath, out to);
    }

    public Entity? Navigate(ReadOnlySpan<char> dottedPath)
    {
        if (dottedPath.IsEmpty) return null;

        int firstDot = dottedPath.IndexOf('.');
        ReadOnlySpan<char> anchorName = firstDot == -1 ? dottedPath : dottedPath[..firstDot];

        string anchorStr = new string(anchorName);
        if (!_anchors.TryGetValue(anchorStr, out Entity anchorEntity)) return null;

        Entity? current = anchorEntity;

        if (firstDot == -1) return current;

        ReadOnlySpan<char> remainingPath = dottedPath[(firstDot + 1)..];

        while (!remainingPath.IsEmpty)
        {
            int nextDot = remainingPath.IndexOf('.');
            ReadOnlySpan<char> segment = nextDot == -1 ? remainingPath : remainingPath[..nextDot];

            current = FollowRelation(current, segment);

            if (current is null) return null;

            if (nextDot == -1) break;
            remainingPath = remainingPath[(nextDot + 1)..];
        }

        return current;
    }

    public void TriggerChainedEvent(string eventId, EntityScope? newScope = null)
        => ChainedEventTriggered?.Invoke(eventId, newScope);
}