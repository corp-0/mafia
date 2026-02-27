using Mafia.Core.Content.Parsers.Dtos;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Events.Conditions;
using Mafia.Core.Events.Conditions.Interfaces;

namespace Mafia.Core.Content.Factories;

public partial class ConditionFactory
{
    public static IEventCondition Create(ConditionDto dto)
    {
        return dto.Type switch
        {
            "stat_threshold" => ResolveStatThreshold(
                Normalize(dto.Stat!), dto.Path!, ParseComparison(dto.Comparison!), dto.Value ?? 0),

            "has_tag" => ResolveHasTagCondition(
                Normalize(dto.Tag!), dto.Path!),

            "has_relationship" => ResolveHasRelationship(
                Normalize(dto.Kind!), dto.From!, dto.To!),

            "has_minimum_rank" => new HasMinimumRank(
                dto.Path!, Enum.Parse<RankId>(dto.Rank!, ignoreCase: true)),

            "same_location" => new SameLocation(
                dto.PathA!, dto.PathB!),

            "event_fired" => new EventFired(
                dto.EventId!, dto.Path!,
                dto.Comparison is not null ? ParseComparison(dto.Comparison) : null,
                dto.Value),

            "all_of" => new AllOf(
                dto.Conditions!.Select(Create).ToArray()),

            "any_of" => new AnyOf(
                dto.Conditions!.Select(Create).ToArray()),

            "none_of" => new NoneOf(
                dto.Conditions!.Select(Create).ToArray()),

            _ => throw new ArgumentException($"Unknown condition type: '{dto.Type}'")
        };
    }

    public static Comparison ParseComparison(string value)
    {
        return Normalize(value) switch
        {
            "greaterthan" or "gt" or ">" => Comparison.GreaterThan,
            "lessthan" or "lt" or "<" => Comparison.LessThan,
            "greaterthanorequalto" or "gte" or ">=" => Comparison.GreaterThanOrEqualTo,
            "lessthanorequalto" or "lte" or "<=" => Comparison.LessThanOrEqualTo,
            "equal" or "eq" or "==" => Comparison.Equal,
            _ => throw new ArgumentException($"Unknown comparison: '{value}'")
        };
    }

    private static string Normalize(string input) =>
        input.Replace("_", "").ToLowerInvariant();
}
