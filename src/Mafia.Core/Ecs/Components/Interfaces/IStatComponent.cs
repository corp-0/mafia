namespace Mafia.Core.Ecs.Components.Interfaces;

public interface IStatComponent
{
    int Amount { get; set; }
    int Min => int.MinValue;
    int Max => int.MaxValue;
}
