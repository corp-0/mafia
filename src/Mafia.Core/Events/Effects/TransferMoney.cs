using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class TransferMoney(string fromPath, string toPath, int amount): IEventEffect, IDescribableEffect
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(fromPath, toPath, out Entity a, out Entity b)) return;
        if (!a.Has<Wealth>() || !b.Has<Wealth>()) return;

        var available = a.Ref<Wealth>().Amount;
        var actual = Math.Max(0, Math.Min(amount, available));

        a.Ref<Wealth>().Amount -= actual;
        b.Ref<Wealth>().Amount += actual;

        var debt = amount - actual;
        if (debt <= 0) return;

        if (a.Has<Owes>(b))
        {
            var existing = a.Ref<Owes>(b).Amount;
            a.Remove<Owes>(b);
            a.Add(new Owes(b) { Amount = existing + debt }, b);
        }
        else
        {
            a.Add(new Owes(b) { Amount = debt }, b);
        }
    }

    public Localizable Describe(EntityScope context)
    {
        var key = fromPath.StartsWith("root")
            ? "effect.transfer_money.lose"
            : "effect.transfer_money.gain";
        return new Localizable(key, new Dictionary<string, object?>
        {
            ["amount"] = amount.ToString()
        });
    }
}