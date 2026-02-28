using Mafia.Core.Ecs.Components.Interfaces;

namespace Mafia.Core.Ecs.Components.Identity;

public enum Sex
{
    Male,
    Female
}

public record struct CharacterName(string Name, string NickName);

public record struct Age : IStatComponent
{
    public int Amount { get; set; }
    int IStatComponent.Min => 0;
}

public record struct Surname(string Value);

public record struct OrgName(string Value);

public record struct BirthDay(int Month, int Day);