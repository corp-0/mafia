namespace Mafia.Core.Text;

public record Localizable(string Key, IReadOnlyDictionary<string, string> Args);