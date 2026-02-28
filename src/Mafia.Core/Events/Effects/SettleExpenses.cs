using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public record ExpenseLineItem(ExpenseCategory Category, int Amount, Localizable? Label);

public class SettleExpenses(string path) : IEventEffect, IDescribableEffect
{
    private int _total;
    private int _remainingWealth;
    private readonly List<ExpenseLineItem> _items = [];

    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return;

        var households = entity.Get<MemberOfHousehold>(Match.Entity);
        if (households.Length == 0) return;

        Entity householdEntity = households[0].Target;

        var toDelete = new List<Entity>();

        var stream = context.World.Query<Expense>().Has<ExpenseOf>(Match.Entity).Stream();
        stream.For((in Entity expenseEntity, ref Expense expense) =>
        {
            var relations = expenseEntity.Get<ExpenseOf>(Match.Entity);
            if (relations.Length == 0) return;

            if (relations[0].Target != householdEntity) return;

            Localizable? label = expenseEntity.Has<ExpenseLabel>()
                ? expenseEntity.Ref<ExpenseLabel>().Label
                : null;

            _items.Add(new ExpenseLineItem(expense.Category, expense.Amount, label));
            _total += expense.Amount;
            toDelete.Add(expenseEntity);
        });

        if (_total > 0 && entity.Has<Wealth>())
        {
            ref Wealth wealth = ref entity.Ref<Wealth>();
            wealth.Amount = Math.Max(0, wealth.Amount - _total);
            _remainingWealth = wealth.Amount;
        }

        foreach (Entity e in toDelete)
            context.World.Despawn(e);
    }

    public Localizable Describe(EntityScope context) =>
        new("effect.settle_expenses", new Dictionary<string, object?>
        {
            ["amount"] = _total,
            ["remaining_wealth"] = _remainingWealth,
            ["items"] = _items,
        });
}
