using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Interfaces;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class ModifyStat<TStat>(string path, int amount) : IEventEffect, IDescribableEffect
    where TStat : struct, IStatComponent
{
    public void Apply(EntityScope context)
    {
        var current = context.GetComponent<TStat>(path);
        if (current is not { } stat) return;
        context.SetComponent(path, stat with { Amount = stat.Amount + amount });
    }

    public Localizable Describe(EntityScope context)
    {
        var sign = amount >= 0 ? "+" : "";
        return new Localizable("effect.modify_stat", new Dictionary<string, string>
        {
            ["sign"] = sign,
            ["amount"] = Math.Abs(amount).ToString(),
            ["stat"] = typeof(TStat).Name.ToLower()
        });
    }
}