using Mafia.Core.Context;
using Mafia.Core.Ecs.Relations.Interfaces;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class AddRelationship<TRelation>(string fromPath, string toPath) : IEventEffect, IDescribableEffect
    where TRelation : struct, IRelation
{
    public void Apply(EntityScope context)
    {
        context.AddRelation<TRelation>(fromPath, toPath);
    }

    public Localizable Describe(EntityScope context)
    {
        return new Localizable("effect.add_relationship", new Dictionary<string, string>
        {
            ["relation"] = typeof(TRelation).Name.ToLower()
        });
    }
}