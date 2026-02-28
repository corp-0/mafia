using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects;
using Mafia.Core.Events.Engine;
using Mafia.Core.Text;
using Mafia.Core.Time;

namespace Mafia.Core.Ecs.Systems;

public class ExpenseSettlementSystem(World world) : ITickSystem
{
    public void Tick(GameDate currentDate, IActionTrigger actionTrigger)
    {
        if (currentDate.Day != 1) return;

        var expensesByHousehold = new Dictionary<Entity, List<ExpenseLineItem>>();

        var stream = world.Query<Expense>().Has<ExpenseOf>(Match.Entity).Stream();
        stream.For((in Entity expenseEntity, ref Expense expense) =>
        {
            var relations = expenseEntity.Get<ExpenseOf>(Match.Entity);
            if (relations.Length == 0) return;

            Entity household = relations[0].Target;

            Localizable? label = expenseEntity.Has<ExpenseLabel>()
                ? expenseEntity.Ref<ExpenseLabel>().Label
                : null;

            if (!expensesByHousehold.TryGetValue(household, out var list))
            {
                list = [];
                expensesByHousehold[household] = list;
            }

            list.Add(new ExpenseLineItem(expense.Category, expense.Amount, label));
        });

        foreach (var (household, items) in expensesByHousehold)
        {
            var heads = household.Get<HeadOfHousehold>(Match.Entity);
            if (heads.Length == 0) continue;

            Entity head = heads[0].Target;

            var scope = new EntityScope(world)
                .WithAnchor("root", head)
                .WithMeta("ui_hint", "bills")
                .WithMeta("expenses", items);

            actionTrigger.OnAction("pay_bills", scope);
        }
    }
}
