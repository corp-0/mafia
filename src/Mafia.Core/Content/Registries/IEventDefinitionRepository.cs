using Mafia.Core.Events.Definition;

namespace Mafia.Core.Content.Registries;

public interface IEventDefinitionRepository
{
    void Register(EventDefinition definition);
    IReadOnlyList<T> GetAll<T>() where T : EventDefinition;
    IReadOnlyList<ActionEventDefinition> GetByActionId(string actionId);
    EventDefinition? GetById(string id);
}
