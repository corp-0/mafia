using fennecs;
using Mafia.Core.Content.Registries;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Ecs.Systems;
using Mafia.Core.Events.Definition;
using Mafia.Core.Time;
using Microsoft.Extensions.Logging;

namespace Mafia.Core.Events.Engine;

public class PulseTrigger(
    IEventDefinitionRepository definitions,
    World world,
    IEventHistory history,
    TargetPoolResolver poolResolver,
    ILogger<PulseTrigger> logger,
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
                    var target = poolResolver.ResolveTarget(selection, entity, scope);
                    if (target is null)
                        continue;

                    scope.WithAnchor("target", target.Value);
                }

                candidates.Add(new EventCandidate(def, scope));
            }
        }

        return candidates;
    }
}
