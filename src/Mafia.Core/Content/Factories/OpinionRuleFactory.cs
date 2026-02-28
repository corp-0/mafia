using Mafia.Core.Content.Parsers.Dtos;
using Mafia.Core.Events.Conditions.Interfaces;
using Mafia.Core.Opinions;

namespace Mafia.Core.Content.Factories;

public static class OpinionRuleFactory
{
    public static OpinionRuleDefinition Create(OpinionRuleDto dto)
    {
        IEventCondition conditions = ConditionFactory.Create(dto.Conditions);
        
        var def = new OpinionRuleDefinition
        {
            Id = dto.Id,
            Modifier = dto.Modifier,
            TooltipKey = dto.TooltipKey,
            Conditions = conditions
        };
        
        return def;
    }
}