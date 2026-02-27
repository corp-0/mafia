using System.Text;
using Tomlyn;

namespace Mafia.Core.Content.Parsers;

internal static class TomlOptions
{
    internal static readonly TomlModelOptions Default = new()
    {
        ConvertPropertyName = PascalToSnakeCase,
    };

    internal static string PascalToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var sb = new StringBuilder();
        sb.Append(char.ToLowerInvariant(name[0]));

        for (var i = 1; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
