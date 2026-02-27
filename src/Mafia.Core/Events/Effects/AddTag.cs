using Mafia.Core.Context;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class AddTag<TTag>(string path) : IEventEffect, IDescribableEffect
    where TTag : struct
{

    public void Apply(EntityScope context)
    {
        context.AddComponent<TTag>(path);
    }

    public Localizable Describe(EntityScope context) =>
        new("effect.add_trait", new Dictionary<string, string>
        {
            ["trait"] = typeof(TTag).Name.ToLower()
        });
}