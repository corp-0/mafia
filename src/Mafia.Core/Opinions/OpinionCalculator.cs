using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Text;

namespace Mafia.Core.Opinions;

/// <summary>
/// Calculates the opinion and outputs the formatted strings for the UI tooltip.
/// </summary>
public class OpinionCalculator(World world, IReadOnlyList<OpinionRuleDefinition> passiveRules)
{
    public const int BASE_OPINION = 10;
    
    public int Calculate(Entity evaluator, Entity target, out List<Localizable> tooltips)
    {
        int totalScore = BASE_OPINION;
        tooltips = new();

        var scope = new EntityScope(world)
            .WithAnchor("root", evaluator)
            .WithAnchor("target", target);

        totalScore += CalculatePassiveRules(scope, tooltips);
        totalScore += CalculateMemories(evaluator, target, tooltips);

        return totalScore;
    }

    private static int CalculateMemories(Entity evaluator, Entity target, List<Localizable> tooltips)
    {
        if (!evaluator.Has<MemoriesOf>(target))
            return 0;

        int totalScore = 0;
        MemoriesOf memories = evaluator.Ref<MemoriesOf>(target);
        foreach (OpinionMemory memory in memories.Memories)
        {
            totalScore += memory.Amount;
            var sign = memory.Amount > 0 ? "+" : "";
            tooltips.Add(new Localizable(memory.DefinitionId, new Dictionary<string, object?>
            {
                ["sign"] = sign,
                ["amount"] = Math.Abs(memory.Amount).ToString()
            }));
        }

        return totalScore;
    }

    private int CalculatePassiveRules(EntityScope scope, List<Localizable> tooltips)
    {
        int totalScore = 0;

        foreach (OpinionRuleDefinition rule in passiveRules)
        {
            if (rule.Conditions.Evaluate(scope))
            {
                totalScore += rule.Modifier;

                var sign = rule.Modifier > 0 ? "+" : "";
                tooltips.Add(new Localizable(rule.TooltipKey,
                    new Dictionary<string, object?>
                    {
                        ["sign"] = sign,
                        ["amount"] = Math.Abs(rule.Modifier).ToString()
                    }));
            }
        }

        return totalScore;
    }
}