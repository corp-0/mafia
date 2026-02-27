using fennecs;
using Mafia.Core.Content.Registries;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Definition;
using Mafia.Core.Time;

namespace Mafia.Core.Events.Engine;

public class PulseTrigger(
    IEventDefinitionRepository definitions,
    World world,
    IEventHistory history,
    TargetPoolResolver poolResolver,
    int bucketCount = 10) : IEventTriggerSource
{
    private readonly Stream<Character> _aliveCharacters =
        world.Query<Character>().Not<Disabled>().Stream();

    private int _currentBucket;

    public IEnumerable<EventCandidate> GetCandidates(GameDate currentDate)
    {
        var pulseDefs = definitions.GetAll<PulseEventDefinition>();
        if (pulseDefs.Count == 0)
            return [];

        _currentBucket = (_currentBucket + 1) % bucketCount;

        var candidates = new List<EventCandidate>();

        foreach (var (entity, _) in _aliveCharacters)
        {
            if (Math.Abs(entity.GetHashCode()) % bucketCount != _currentBucket)
                continue;

            foreach (var def in pulseDefs)
            {
                if (def.Scope != ScopeType.Character)
                    continue;

                var scope = new EntityScope(world)
                {
                    CurrentDate = currentDate,
                    EventHistory = history
                };
                scope.WithAnchor("root", entity);

                if (def.TargetSelection is { } selection)
                {
                    var target = ResolveTarget(selection, entity, scope);
                    if (target is null)
                        continue;

                    scope.WithAnchor("target", target.Value);
                }

                candidates.Add(new EventCandidate(def, scope));
            }
        }

        return candidates;
    }

    private Entity? ResolveTarget(TargetSelection selection, Entity root, EntityScope scope)
    {
        var pool = poolResolver.Resolve(selection.Pool, root);
        if (pool is null || pool.Count == 0)
            return null;

        if (selection.Filter is { } filter)
        {
            var filtered = new List<Entity>();
            foreach (Entity candidate in pool)
            {
                var tempScope = new EntityScope(world)
                {
                    CurrentDate = scope.CurrentDate,
                    EventHistory = scope.EventHistory
                };
                tempScope.WithAnchor("root", candidate);

                if (filter.Evaluate(tempScope))
                    filtered.Add(candidate);
            }

            pool = filtered;
        }

        if (pool.Count == 0)
            return null;

        return selection.SelectionMode switch
        {
            "random" => pool[Random.Shared.Next(pool.Count)],
            _ => pool[0]
        };
    }
}
