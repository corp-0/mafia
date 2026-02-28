using fennecs;
using Mafia.Core.Ecs.Relations.Interfaces;

namespace Mafia.Core.Ecs.Relations;

public readonly struct SubordinateOf(Entity target) : IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct BossOf(Entity target) : IRelation
{
    public Entity Target { get; init; } = target;
}

// owner of a business, owner of an operation
public readonly struct OwnerOf(Entity target) : IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct MemberOf(Entity target) : IRelation
{
    public Entity Target { get; init; } = target;
}
