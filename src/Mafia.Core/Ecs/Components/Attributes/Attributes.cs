using Mafia.Core.Ecs.Components.Interfaces;

namespace Mafia.Core.Ecs.Components.Attributes;

public record struct Muscle(int Amount) : IStatComponent
{
    int IStatComponent.Min => 1;
    int IStatComponent.Max => 10;
}

public record struct Nerve(int Amount) : IStatComponent
{
    int IStatComponent.Min => 1;
    int IStatComponent.Max => 10;
}

public record struct Brains(int Amount) : IStatComponent
{
    int IStatComponent.Min => 1;
    int IStatComponent.Max => 10;
}

public record struct Charm(int Amount) : IStatComponent
{
    int IStatComponent.Min => 1;
    int IStatComponent.Max => 10;
}

public record struct Instinct(int Amount) : IStatComponent
{
    int IStatComponent.Min => 1;
    int IStatComponent.Max => 10;
}