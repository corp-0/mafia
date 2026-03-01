using Tomlyn;
using Humanizer;

namespace Mafia.Core.Content.Parsers;

public static class TomlOptions
{
    public static readonly TomlModelOptions Default = new()
    {
        ConvertPropertyName = PascalToSnakeCase,
    };

    public static string PascalToSnakeCase(string name) => name.Underscore();
}
