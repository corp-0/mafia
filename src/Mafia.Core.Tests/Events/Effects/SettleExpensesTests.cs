using fennecs;
using FluentAssertions;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects;
using Mafia.Core.Text;
using Xunit;

namespace Mafia.Core.Tests.Events.Effects;

public class SettleExpensesTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    private EntityScope CreateScope() => new(_world);

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
    public void Apply_DeductsExpensesFromWealth()
    {
        var (household, head) = CreateHouseholdWithHead(1000);
        AddExpense(household, ExpenseCategory.Food, 150);
        AddExpense(household, ExpenseCategory.Housing, 100);

        var scope = CreateScope().WithAnchor("root", head);
        new SettleExpenses("root").Apply(scope);

        head.Ref<Wealth>().Amount.Should().Be(750);
    }

    [Fact]
    public void Apply_DespawnsExpenseEntities()
    {
        var (household, head) = CreateHouseholdWithHead(1000);
        AddExpense(household, ExpenseCategory.Food, 50);
        AddExpense(household, ExpenseCategory.Housing, 30);

        CountExpenseEntities().Should().Be(2);

        var scope = CreateScope().WithAnchor("root", head);
        new SettleExpenses("root").Apply(scope);

        CountExpenseEntities().Should().Be(0);
    }

    [Fact]
    public void Apply_ClampsWealthToZero()
    {
        var (household, head) = CreateHouseholdWithHead(100);
        AddExpense(household, ExpenseCategory.Food, 300);

        var scope = CreateScope().WithAnchor("root", head);
        new SettleExpenses("root").Apply(scope);

        head.Ref<Wealth>().Amount.Should().Be(0);
    }

    [Fact]
    public void Apply_HeadNoWealth_StillDespawnsExpenses()
    {
        var head = _world.Spawn();
        var household = _world.Spawn();
        household.Add(new Household());
        household.Add(new HeadOfHousehold(head), head);
        head.Add(new MemberOfHousehold(household), household);
        AddExpense(household, ExpenseCategory.Food, 50);

        var scope = CreateScope().WithAnchor("root", head);
        new SettleExpenses("root").Apply(scope);

        CountExpenseEntities().Should().Be(0);
    }

    [Fact]
    public void Apply_OnlyTargetsCorrectHousehold()
    {
        var (household1, head1) = CreateHouseholdWithHead(1000);
        var (household2, _) = CreateHouseholdWithHead(500);
        AddExpense(household1, ExpenseCategory.Food, 100);
        AddExpense(household2, ExpenseCategory.Housing, 200);

        var scope = CreateScope().WithAnchor("root", head1);
        new SettleExpenses("root").Apply(scope);

        head1.Ref<Wealth>().Amount.Should().Be(900);
        CountExpenseEntities().Should().Be(1);
    }

    [Fact]
    public void Apply_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();
        new SettleExpenses("nobody").Apply(scope);
    }

    [Fact]
    public void Apply_NoHousehold_DoesNotThrow()
    {
        var head = _world.Spawn();
        head.Add(new Wealth { Amount = 500 });

        var scope = CreateScope().WithAnchor("root", head);
        new SettleExpenses("root").Apply(scope);

        head.Ref<Wealth>().Amount.Should().Be(500);
    }

    [Fact]
    public void Apply_LabeledExpenses_AlsoDespawned()
    {
        var (household, head) = CreateHouseholdWithHead(1000);
        var expense = _world.Spawn();
        expense.Add(new Expense(ExpenseCategory.Entertainment, 200));
        expense.Add(new ExpenseOf(household), household);
        expense.Add(new ExpenseLabel(new Localizable("expense.bribe", new Dictionary<string, object?>())));

        var scope = CreateScope().WithAnchor("root", head);
        new SettleExpenses("root").Apply(scope);

        head.Ref<Wealth>().Amount.Should().Be(800);
        CountExpenseEntities().Should().Be(0);
    }

    [Fact]
    public void Describe_AfterApply_ContainsTotalAndItems()
    {
        var (household, head) = CreateHouseholdWithHead(1000);
        AddExpense(household, ExpenseCategory.Food, 150);
        AddExpense(household, ExpenseCategory.Housing, 100);

        var effect = new SettleExpenses("root");
        var scope = CreateScope().WithAnchor("root", head);
        effect.Apply(scope);

        var desc = effect.Describe(scope);
        desc.Key.Should().Be("effect.settle_expenses");
        desc.Args["amount"].Should().Be(250);
        desc.Args["remaining_wealth"].Should().Be(750);

        var items = desc.Args["items"] as List<ExpenseLineItem>;
        items.Should().NotBeNull();
        items.Should().HaveCount(2);
        items.Should().Contain(i => i.Category == ExpenseCategory.Food && i.Amount == 150);
        items.Should().Contain(i => i.Category == ExpenseCategory.Housing && i.Amount == 100);
    }

    [Fact]
    public void Describe_AfterApply_CapturesLabels()
    {
        var (household, head) = CreateHouseholdWithHead(1000);
        var label = new Localizable("expense.bribe", new Dictionary<string, object?>());
        var expense = _world.Spawn();
        expense.Add(new Expense(ExpenseCategory.Entertainment, 200));
        expense.Add(new ExpenseOf(household), household);
        expense.Add(new ExpenseLabel(label));

        var effect = new SettleExpenses("root");
        var scope = CreateScope().WithAnchor("root", head);
        effect.Apply(scope);

        var desc = effect.Describe(scope);
        var items = desc.Args["items"] as List<ExpenseLineItem>;
        items.Should().HaveCount(1);
        items![0].Label.Should().Be(label);
    }

    [Fact]
    public void Describe_BeforeApply_HasZeroTotalAndEmptyItems()
    {
        var scope = CreateScope();
        var desc = new SettleExpenses("root").Describe(scope);

        desc.Key.Should().Be("effect.settle_expenses");
        desc.Args["amount"].Should().Be(0);
        (desc.Args["items"] as List<ExpenseLineItem>).Should().BeEmpty();
    }
}
