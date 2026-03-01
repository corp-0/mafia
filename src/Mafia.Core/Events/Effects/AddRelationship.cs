using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Relations.Interfaces;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Extensions;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class AddRelationship<TRelation>(string fromPath, string toPath) : IEventEffect, IDescribableEffect
    where TRelation : struct, IRelation
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(fromPath, toPath, out Entity from, out Entity to)) return;
        from.TryAddRelation<TRelation>(to);
    }

    public Localizable Describe(EntityScope context)
    {
        return new Localizable("effect.add_relationship", new Dictionary<string, object?>
        {
            ["relation"] = typeof(TRelation).Name.ToLower()
        });
    }
}