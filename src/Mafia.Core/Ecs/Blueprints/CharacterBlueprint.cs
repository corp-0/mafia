using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;

namespace Mafia.Core.Ecs.Blueprints;

public record CharacterBlueprint
{
    public required string Name { get; init; }
    public string NickName { get; init; } = "";
    public required int Age { get; init; }
    public required Sex Sex { get; init; }
    public RankId? Rank { get; init; }
    public string Surname { get; init; } = "";
    public int Muscle { get; init; } = 5;
    public int Nerve { get; init; } = 5;
    public int Brains { get; init; } = 5;
    public int Charm { get; init; } = 5;
    public int Instinct { get; init; } = 5;
    public int Wealth { get; init; } = 0;
}
