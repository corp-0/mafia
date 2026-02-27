using fennecs;
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
        if (!context.TryNavigate(path, out Entity entity)) return;
        entity.ModifyComponent<TStat>(s => s with { Amount = s.Amount + amount });
    }

    public Localizable Describe(EntityScope context)
    {
        var sign = amount >= 0 ? "+" : "";
        return new Localizable("effect.modify_stat", new Dictionary<string, object?>
        {
            ["sign"] = sign,
            ["amount"] = Math.Abs(amount).ToString(),
            ["stat"] = typeof(TStat).Name.ToLower()
        });
    }
}