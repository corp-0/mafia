using Mafia.Core.Content.Factories;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.Core.Events.Definition;
using Tomlyn;

namespace Mafia.Core.Content.Parsers;

public static class EventTomlReader
{
    public static EventDto Deserialize(string toml)
        => Toml.ToModel<EventDto>(toml, options: TomlOptions.Default);

    public static EventDefinition Read(string toml)
        => EventDefinitionFactory.Create(Deserialize(toml));
}
