using fennecs;
using FluentAssertions;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Text;
using Xunit;

namespace Mafia.Core.Tests.Ecs.Components;

public class ExpenseEntityTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    [Fact]
    public void Expense_StoresCategory()
    {
        var entity = _world.Spawn();
        entity.Add(new Expense(ExpenseCategory.Food, 50));

        entity.Ref<Expense>().Category.Should().Be(ExpenseCategory.Food);
    }

    [Fact]
    public void Expense_StoresAmount()
    {
        var entity = _world.Spawn();
        entity.Add(new Expense(ExpenseCategory.Housing, 100));

        entity.Ref<Expense>().Amount.Should().Be(100);
    }

    [Fact]
    public void ExpenseLabel_StoresLocalizable()
    {
        var entity = _world.Spawn();
        var label = new Localizable("expense.bribe", new Dictionary<string, object?>());
        entity.Add(new ExpenseLabel(label));

        entity.Ref<ExpenseLabel>().Label.Should().Be(label);
    }

    [Fact]
    public void ExpenseOf_LinksToHousehold()
    {
        var household = _world.Spawn();
        household.Add(new Household());

        var expense = _world.Spawn();
        expense.Add(new Expense(ExpenseCategory.Medical, 75));
        expense.Add(new ExpenseOf(household), household);

        var relations = expense.Get<ExpenseOf>(Match.Entity);
        relations.Length.Should().Be(1);
        relations[0].Target.Should().Be(household);
    }

    [Fact]
    public void MultipleExpenses_CanLinkToSameHousehold()
    {
        var household = _world.Spawn();
        household.Add(new Household());

        var e1 = _world.Spawn();
        e1.Add(new Expense(ExpenseCategory.Food, 30));
        e1.Add(new ExpenseOf(household), household);

        var e2 = _world.Spawn();
        e2.Add(new Expense(ExpenseCategory.Housing, 50));
        e2.Add(new ExpenseOf(household), household);

        var stream = _world.Query<Expense>().Stream();
        var total = 0;
        foreach ((Entity _, Expense exp) in stream)
            total += exp.Amount;

        total.Should().Be(80);
    }
}
