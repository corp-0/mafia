using fennecs;
using FluentAssertions;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Ecs.Systems;
using Mafia.Core.Events.Effects;
using Mafia.Core.Tests.Events.Engine;
using Mafia.Core.Text;
using Mafia.Core.Time;
using Xunit;

namespace Mafia.Core.Tests.Ecs.Systems;

public class ExpenseSettlementSystemTests : IDisposable
{
    private readonly World _world = new();
    private readonly RecordingActionTrigger _trigger = new();

    public void Dispose() => _world.Dispose();

    private (Entity household, Entity head) CreateHouseholdWithHead(int wealth)
    {
        var head = _world.Spawn();
        head.Add(new Wealth { Amount = wealth });

        var household = _world.Spawn();
        household.Add(new Household());
        household.Add(new HeadOfHousehold(head), head);
        head.Add(new MemberOfHousehold(household), household);

        return (household, head);
    }

    private void AddExpense(Entity household, ExpenseCategory category, int amount)
    {
        var expense = _world.Spawn();
        expense.Add(new Expense(category, amount));
        expense.Add(new ExpenseOf(household), household);
    }

    private int CountExpenseEntities()
    {
        var count = 0;
        var stream = _world.Query<Expense>().Stream();
        foreach (var _ in stream) count++;
        return count;
    }

    [Fact]
    public void Tick_Day1_FiresPayBillsForHouseholdWithExpenses()
    {
        var (household, head) = CreateHouseholdWithHead(1000);
        AddExpense(household, ExpenseCategory.Food, 150);

        var system = new ExpenseSettlementSystem(_world);
        system.Tick(new GameDate(1930, 2, 1), _trigger);

        _trigger.Invocations.Should().HaveCount(1);
        _trigger.Invocations[0].ActionId.Should().Be("pay_bills");
        _trigger.Invocations[0].Scope.ResolveAnchor("root").Should().Be(head);
    }

    [Fact]
    public void Tick_Day1_MultipleHouseholds_FiresOncePerHousehold()
    {
        var (household1, head1) = CreateHouseholdWithHead(1000);
        var (household2, head2) = CreateHouseholdWithHead(500);
        AddExpense(household1, ExpenseCategory.Food, 100);
        AddExpense(household2, ExpenseCategory.Housing, 200);

        var system = new ExpenseSettlementSystem(_world);
        system.Tick(new GameDate(1930, 1, 1), _trigger);

        _trigger.Invocations.Should().HaveCount(2);
        var heads = _trigger.Invocations.Select(i => i.Scope.ResolveAnchor("root")).ToList();
        heads.Should().Contain(head1);
        heads.Should().Contain(head2);
    }

    [Fact]
    public void Tick_NotDay1_DoesNotFireAnyAction()
    {
        var (household, _) = CreateHouseholdWithHead(1000);
        AddExpense(household, ExpenseCategory.Food, 150);

        var system = new ExpenseSettlementSystem(_world);
        system.Tick(new GameDate(1930, 2, 15), _trigger);

        _trigger.Invocations.Should().BeEmpty();
    }

    [Fact]
    public void Tick_Day1_NoExpenses_DoesNotFireAnyAction()
    {
        CreateHouseholdWithHead(500);

        var system = new ExpenseSettlementSystem(_world);
        system.Tick(new GameDate(1930, 1, 1), _trigger);

        _trigger.Invocations.Should().BeEmpty();
    }

    [Fact]
    public void Tick_Day1_ExpensesAreNotDespawned()
    {
        var (household, _) = CreateHouseholdWithHead(1000);
        AddExpense(household, ExpenseCategory.Food, 50);
        AddExpense(household, ExpenseCategory.Housing, 30);

        CountExpenseEntities().Should().Be(2);

        var system = new ExpenseSettlementSystem(_world);
        system.Tick(new GameDate(1930, 2, 1), _trigger);

        CountExpenseEntities().Should().Be(2);
    }

    [Fact]
    public void Tick_Day1_WealthIsNotDeducted()
    {
        var (household, head) = CreateHouseholdWithHead(1000);
        AddExpense(household, ExpenseCategory.Food, 150);
        AddExpense(household, ExpenseCategory.Housing, 100);

        var system = new ExpenseSettlementSystem(_world);
        system.Tick(new GameDate(1930, 2, 1), _trigger);

        head.Ref<Wealth>().Amount.Should().Be(1000);
    }

    [Fact]
    public void Tick_Day1_NoHead_SkipsHousehold()
    {
        var household = _world.Spawn();
        household.Add(new Household());
        AddExpense(household, ExpenseCategory.Food, 50);

        var system = new ExpenseSettlementSystem(_world);
        system.Tick(new GameDate(1930, 1, 1), _trigger);

        _trigger.Invocations.Should().BeEmpty();
    }

    [Fact]
    public void Tick_Day1_MultipleExpensesSameHousehold_FiresOnce()
    {
        var (household, _) = CreateHouseholdWithHead(1000);
        AddExpense(household, ExpenseCategory.Food, 50);
        AddExpense(household, ExpenseCategory.Housing, 100);
        AddExpense(household, ExpenseCategory.Entertainment, 200);

        var system = new ExpenseSettlementSystem(_world);
        system.Tick(new GameDate(1930, 1, 1), _trigger);

        _trigger.Invocations.Should().HaveCount(1);
    }

    [Fact]
    public void Tick_Day1_ScopeHasUiHintMetadata()
    {
        var (household, _) = CreateHouseholdWithHead(1000);
        AddExpense(household, ExpenseCategory.Food, 100);

        var system = new ExpenseSettlementSystem(_world);
        system.Tick(new GameDate(1930, 1, 1), _trigger);

        _trigger.Invocations[0].Scope.GetMeta("ui_hint").Should().Be("bills");
    }

    [Fact]
    public void Tick_Day1_ScopeHasExpenseItems()
    {
        var (household, _) = CreateHouseholdWithHead(1000);
        AddExpense(household, ExpenseCategory.Food, 150);
        AddExpense(household, ExpenseCategory.Housing, 100);

        var system = new ExpenseSettlementSystem(_world);
        system.Tick(new GameDate(1930, 1, 1), _trigger);

        var items = _trigger.Invocations[0].Scope.GetMeta("expenses") as List<ExpenseLineItem>;
        items.Should().NotBeNull();
        items.Should().HaveCount(2);
        items!.Sum(i => i.Amount).Should().Be(250);
    }

    [Fact]
    public void Tick_Day1_LabeledExpenses_CapturedInMetadata()
    {
        var (household, _) = CreateHouseholdWithHead(1000);
        var label = new Localizable("expense.bribe", new Dictionary<string, object?>());
        var expense = _world.Spawn();
        expense.Add(new Expense(ExpenseCategory.Entertainment, 200));
        expense.Add(new ExpenseOf(household), household);
        expense.Add(new ExpenseLabel(label));

        var system = new ExpenseSettlementSystem(_world);
        system.Tick(new GameDate(1930, 1, 1), _trigger);

        var items = _trigger.Invocations[0].Scope.GetMeta("expenses") as List<ExpenseLineItem>;
        items.Should().HaveCount(1);
        items![0].Label.Should().Be(label);
        items[0].Category.Should().Be(ExpenseCategory.Entertainment);
        items[0].Amount.Should().Be(200);
    }
}
