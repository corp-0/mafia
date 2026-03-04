using Mafia.Core.Content.Parsers.Dtos;
using Tomlyn;

namespace Mafia.Core.Content.Parsers;

public static class NamesTomlReader
{
    public static NamesDto Deserialize(string toml)
        => Toml.ToModel<NamesDto>(toml, options: TomlOptions.Default);
}
