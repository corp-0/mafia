using Jeffijoe.MessageFormat;

namespace Mafia.Core.Text;

public record Localizable(string Key, IReadOnlyDictionary<string, object?> Args)
{
    private static readonly MessageFormatter Formatter = new();

    /// <summary>
    /// Formats the given template using ICU MessageFormat syntax.
    /// Supports <c>{argName}</c> substitution, <c>{gender, select, ...}</c>, <c>{count, plural, ...}</c>, etc.
    /// The template is expected to come from the localization layer (e.g. Godot's <c>Tr(Key)</c>).
    /// </summary>
    public string Format(string template)
    {
        return Formatter.FormatMessage(template, Args);
    }
}