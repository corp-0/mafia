using Mafia.Core.Ecs.Components.Interfaces;

namespace Mafia.Core.Ecs.Components.State;

public record struct DrinkingUrge: IStatComponent
{
    public int Amount { get; set; }
    int IStatComponent.Min => 0;
    int IStatComponent.Max => 100;
}

public record struct SmokingUrge : IStatComponent
{
    public int Amount { get; set; }
    int IStatComponent.Min => 0;
    int IStatComponent.Max => 100;
}

public record struct HighUrge: IStatComponent
{
    public int Amount { get; set; }
    int IStatComponent.Min => 0;
    int IStatComponent.Max => 100;
}

public record struct GamblingUrge: IStatComponent
{
    public int Amount { get; set; }
    int IStatComponent.Min => 0;
    int IStatComponent.Max => 100;
}

public record struct EatingUrge: IStatComponent
{
    public int Amount { get; set; }
    int IStatComponent.Min => 0;
    int IStatComponent.Max => 100;
}

public record struct LustUrge: IStatComponent
{
    public int Amount { get; set; }
    int IStatComponent.Min => 0;
    int IStatComponent.Max => 100;
}

public record struct ViolenceUrge: IStatComponent
{
    public int Amount { get; set; }
    int IStatComponent.Min => 0;
    int IStatComponent.Max => 100;
}