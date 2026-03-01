using Mafia.Core.Content.Parsers;
using Mafia.Core.Content.Parsers.Dtos;
using Tomlyn;

namespace Mafia.EventEditor;

/// <summary>
/// Serializes an EventDto to a TOML string using Tomlyn + TomlOptions.
/// </summary>
public static class TomlSerializer
{
    public static string Serialize(EventDto dto)
    {
        return Toml.FromModel(dto, TomlOptions.Default);
    }

    public static EventDto Deserialize(string toml)
    {
        return Toml.ToModel<EventDto>(toml, options: TomlOptions.Default);
    }
}
