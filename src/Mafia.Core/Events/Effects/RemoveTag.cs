using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class RemoveTag<TTag>(string path) : IEventEffect, IDescribableEffect
    where TTag : struct
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return;
        entity.TryRemoveComponent<TTag>();
    }

    public Localizable Describe(EntityScope context) =>
        new("effect.remove_trait", new Dictionary<string, object?>
        {
            ["trait"] = typeof(TTag).Name.ToLower()
        });
}
