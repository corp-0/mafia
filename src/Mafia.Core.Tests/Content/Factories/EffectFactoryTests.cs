using FluentAssertions;
using Mafia.Core.Content.Factories;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects;
using Xunit;

namespace Mafia.Core.Tests.Content.Factories;

public class EffectFactoryTests
{
    [Fact]
    public void Create_ModifyStat_ReturnsModifyStatEffect()
    {
        var dto = new EffectDto { Type = "modify_stat", Stat = "muscle", Path = "root", Amount = 3 };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<ModifyStat<Muscle>>();
    }

    [Fact]
    public void Create_ModifyStat_SnakeCase_NormalizesCorrectly()
    {
        // Wealth has no underscores, but test normalization with meters
        var dto = new EffectDto { Type = "modify_stat", Stat = "wealth", Path = "root", Amount = 100 };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<ModifyStat<Wealth>>();
    }

    [Fact]
    public void Create_SetStat_ReturnsSetStatEffect()
    {
        var dto = new EffectDto { Type = "set_stat", Stat = "stress", Path = "root", Value = 50 };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<SetStat<Stress>>();
    }

    [Fact]
    public void Create_AddTag_ReturnsAddTagEffect()
    {
        var dto = new EffectDto { Type = "add_tag", Tag = "underboss", Path = "root" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<AddTag<Underboss>>();
    }

    [Fact]
    public void Create_RemoveTag_ReturnsRemoveTagEffect()
    {
        var dto = new EffectDto { Type = "remove_tag", Tag = "consigliere", Path = "root" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<RemoveTag<Consigliere>>();
    }

    [Fact]
    public void Create_AddRelationship_ReturnsAddRelationshipEffect()
    {
        var dto = new EffectDto { Type = "add_relationship", Kind = "subordinate_of", From = "root", To = "boss" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<AddRelationship<SubordinateOf>>();
    }

    [Fact]
    public void Create_RemoveRelationship_ReturnsRemoveRelationshipEffect()
    {
        var dto = new EffectDto { Type = "remove_relationship", Kind = "boss_of", From = "root", To = "target" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<RemoveRelationship<BossOf>>();
    }

    [Fact]
    public void Create_DisableCharacter_ReturnsDisableEffect()
    {
        var dto = new EffectDto { Type = "disable_character", Reason = "killed", Path = "root" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<DisableCharacter<Killed>>();
    }

    [Fact]
    public void Create_EnableCharacter_ReturnsEnableEffect()
    {
        var dto = new EffectDto { Type = "enable_character", Reason = "arrested", Path = "root" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<EnableCharacter<Arrested>>();
    }

    [Fact]
    public void Create_TransferMoney_ReturnsTransferMoneyEffect()
    {
        var dto = new EffectDto { Type = "transfer_money", From = "root", To = "target", Amount = 500 };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<TransferMoney>();
    }

    [Fact]
    public void Create_TriggerEvent_ReturnsTriggerEventEffect()
    {
        var dto = new EffectDto { Type = "trigger_event", EventId = "evt_revenge" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<TriggerEvent>();
    }

    [Fact]
    public void Create_ChangeRank_ReturnsChangeRankEffect()
    {
        var dto = new EffectDto { Type = "change_rank", Path = "root", Rank = "caporegime" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<ChangeRank>();
    }

    [Fact]
    public void Create_ChangeNickname_ReturnsChangeNicknameEffect()
    {
        var dto = new EffectDto { Type = "change_nickname", Path = "root", Nickname = "The Bull" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<ChangeNickname>();
    }

    [Fact]
    public void Create_AddMemory_ReturnsAddMemoryEffect()
    {
        var dto = new EffectDto
        {
            Type = "add_memory", From = "root", To = "target",
            MemoryId = "betrayal", Amount = -20, ExpiresOn = "1930-06-15"
        };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<AddMemory>();
    }

    [Fact]
    public void Create_RemoveMemory_ReturnsRemoveMemoryEffect()
    {
        var dto = new EffectDto { Type = "remove_memory", From = "root", To = "target", MemoryId = "betrayal" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<RemoveMemory>();
    }

    [Fact]
    public void Create_ClearMemories_ReturnsClearMemoriesEffect()
    {
        var dto = new EffectDto { Type = "clear_memories", From = "root", To = "target" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<ClearMemories>();
    }

    [Fact]
    public void Create_UnknownType_ThrowsArgumentException()
    {
        var dto = new EffectDto { Type = "unknown_effect" };
        var act = () => EffectFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown effect type*");
    }

    [Fact]
    public void Create_ModifyStat_UnknownStat_ThrowsArgumentException()
    {
        var dto = new EffectDto { Type = "modify_stat", Stat = "nonexistent", Path = "root", Amount = 1 };
        var act = () => EffectFactory.Create(dto);
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown stat*");
    }

    [Fact]
    public void Create_AddTag_UnknownTag_ReturnsAddCustomTag()
    {
        var dto = new EffectDto { Type = "add_tag", Tag = "nonexistent", Path = "root" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<AddCustomTag>();
    }

    [Fact]
    public void Create_RemoveTag_UnknownTag_ReturnsRemoveCustomTag()
    {
        var dto = new EffectDto { Type = "remove_tag", Tag = "nonexistent", Path = "root" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<RemoveCustomTag>();
    }

    [Fact]
    public void Create_SnakeCase_Normalization_Works()
    {
        // "subordinate_of" → strips underscores → "subordinateof" matches SubordinateOf
        var dto = new EffectDto { Type = "add_relationship", Kind = "father_of", From = "root", To = "child" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<AddRelationship<FatherOf>>();
    }

    [Fact]
    public void Create_SettleExpenses_ReturnsSettleExpensesEffect()
    {
        var dto = new EffectDto { Type = "settle_expenses", Path = "root" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<SettleExpenses>();
    }

    [Fact]
    public void Create_AddExpense_ReturnsAddExpenseEffect()
    {
        var dto = new EffectDto { Type = "add_expense", Path = "root", Category = "food", Amount = 50 };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<AddExpense>();
    }

    [Fact]
    public void Create_AddExpense_WithLabel_ReturnsAddExpenseEffect()
    {
        var dto = new EffectDto { Type = "add_expense", Path = "root", Category = "entertainment", Amount = 200, LabelKey = "expense.bribe" };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<AddExpense>();
    }

    [Fact]
    public void Create_AddExpense_CaseInsensitiveCategory()
    {
        var dto = new EffectDto { Type = "add_expense", Path = "root", Category = "Medical", Amount = 75 };
        var effect = EffectFactory.Create(dto);
        effect.Should().BeOfType<AddExpense>();
    }

    [Fact]
    public void Create_AddExpense_InvalidCategory_Throws()
    {
        var dto = new EffectDto { Type = "add_expense", Path = "root", Category = "nonexistent", Amount = 50 };
        var act = () => EffectFactory.Create(dto);
        act.Should().Throw<ArgumentException>();
    }
}
