using Mafia.Core.Ecs.Components.Interfaces;

namespace Mafia.Core.Ecs.Components.State;

public record struct Wealth : IStatComponent
{
    public int Amount { get; set; }
    int IStatComponent.Min => 0;
}

public record struct Stress : IStatComponent
{
    public int Amount { get; set; }
    int IStatComponent.Min => 0;
    int IStatComponent.Max => 100;
}

public record struct Notoriety : IStatComponent
{
    public int Amount { get; set; }
    int IStatComponent.Min => 0;
    int IStatComponent.Max => 100;
}
