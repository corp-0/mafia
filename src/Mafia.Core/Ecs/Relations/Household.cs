using fennecs;
using Mafia.Core.Ecs.Relations.Interfaces;

namespace Mafia.Core.Ecs.Relations;

public readonly struct MemberOfHousehold(Entity target) : IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct HeadOfHousehold(Entity target) : IRelation
{
    public Entity Target { get; init; } = target;
}

public readonly struct ExpenseOf(Entity target) : IRelation
{
    public Entity Target { get; init; } = target;
}
