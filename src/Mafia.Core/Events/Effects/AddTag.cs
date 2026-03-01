using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Extensions;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class AddTag<TTag>(string path) : IEventEffect, IDescribableEffect
    where TTag : struct
{

    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return;
        entity.TryAddComponent<TTag>();
    }

    public Localizable Describe(EntityScope context) =>
        new("effect.add_trait", new Dictionary<string, object?>
        {
            ["trait"] = typeof(TTag).Name.ToLower()
        });
}