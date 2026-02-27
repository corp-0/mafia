using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class RemoveCustomTag(string normalizedTag, string path) : IEventEffect, IDescribableEffect
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return;
        if (!entity.Has<CustomTags>()) return;

        entity.Ref<CustomTags>().Remove(normalizedTag);
    }

    public Localizable Describe(EntityScope context) =>
        new("effect.remove_trait", new Dictionary<string, object?>
        {
            ["trait"] = normalizedTag
        });
}
