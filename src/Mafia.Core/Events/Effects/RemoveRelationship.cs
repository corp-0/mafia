using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Relations.Interfaces;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Extensions;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class RemoveRelationship<TRelation>(string fromPath, string toPath): IEventEffect, IDescribableEffect
    where TRelation: struct, IRelation
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(fromPath, toPath, out Entity from, out Entity to)) return;
        from.TryRemoveRelation<TRelation>(to);
    }

    public Localizable Describe(EntityScope context)
    {
        return new Localizable("effect.remove_relationship", new Dictionary<string, object?>
        {
            ["relation"] = typeof(TRelation).Name.ToLower()
        });
    }
}
