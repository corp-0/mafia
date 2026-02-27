using fennecs;
using Mafia.Core.Ecs.Relations.Interfaces;
using Mafia.Core.Opinions;

namespace Mafia.Core.Ecs.Relations;

public readonly struct MemoriesOf(Entity target) : IRelation
{
    public Entity Target { get; init; } = target;
    public List<OpinionMemory> Memories { get; init; } = [];
}