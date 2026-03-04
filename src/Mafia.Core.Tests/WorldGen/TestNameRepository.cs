using Mafia.Core.Content.Registries;

namespace Mafia.Core.Tests.WorldGen;

internal sealed class TestNameRepository : INameRepository
{
    public IReadOnlyList<string> MaleNames { get; } =
    [
        "Tony", "Sal", "Vinnie", "Marco", "Carlo", "Joe", "Frank", "Nick",
        "Mike", "Danny", "Leo", "Dom", "Gino", "Al", "Pete", "Lou", "Vito",
        "Bruno", "Mario", "Sonny"
    ];

    public IReadOnlyList<string> FemaleNames { get; } =
    [
        "Maria", "Rosa", "Carmela", "Angela", "Lucia", "Gina", "Teresa",
        "Francesca", "Diana", "Elena", "Lisa", "Connie", "Sofia", "Adriana",
        "Vanessa"
    ];

    public IReadOnlyList<string> Surnames { get; } =
    [
        "Corleone", "Soprano", "Luciano", "Gambino", "Genovese", "Colombo",
        "Bonanno", "Profaci", "Galante", "Costello", "Morello", "Rizzo",
        "DeMarco", "Ferraro", "Bianchi", "Romano", "Esposito", "Russo",
        "Lombardi", "Mancini"
    ];

    public IReadOnlyList<string> GetNicknames(string name) => [];
}
