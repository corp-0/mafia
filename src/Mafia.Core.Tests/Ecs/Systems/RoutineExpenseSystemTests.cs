using fennecs;
using FluentAssertions;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Ecs.Systems;
using Mafia.Core.Tests.Events.Engine;
using Mafia.Core.Time;
using Xunit;

namespace Mafia.Core.Tests.Ecs.Systems;

public class RoutineExpenseSystemTests : IDisposable
{
    private readonly World _world = new();
    private readonly NullActionTrigger _trigger = new();

    public void Dispose() => _world.Dispose();

    private Entity CreateHouseholdWithMembers(int memberCount)
    {
        var household = _world.Spawn();
        household.Add<Household>();

        for (var i = 0; i < memberCount; i++)
        {
            var member = _world.Spawn();
            member.Add<Character>();
            member.Add(new MemberOfHousehold(household), household);
        }

        return household;
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
    public void Tick_Day15_SpawnsRoutineExpenses()
    {
        var household = CreateHouseholdWithMembers(1);

        var system = new RoutineExpenseSystem(_world);
        system.Tick(new GameDate(1930, 1, 15), _trigger);

        var expenses = GetExpensesFor(household);
        expenses.Should().HaveCount(3);
        expenses.Should().Contain(e => e.Category == ExpenseCategory.Food);
        expenses.Should().Contain(e => e.Category == ExpenseCategory.Housing);
        expenses.Should().Contain(e => e.Category == ExpenseCategory.Clothing);
    }

    [Fact]
    public void Tick_Day15_ScalesByMemberCount()
    {
        var household = CreateHouseholdWithMembers(3);

        var system = new RoutineExpenseSystem(_world);
        system.Tick(new GameDate(1930, 1, 15), _trigger);

        var expenses = GetExpensesFor(household);
        expenses.First(e => e.Category == ExpenseCategory.Food).Amount.Should().Be(90);    // 30 * 3
        expenses.First(e => e.Category == ExpenseCategory.Housing).Amount.Should().Be(60); // 20 * 3
        expenses.First(e => e.Category == ExpenseCategory.Clothing).Amount.Should().Be(30); // 10 * 3
    }

    [Fact]
    public void Tick_NotDay15_DoesNothing()
    {
        CreateHouseholdWithMembers(2);

        var system = new RoutineExpenseSystem(_world);
        system.Tick(new GameDate(1930, 1, 1), _trigger);

        var stream = _world.Query<Expense>().Stream();
        var count = 0;
        foreach (var _ in stream) count++;
        count.Should().Be(0);
    }

    [Fact]
    public void Tick_Day15_MultipleHouseholds_EachGetsExpenses()
    {
        var h1 = CreateHouseholdWithMembers(1);
        var h2 = CreateHouseholdWithMembers(2);

        var system = new RoutineExpenseSystem(_world);
        system.Tick(new GameDate(1930, 1, 15), _trigger);

        GetExpensesFor(h1).Should().HaveCount(3);
        GetExpensesFor(h2).Should().HaveCount(3);
    }
}
