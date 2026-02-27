namespace Mafia.Core.Ecs.Components.Identity;

//TODO: move somewhere when we have more uses of this
public enum Gender
{
    Male,
    Female
}

//TODO: split into components if we are querying them independently in the future.
public record struct Identity(string Name, string NickName, int Age, Gender Gender);

