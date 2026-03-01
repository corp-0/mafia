using fennecs;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Engine;
using Mafia.Core.Time;
using Microsoft.Extensions.Logging;

namespace Mafia.Core.Ecs.Systems;

public class RoutineExpenseSystem(World world, ILogger<RoutineExpenseSystem> logger) : ITickSystem
{
    private const int BASE_FOOD_PER_MEMBER = 30;
    private const int BASE_HOUSING_PER_MEMBER = 20;
    private const int BASE_CLOTHING_PER_MEMBER = 10;

    public void Tick(GameDate currentDate, IActionTrigger _)
    {
        if (currentDate.Day != 15) return;

        // Count members per household by walking the relation graph
        var memberCounts = new Dictionary<Entity, int>();
        var characterStream = world.Query<Character>().Stream();
        foreach ((Entity character, Character _) in characterStream)
        {
            var rels = character.Get<MemberOfHousehold>(Match.Entity);
            if (rels.Length == 0) continue;
            var household = rels[0].Target;
            if (!memberCounts.TryAdd(household, 1))
                memberCounts[household]++;
        }

        foreach (var (household, members) in memberCounts)
        {
            SpawnExpense(household, ExpenseCategory.Food, BASE_FOOD_PER_MEMBER * members);
            SpawnExpense(household, ExpenseCategory.Housing, BASE_HOUSING_PER_MEMBER * members);
            SpawnExpense(household, ExpenseCategory.Clothing, BASE_CLOTHING_PER_MEMBER * members);
        }
    }

    private void SpawnExpense(Entity household, ExpenseCategory category, int amount)
    {
        using EntitySpawner expense = world.Entity()
            .Add(new Expense(category, amount))
            .Add(new ExpenseOf(household), household)
            .Spawn();
    }
}
