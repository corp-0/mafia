using Mafia.Core.Content.Parsers.Dtos;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Events.Effects;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Opinions;
using Mafia.Core.Time;

namespace Mafia.Core.Content.Factories;

public partial class EffectFactory
{
    public static IEventEffect Create(EffectDto dto)
    {
        return dto.Type switch
        {
            "modify_stat" => ResolveModifyStat(
                Normalize(dto.Stat!), dto.Path!, dto.Amount ?? 0),

            "set_stat" => ResolveSetStat(
                Normalize(dto.Stat!), dto.Path!, dto.Value ?? 0),

            "add_tag" => ResolveAddTag(
                Normalize(dto.Tag!), dto.Path!),

            "remove_tag" => ResolveRemoveTag(
                Normalize(dto.Tag!), dto.Path!),

            "add_relationship" => ResolveAddRelationship(
                Normalize(dto.Kind!), dto.From!, dto.To!),

            "remove_relationship" => ResolveRemoveRelationship(
                Normalize(dto.Kind!), dto.From!, dto.To!),

            "disable_character" => ResolveDisableCharacter(
                Normalize(dto.Reason!), dto.Path!),

            "enable_character" => ResolveEnableCharacter(
                Normalize(dto.Reason!), dto.Path!),

            "transfer_money" => new TransferMoney(
                dto.From!, dto.To!, dto.Amount ?? 0),

            "trigger_event" => new TriggerEvent(
                dto.EventId!, dto.Path),

            "add_memory" => new AddMemory(
                dto.From!, dto.To!,
                new OpinionMemory
                {
                    DefinitionId = dto.MemoryId!,
                    Amount = dto.Amount ?? 0,
                    ExpiresOn = GameDate.Parse(dto.ExpiresOn!)
                }),

            "remove_memory" => new RemoveMemory(
                dto.From!, dto.To!, dto.MemoryId!),

            "clear_memories" => new ClearMemories(
                dto.From!, dto.To!),

            "change_rank" => new ChangeRank(
                dto.Path!, Enum.Parse<RankId>(dto.Rank!, ignoreCase: true)),

            "change_nickname" => new ChangeNickname(
                dto.Path!, dto.Nickname!),

            _ => throw new ArgumentException($"Unknown effect type: '{dto.Type}'")
        };
    }

    private static string Normalize(string input) =>
        input.Replace("_", "").ToLowerInvariant();
}
