using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class ModifyWealthPercent(string path, int percent) : IEventEffect, IDescribableEffect
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return;
        if (!entity.Has<Wealth>()) return;

        var current = entity.Ref<Wealth>().Amount;
        var delta = (int)(current * (percent / 100.0));
        entity.Ref<Wealth>().Amount = Math.Max(0, current + delta);
    }

    public Localizable Describe(EntityScope context)
    {
        var sign = percent >= 0 ? "+" : "";
        return new Localizable("effect.modify_wealth_percent", new Dictionary<string, object?>
        {
            ["sign"] = sign,
            ["percent"] = Math.Abs(percent).ToString()
        });
    }
}
