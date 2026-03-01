using fennecs;
using Mafia.Core.Content.Registries;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Ecs.Systems;
using Mafia.Core.Events.Definition;
using Mafia.Core.Time;
using Microsoft.Extensions.Logging;

namespace Mafia.Core.Events.Engine;

public class StoryBeatTrigger(
    IEventDefinitionRepository definitions,
    World world,
    IEventHistory history,
    TargetPoolResolver poolResolver,
    ILogger<StoryBeatTrigger> logger) : IEventTriggerSource
{
    private readonly Stream<Character> _aliveCharacters =
        world.Query<Character>().Not<Disabled>().Stream();

    public IEnumerable<EventCandidate> GetCandidates(GameDate currentDate)
    {
        var storyDefs = definitions.GetAll<StoryBeatEventDefinition>();
        if (storyDefs.Count == 0)
            return [];

        var candidates = new List<EventCandidate>();

        foreach (var def in storyDefs)
        {
            if (currentDate < def.StoryDate)
                continue;

            if (def.Scope != ScopeType.Character)
                continue;

            foreach (var (entity, _) in _aliveCharacters)
            {
                if (def.IsOneTimeOnly && history.HasFired(def.Id, entity))
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
