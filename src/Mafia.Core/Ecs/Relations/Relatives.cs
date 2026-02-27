using fennecs;
using Mafia.Core.Ecs.Relations.Interfaces;

namespace Mafia.Core.Ecs.Relations;

public readonly struct FatherOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct MotherOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct SonOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct DaughterOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct BrotherOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct SisterOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct HusbandOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct WifeOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct UncleOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct AuntOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct NephewOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct NieceOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct CousinOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct GrandfatherOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct GrandmotherOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct GrandsonOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct GranddaughterOf(Entity target): IRelation
{
    public Entity Target { get; init; } = target;
}
