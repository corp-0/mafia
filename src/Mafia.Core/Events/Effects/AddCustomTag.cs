using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class AddCustomTag(string normalizedTag, string path) : IEventEffect, IDescribableEffect
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return;

        if (!entity.Has<CustomTags>())
            entity.Add(new CustomTags());

        entity.Ref<CustomTags>().Add(normalizedTag);
    }

    public Localizable Describe(EntityScope context) =>
        new("effect.add_trait", new Dictionary<string, object?>
        {
            ["trait"] = normalizedTag
        });
}
