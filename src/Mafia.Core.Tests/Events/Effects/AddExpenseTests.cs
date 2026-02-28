using fennecs;
using FluentAssertions;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects;
using Mafia.Core.Text;
using Xunit;

namespace Mafia.Core.Tests.Events.Effects;

public class AddExpenseTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    private EntityScope CreateScope() => new(_world);

    private Entity SpawnCharacterInHousehold(out Entity household)
    {
        var character = _world.Spawn();
        household = _world.Spawn();
        household.Add(new Household());
        character.Add(new MemberOfHousehold(household), household);
        return character;
    }

    private List<Expense> GetExpensesFor(Entity household)
    {
        var expenses = new List<Expense>();
        var stream = _world.Query<Expense>().Stream();
        foreach ((Entity expenseEntity, Expense expense) in stream)
        {
            var relations = expenseEntity.Get<ExpenseOf>(Match.Entity);
            if (relations.Length > 0 && relations[0].Target == household)
                expenses.Add(expense);
        }
        return expenses;
    }

    [Fact]
    public void Apply_WithLabel_SpawnsExpenseEntityWithLabel()
    {
        var scope = CreateScope();
        var character = SpawnCharacterInHousehold(out var household);
        scope.WithAnchor("root", character);
        var label = new Localizable("expense.bribe", new Dictionary<string, object?>());

        new AddExpense("root", ExpenseCategory.Entertainment, 200, label).Apply(scope);

        var expenses = GetExpensesFor(household);
        expenses.Should().HaveCount(1);
        expenses[0].Category.Should().Be(ExpenseCategory.Entertainment);
        expenses[0].Amount.Should().Be(200);

        // Verify label component exists
        var expenseEntities = _world.Query<Expense, ExpenseLabel>().Stream();
        var count = 0;
        foreach ((Entity _, Expense _, ExpenseLabel expLabel) in expenseEntities)
        {
            expLabel.Label.Should().Be(label);
            count++;
        }
        count.Should().Be(1);
    }

    [Fact]
    public void Apply_WithoutLabel_SpawnsExpenseEntityWithoutLabel()
    {
        var scope = CreateScope();
        var character = SpawnCharacterInHousehold(out var household);
        scope.WithAnchor("root", character);

        new AddExpense("root", ExpenseCategory.Food, 50).Apply(scope);

        var expenses = GetExpensesFor(household);
        expenses.Should().HaveCount(1);
        expenses[0].Category.Should().Be(ExpenseCategory.Food);
        expenses[0].Amount.Should().Be(50);

        // Verify no label component
        var labeled = _world.Query<Expense, ExpenseLabel>().Stream();
        var count = 0;
        foreach (var _ in labeled) count++;
        count.Should().Be(0);
    }

    [Fact]
    public void Apply_NoHousehold_DoesNotSpawnExpense()
    {
        var scope = CreateScope();
        var character = _world.Spawn();
        scope.WithAnchor("root", character);

        new AddExpense("root", ExpenseCategory.Food, 50).Apply(scope);

        var stream = _world.Query<Expense>().Stream();
        var count = 0;
        foreach (var _ in stream) count++;
        count.Should().Be(0);
    }

    [Fact]
    public void Apply_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new AddExpense("nobody", ExpenseCategory.Food, 50).Apply(scope);
    }

    [Fact]
    public void Apply_ThroughRelation_FindsHousehold()
    {
        var scope = CreateScope();
        var character = SpawnCharacterInHousehold(out var household);
        var boss = _world.Spawn();
        boss.Add(new BossOf(character), character);
        scope.WithAnchor("boss", boss);

        new AddExpense("boss.BossOf", ExpenseCategory.Medical, 100).Apply(scope);

        var expenses = GetExpensesFor(household);
        expenses.Should().HaveCount(1);
        expenses[0].Category.Should().Be(ExpenseCategory.Medical);
        expenses[0].Amount.Should().Be(100);
    }

    [Fact]
    public void Describe_ReturnsLocalizableWithCategoryAndAmount()
    {
        var scope = CreateScope();
        var desc = new AddExpense("root", ExpenseCategory.Food, 50).Describe(scope);

        desc.Key.Should().Be("effect.add_expense");
        desc.Args["category"].Should().Be("food");
        desc.Args["amount"].Should().Be("50");
    }

    [Fact]
    public void Apply_MultipleExpenses_SpawnsSeparateEntities()
    {
        var scope = CreateScope();
        var character = SpawnCharacterInHousehold(out var household);
        scope.WithAnchor("root", character);

        new AddExpense("root", ExpenseCategory.Food, 50).Apply(scope);
        new AddExpense("root", ExpenseCategory.Food, 30).Apply(scope);

        var expenses = GetExpensesFor(household);
        expenses.Should().HaveCount(2);
        expenses.Sum(e => e.Amount).Should().Be(80);
    }
}
