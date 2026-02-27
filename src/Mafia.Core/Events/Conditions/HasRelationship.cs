using Mafia.Core.Context;
using Mafia.Core.Ecs.Relations.Interfaces;
using Mafia.Core.Events.Conditions.Interfaces;

namespace Mafia.Core.Events.Conditions;

public class HasRelationship<TRelation>(string fromPath, string toPath) : IEventCondition
    where TRelation : struct, IRelation
{
    public bool Evaluate(EntityScope context)
    {
        return context.HasRelation<TRelation>(fromPath, toPath);
    }
}