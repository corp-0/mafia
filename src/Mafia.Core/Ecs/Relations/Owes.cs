using fennecs;
using Mafia.Core.Ecs.Relations.Interfaces;

namespace Mafia.Core.Ecs.Relations;

public readonly struct Owes(Entity target) : IRelation
{
    public Entity Target { get; init; } = target;
    public int Amount { get; init; }
}