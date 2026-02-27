using fennecs;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Relations.Interfaces;
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

    public T? GetComponent<T>(string dottedPath) where T : struct
    {
        Entity? root = Navigate(dottedPath);
        if (root is not { } entity) return null;
        if (!entity.Has<T>()) return null;
        return entity.Ref<T>();
    }

    public bool HasTag<T>(string dottedPath) where T : struct
    {
        Entity? root = Navigate(dottedPath);
        if (root is not { } entity) return false;
        return entity.Has<T>();
    }

    public bool SetComponent<T>(string dottedPath, T value) where T : struct
    {
        Entity? root = Navigate(dottedPath);
        if (root is not { } entity) return false;
        if (!entity.Has<T>()) return false;
        entity.Ref<T>() = value;
        return true;
    }

    public bool AddComponent<T>(string dottedPath, T data = default) where T : struct
    {
        Entity? root = Navigate(dottedPath);
        if (root is not { } entity) return false;
        if (entity.Has<T>()) return false;
        entity.Add(data);
        return true;
    }

    public bool RemoveComponent<T>(string dottedPath) where T : struct
    {
        Entity? root = Navigate(dottedPath);
        if (root is not { } entity) return false;
        if (!entity.Has<T>()) return false;
        entity.Remove<T>();
        return true;
    }

    public bool HasRelation<T>(string fromPath, string toPath) where T : struct, IRelation
    {
        var from = Navigate(fromPath);
        var to = Navigate(toPath);
        if (from is not { } entityA || to is not { } entityB) return false;
        return entityA.Has<T>(entityB);
    }

    public bool AddRelation<T>(string fromPath, string toPath) where T : struct, IRelation
    {
        var from = Navigate(fromPath);
        var to = Navigate(toPath);
        if (from is not { } entityA || to is not { } entityB) return false;
        if (entityA.Has<T>(entityB)) return false;
        entityA.Add(new T { Target = entityB }, entityB);
        return true;
    }

    public bool RemoveRelation<T>(string fromPath, string toPath) where T : struct, IRelation
    {
        var from = Navigate(fromPath);
        var to = Navigate(toPath);
        if (from is not { } entityA || to is not { } entityB) return false;
        if (!entityA.Has<T>(entityB)) return false;
        entityA.Remove<T>(entityB);
        return true;
    }

    public RankId? GetRank(string dottedPath)
    {
        var rank = GetComponent<Rank>(dottedPath);
        return rank?.Id;
    }

    public void TriggerChainedEvent(string eventId, EntityScope? newScope = null)
        => ChainedEventTriggered?.Invoke(eventId, newScope);
}