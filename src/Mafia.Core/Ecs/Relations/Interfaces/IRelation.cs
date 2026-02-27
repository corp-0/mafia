using fennecs;

namespace Mafia.Core.Ecs.Relations.Interfaces;

public interface IRelation
{
    Entity Target { get; init; }
}

public interface IFamilyRelation : IRelation;
