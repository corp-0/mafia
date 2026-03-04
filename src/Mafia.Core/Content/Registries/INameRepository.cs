namespace Mafia.Core.Content.Registries;

public interface INameRepository
{
    IReadOnlyList<string> MaleNames { get; }
    IReadOnlyList<string> FemaleNames { get; }
    IReadOnlyList<string> Surnames { get; }
    IReadOnlyList<string> GetNicknames(string name);
}
