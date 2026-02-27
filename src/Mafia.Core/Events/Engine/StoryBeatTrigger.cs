using fennecs;
using Mafia.Core.Content.Registries;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Events.Definition;
using Mafia.Core.Time;

namespace Mafia.Core.Events.Engine;

public class StoryBeatTrigger(
    IEventDefinitionRepository definitions,
    World world,
    IEventHistory history) : IEventTriggerSource
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

                candidates.Add(new EventCandidate(def, scope));
            }
        }

        return candidates;
    }
}
