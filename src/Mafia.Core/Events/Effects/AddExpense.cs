using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class AddExpense(string path, ExpenseCategory category, int amount, Localizable? label = null)
    : IEventEffect, IDescribableEffect
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return;

        var households = entity.Get<MemberOfHousehold>(Match.Entity);
        if (households.Length == 0) return;

        var householdEntity = households[0].Target;

        var expense = context.World.Spawn()
            .Add(new Expense(category, amount))
            .Add(new ExpenseOf(householdEntity), householdEntity);

        if (label is not null)
            expense.Add(new ExpenseLabel(label));
    }

    public Localizable Describe(EntityScope context)
    {
        return new Localizable("effect.add_expense", new Dictionary<string, object?>
        {
            ["category"] = category.ToString().ToLower(),
            ["amount"] = amount.ToString()
        });
    }
}
