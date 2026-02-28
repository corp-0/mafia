using fennecs;
using Mafia.Core.Ecs.Relations.Interfaces;

namespace Mafia.Core.Ecs.Relations;

public readonly struct FatherOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct MotherOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct SonOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct DaughterOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct BrotherOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct SisterOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct HusbandOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct WifeOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct UncleOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct AuntOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct NephewOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct NieceOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct CousinOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct GrandfatherOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct GrandmotherOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct GrandsonOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct GranddaughterOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct ParentOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct SpouseOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct HalfBrotherOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct HalfSisterOf(Entity target): IFamilyRelation
{
    public Entity Target { get; init; } = target;
}
