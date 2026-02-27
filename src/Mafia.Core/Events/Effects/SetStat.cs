using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Interfaces;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class SetStat<TStat>(string path, int value) : IEventEffect, IDescribableEffect
    where TStat : struct, IStatComponent
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return;
        if (!entity.Has<TStat>()) return;
        var stat = entity.Ref<TStat>();
        var clamped = Math.Clamp(value, stat.Min, stat.Max);
        entity.Ref<TStat>() = stat with { Amount = clamped };
    }

    public Localizable Describe(EntityScope context) =>
        new("effect.set_stat", new Dictionary<string, object?>
        {
            ["value"] = value.ToString(),
            ["stat"] = typeof(TStat).Name.ToLower()
        });
}
