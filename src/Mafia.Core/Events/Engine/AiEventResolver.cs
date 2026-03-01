using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Events.Definition;
using Mafia.Core.Extensions;
using Microsoft.Extensions.Logging;

namespace Mafia.Core.Events.Engine;

public class AiEventResolver(ILogger<AiEventResolver> logger, Random? random = null)
{
    private readonly Random _random = random ?? Random.Shared;
    private readonly ILogger<AiEventResolver> _logger = logger;

    public void Resolve(EventDefinition definition, EntityScope scope)
    {
        var visibleOptions = definition.GetVisibleOptions(scope);
        if (visibleOptions.Count == 0) return;

        var chosen = WeightedRandomSelect(visibleOptions, scope);
        if (chosen is null) return;

        ApplyOptionOutcome(chosen, scope);
    }

    public void ApplyOptionOutcome(EventOptionDefinition option, EntityScope scope)
    {
        var outcome = ResolveOutcome(option, scope);
        if (outcome is null) return;

        foreach (var effect in outcome.Effects)
            effect.Apply(scope);
    }

    private EventOutcome? ResolveOutcome(EventOptionDefinition option, EntityScope scope) => option switch
    {
        StandardOptionDefinition standard => standard.Outcome,
        SkillCheckOptionDefinition skillCheck => ResolveSkillCheck(skillCheck, scope),
        RandomOptionDefinition random => ResolveRandomOutcome(random),
        _ => null
    };

    private EventOutcome ResolveSkillCheck(SkillCheckOptionDefinition skillCheck, EntityScope scope)
    {
        var statValue = GetStatValue(skillCheck.StatName, skillCheck.StatPath, scope);
        var roll = _random.Next(1, 7) + _random.Next(1, 7); // 2d6
        var total = roll + statValue;
        return total >= skillCheck.Difficulty ? skillCheck.Success : skillCheck.Failure;
    }

    private EventOutcome ResolveRandomOutcome(RandomOptionDefinition random)
    {
        var totalWeight = 0;
        foreach (var outcome in random.Outcomes)
            totalWeight += outcome.Weight;

        if (totalWeight <= 0) return random.Outcomes[0];

        var roll = _random.Next(totalWeight);
        var cumulative = 0;
        foreach (var outcome in random.Outcomes)
        {
            cumulative += outcome.Weight;
            if (roll < cumulative) return outcome;
        }

        return random.Outcomes[^1];
    }

    public static int GetStatValue(string statName, string statPath, EntityScope scope)
    {
        if (!scope.TryNavigate(statPath, out var entity)) return 0;
        return statName.ToLowerInvariant() switch
        {
            "muscle" => entity.GetComponent<Muscle>()?.Amount ?? 0,
            "nerve" => entity.GetComponent<Nerve>()?.Amount ?? 0,
            "brains" => entity.GetComponent<Brains>()?.Amount ?? 0,
            "charm" => entity.GetComponent<Charm>()?.Amount ?? 0,
            "instinct" => entity.GetComponent<Instinct>()?.Amount ?? 0,
            _ => 0
        };
    }

    private EventOptionDefinition? WeightedRandomSelect(List<EventOptionDefinition> options, EntityScope scope)
    {
        var totalWeight = 0;
        var weights = new int[options.Count];
        for (var i = 0; i < options.Count; i++)
        {
            weights[i] = options[i].AiWeight.Calculate(scope);
            totalWeight += weights[i];
        }

        if (totalWeight <= 0) return null;

        var roll = _random.Next(totalWeight);
        var cumulative = 0;
        for (var i = 0; i < options.Count; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) return options[i];
        }

        return options[^1];
    }
}
