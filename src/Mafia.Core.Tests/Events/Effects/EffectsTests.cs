using fennecs;
using FluentAssertions;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects;
using Xunit;

namespace Mafia.Core.Tests.Events.Effects;

using Mafia.Core.Opinions;
using Mafia.Core.Time;

public class EffectsTests : IDisposable
{
    private readonly World _world = new();

    public void Dispose() => _world.Dispose();

    private EntityScope CreateScope() => new(_world);

    private Entity SpawnEntity() => _world.Spawn();

    #region AddRelationship

    [Fact]
    public void AddRelationship_CreatesRelationBetweenEntities()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        scope.WithAnchor("vito", vito).WithAnchor("michael", michael);

        new AddRelationship<FatherOf>("vito", "michael").Apply(scope);

        vito.Has<FatherOf>(michael).Should().BeTrue();
    }

    [Fact]
    public void AddRelationship_AlreadyExists_DoesNotThrow()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito).WithAnchor("michael", michael);

        new AddRelationship<FatherOf>("vito", "michael").Apply(scope);

        vito.Has<FatherOf>(michael).Should().BeTrue();
    }

    [Fact]
    public void AddRelationship_InvalidFromPath_DoesNotThrow()
    {
        var scope = CreateScope();
        var michael = SpawnEntity();
        scope.WithAnchor("michael", michael);

        new AddRelationship<FatherOf>("nobody", "michael").Apply(scope);
    }

    [Fact]
    public void AddRelationship_InvalidToPath_DoesNotThrow()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        scope.WithAnchor("vito", vito);

        new AddRelationship<FatherOf>("vito", "nobody").Apply(scope);
    }

    [Fact]
    public void AddRelationship_Describe_ReturnsLocalizableWithRelationName()
    {
        var scope = CreateScope();
        var desc = new AddRelationship<FatherOf>("vito", "michael").Describe(scope);

        desc.Key.Should().Be("effect.add_relationship");
        desc.Args["relation"].Should().Be("fatherof");
    }

    #endregion

    #region RemoveRelationship

    [Fact]
    public void RemoveRelationship_RemovesExistingRelation()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito).WithAnchor("michael", michael);

        new RemoveRelationship<FatherOf>("vito", "michael").Apply(scope);

        vito.Has<FatherOf>(michael).Should().BeFalse();
    }

    [Fact]
    public void RemoveRelationship_NoRelation_DoesNotThrow()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        scope.WithAnchor("a", a).WithAnchor("b", b);

        new RemoveRelationship<FatherOf>("a", "b").Apply(scope);

        a.Has<FatherOf>(b).Should().BeFalse();
    }

    [Fact]
    public void RemoveRelationship_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new RemoveRelationship<FatherOf>("nobody", "ghost").Apply(scope);
    }

    [Fact]
    public void RemoveRelationship_Describe_ReturnsLocalizableWithRelationName()
    {
        var scope = CreateScope();
        var desc = new RemoveRelationship<BossOf>("a", "b").Describe(scope);

        desc.Key.Should().Be("effect.remove_relationship");
        desc.Args["relation"].Should().Be("bossof");
    }

    #endregion

    #region AddTag

    [Fact]
    public void AddTag_AddsTagToEntity()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new AddTag<Arrested>("target").Apply(scope);

        entity.Has<Arrested>().Should().BeTrue();
    }

    [Fact]
    public void AddTag_AlreadyHasTag_DoesNotThrow()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add<Arrested>();
        scope.WithAnchor("target", entity);

        new AddTag<Arrested>("target").Apply(scope);

        entity.Has<Arrested>().Should().BeTrue();
    }

    [Fact]
    public void AddTag_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new AddTag<Arrested>("nobody").Apply(scope);
    }

    [Fact]
    public void AddTag_Describe_ReturnsLocalizableWithTraitName()
    {
        var scope = CreateScope();
        var desc = new AddTag<Arrested>("target").Describe(scope);

        desc.Key.Should().Be("effect.add_trait");
        desc.Args["trait"].Should().Be("arrested");
    }

    #endregion

    #region ModifyStat

    [Fact]
    public void ModifyStat_IncreasesStatAmount()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("target", entity);

        new ModifyStat<Muscle>("target", 3).Apply(scope);

        entity.Ref<Muscle>().Amount.Should().Be(8);
    }

    [Fact]
    public void ModifyStat_DecreasesStatAmount()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("target", entity);

        new ModifyStat<Muscle>("target", -2).Apply(scope);

        entity.Ref<Muscle>().Amount.Should().Be(3);
    }

    [Fact]
    public void ModifyStat_ZeroAmount_NoChange()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("target", entity);

        new ModifyStat<Muscle>("target", 0).Apply(scope);

        entity.Ref<Muscle>().Amount.Should().Be(5);
    }

    [Fact]
    public void ModifyStat_MissingStat_DoesNotThrow()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new ModifyStat<Muscle>("target", 5).Apply(scope);

        entity.GetComponent<Muscle>().Should().BeNull();
    }

    [Fact]
    public void ModifyStat_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new ModifyStat<Muscle>("nobody", 5).Apply(scope);
    }

    [Fact]
    public void ModifyStat_ThroughRelation_ModifiesTargetEntity()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        michael.Add(new Muscle(5));
        vito.Add(new FatherOf(michael), michael);
        scope.WithAnchor("vito", vito);

        new ModifyStat<Muscle>("vito.FatherOf", 3).Apply(scope);

        michael.Ref<Muscle>().Amount.Should().Be(8);
    }

    [Fact]
    public void ModifyStat_Describe_PositiveAmount_IncludesPlusSign()
    {
        var scope = CreateScope();
        var desc = new ModifyStat<Muscle>("target", 3).Describe(scope);

        desc.Key.Should().Be("effect.modify_stat");
        desc.Args["sign"].Should().Be("+");
        desc.Args["amount"].Should().Be("3");
        desc.Args["stat"].Should().Be("muscle");
    }

    [Fact]
    public void ModifyStat_Describe_NegativeAmount_EmptySign()
    {
        var scope = CreateScope();
        var desc = new ModifyStat<Muscle>("target", -3).Describe(scope);

        desc.Args["sign"].Should().Be("");
        desc.Args["amount"].Should().Be("3");
    }

    #endregion

    #region TransferMoney

    [Fact]
    public void TransferMoney_MovesWealthBetweenEntities()
    {
        var scope = CreateScope();
        var vito = SpawnEntity();
        var michael = SpawnEntity();
        vito.Add(new Wealth { Amount = 1000 });
        michael.Add(new Wealth { Amount = 200 });
        scope.WithAnchor("root", vito).WithAnchor("target", michael);

        new TransferMoney("root", "target", 300).Apply(scope);

        vito.Ref<Wealth>().Amount.Should().Be(700);
        michael.Ref<Wealth>().Amount.Should().Be(500);
    }

    [Fact]
    public void TransferMoney_CanTransferEntireWealth()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        a.Add(new Wealth { Amount = 500 });
        b.Add(new Wealth { Amount = 0 });
        scope.WithAnchor("a", a).WithAnchor("b", b);

        new TransferMoney("a", "b", 500).Apply(scope);

        a.Ref<Wealth>().Amount.Should().Be(0);
        b.Ref<Wealth>().Amount.Should().Be(500);
    }

    [Fact]
    public void TransferMoney_InsufficientFunds_ClampsToZeroAndCreatesDebt()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        a.Add(new Wealth { Amount = 100 });
        b.Add(new Wealth { Amount = 0 });
        scope.WithAnchor("a", a).WithAnchor("b", b);

        new TransferMoney("a", "b", 250).Apply(scope);

        a.Ref<Wealth>().Amount.Should().Be(0);
        b.Ref<Wealth>().Amount.Should().Be(100);
        a.Has<Owes>(b).Should().BeTrue();
        a.Ref<Owes>(b).Amount.Should().Be(150);
    }

    [Fact]
    public void TransferMoney_ZeroWealth_FullDebt()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        a.Add(new Wealth { Amount = 0 });
        b.Add(new Wealth { Amount = 0 });
        scope.WithAnchor("a", a).WithAnchor("b", b);

        new TransferMoney("a", "b", 300).Apply(scope);

        a.Ref<Wealth>().Amount.Should().Be(0);
        b.Ref<Wealth>().Amount.Should().Be(0);
        a.Has<Owes>(b).Should().BeTrue();
        a.Ref<Owes>(b).Amount.Should().Be(300);
    }

    [Fact]
    public void TransferMoney_DebtStacks_WithExistingOwes()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        a.Add(new Wealth { Amount = 0 });
        b.Add(new Wealth { Amount = 0 });
        scope.WithAnchor("a", a).WithAnchor("b", b);

        new TransferMoney("a", "b", 100).Apply(scope);
        new TransferMoney("a", "b", 200).Apply(scope);

        a.Has<Owes>(b).Should().BeTrue();
        a.Ref<Owes>(b).Amount.Should().Be(300);
    }

    [Fact]
    public void TransferMoney_SufficientFunds_NoDebtCreated()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        a.Add(new Wealth { Amount = 500 });
        b.Add(new Wealth { Amount = 0 });
        scope.WithAnchor("a", a).WithAnchor("b", b);

        new TransferMoney("a", "b", 200).Apply(scope);

        a.Has<Owes>(b).Should().BeFalse();
    }

    [Fact]
    public void TransferMoney_FromMissingWealth_DoesNothing()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        b.Add(new Wealth { Amount = 100 });
        scope.WithAnchor("a", a).WithAnchor("b", b);

        new TransferMoney("a", "b", 50).Apply(scope);

        b.Ref<Wealth>().Amount.Should().Be(100);
    }

    [Fact]
    public void TransferMoney_ToMissingWealth_DoesNothing()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        a.Add(new Wealth { Amount = 100 });
        scope.WithAnchor("a", a).WithAnchor("b", b);

        new TransferMoney("a", "b", 50).Apply(scope);

        a.Ref<Wealth>().Amount.Should().Be(100);
    }

    [Fact]
    public void TransferMoney_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new TransferMoney("nobody", "ghost", 100).Apply(scope);
    }

    [Fact]
    public void TransferMoney_Describe_FromRoot_ShowsLoseKey()
    {
        var scope = CreateScope();
        var desc = new TransferMoney("root", "target", 300).Describe(scope);

        desc.Key.Should().Be("effect.transfer_money.lose");
        desc.Args["amount"].Should().Be("300");
    }

    [Fact]
    public void TransferMoney_Describe_FromNonRoot_ShowsGainKey()
    {
        var scope = CreateScope();
        var desc = new TransferMoney("target", "root", 300).Describe(scope);

        desc.Key.Should().Be("effect.transfer_money.gain");
        desc.Args["amount"].Should().Be("300");
    }

    #endregion

    #region TriggerEvent

    [Fact]
    public void TriggerEvent_FiresChainedEventWithSameScope()
    {
        var scope = CreateScope();
        string? firedEventId = null;
        EntityScope? firedScope = null;
        scope.ChainedEventTriggered += (id, s) =>
        {
            firedEventId = id;
            firedScope = s;
        };

        new TriggerEvent("robbery_aftermath").Apply(scope);

        firedEventId.Should().Be("robbery_aftermath");
        firedScope.Should().BeNull();
    }

    [Fact]
    public void TriggerEvent_WithNewRoot_CreatesNewScopeWithAnchor()
    {
        var scope = CreateScope();
        var michael = SpawnEntity();
        scope.WithAnchor("michael", michael);

        string? firedEventId = null;
        EntityScope? firedScope = null;
        scope.ChainedEventTriggered += (id, s) =>
        {
            firedEventId = id;
            firedScope = s;
        };

        new TriggerEvent("michael_story", "michael").Apply(scope);

        firedEventId.Should().Be("michael_story");
        firedScope.Should().NotBeNull();
        firedScope!.ResolveAnchor("root").Should().Be(michael);
    }

    [Fact]
    public void TriggerEvent_WithInvalidNewRoot_DoesNotFire()
    {
        var scope = CreateScope();
        string? firedEventId = null;
        scope.ChainedEventTriggered += (id, _) => firedEventId = id;

        new TriggerEvent("some_event", "nobody").Apply(scope);

        firedEventId.Should().BeNull();
    }

    #endregion

    #region AddMemory

    [Fact]
    public void AddMemory_CreatesMemoryOnEvaluator()
    {
        var scope = CreateScope();
        var evaluator = SpawnEntity();
        var target = SpawnEntity();
        scope.WithAnchor("root", evaluator).WithAnchor("target", target);
        var memory = new OpinionMemory { DefinitionId = "betrayed_me", Amount = -30, ExpiresOn = new GameDate(1930, 6, 1) };

        new AddMemory("root", "target", memory).Apply(scope);

        evaluator.Has<MemoriesOf>(target).Should().BeTrue();
        var memories = evaluator.Ref<MemoriesOf>(target).Memories;
        memories.Should().HaveCount(1);
        memories[0].DefinitionId.Should().Be("betrayed_me");
        memories[0].Amount.Should().Be(-30);
    }

    [Fact]
    public void AddMemory_MultipleMemories_Stack()
    {
        var scope = CreateScope();
        var evaluator = SpawnEntity();
        var target = SpawnEntity();
        scope.WithAnchor("root", evaluator).WithAnchor("target", target);

        new AddMemory("root", "target", new OpinionMemory { DefinitionId = "betrayed_me", Amount = -30, ExpiresOn = new GameDate(1930, 6, 1) }).Apply(scope);
        new AddMemory("root", "target", new OpinionMemory { DefinitionId = "saved_my_life", Amount = 40, ExpiresOn = new GameDate(1932, 1, 1) }).Apply(scope);

        var memories = evaluator.Ref<MemoriesOf>(target).Memories;
        memories.Count.Should().Be(2);
    }

    [Fact]
    public void AddMemory_DifferentTargets_SeparateRelations()
    {
        var scope = CreateScope();
        var evaluator = SpawnEntity();
        var targetA = SpawnEntity();
        var targetB = SpawnEntity();
        scope.WithAnchor("root", evaluator).WithAnchor("target", targetA);

        new AddMemory("root", "target", new OpinionMemory { DefinitionId = "grudge", Amount = -50, ExpiresOn = new GameDate(1930, 1, 1) }).Apply(scope);

        scope.WithAnchor("target", targetB);
        new AddMemory("root", "target", new OpinionMemory { DefinitionId = "favor", Amount = 20, ExpiresOn = new GameDate(1931, 1, 1) }).Apply(scope);

        evaluator.Ref<MemoriesOf>(targetA).Memories.Should().HaveCount(1);
        evaluator.Ref<MemoriesOf>(targetB).Memories.Should().HaveCount(1);
    }

    [Fact]
    public void AddMemory_InvalidRootPath_DoesNotThrow()
    {
        var scope = CreateScope();
        var target = SpawnEntity();
        scope.WithAnchor("target", target);

        new AddMemory("nobody", "target", new OpinionMemory { DefinitionId = "x", Amount = 1, ExpiresOn = new GameDate(1930, 1, 1) }).Apply(scope);
    }

    [Fact]
    public void AddMemory_InvalidTargetPath_DoesNotThrow()
    {
        var scope = CreateScope();
        var evaluator = SpawnEntity();
        scope.WithAnchor("root", evaluator);

        new AddMemory("root", "nobody", new OpinionMemory { DefinitionId = "x", Amount = 1, ExpiresOn = new GameDate(1930, 1, 1) }).Apply(scope);

        evaluator.Has<MemoriesOf>().Should().BeFalse();
    }

    #endregion

    #region ChangeRank

    [Fact]
    public void ChangeRank_SetsNewRank()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Soldier));
        scope.WithAnchor("target", entity);

        new ChangeRank("target", RankId.Caporegime).Apply(scope);

        entity.Ref<Rank>().Id.Should().Be(RankId.Caporegime);
    }

    [Fact]
    public void ChangeRank_Demote_SetsLowerRank()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Rank(RankId.Caporegime));
        scope.WithAnchor("target", entity);

        new ChangeRank("target", RankId.Soldier).Apply(scope);

        entity.Ref<Rank>().Id.Should().Be(RankId.Soldier);
    }

    [Fact]
    public void ChangeRank_NoRankComponent_DoesNotThrow()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new ChangeRank("target", RankId.Boss).Apply(scope);

        entity.Has<Rank>().Should().BeFalse();
    }

    [Fact]
    public void ChangeRank_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new ChangeRank("nobody", RankId.Boss).Apply(scope);
    }

    [Fact]
    public void ChangeRank_Describe_ReturnsLocalizableWithRankName()
    {
        var scope = CreateScope();
        var desc = new ChangeRank("target", RankId.Caporegime).Describe(scope);

        desc.Key.Should().Be("effect.change_rank");
        desc.Args["rank"].Should().Be("caporegime");
    }

    #endregion

    #region RemoveTag

    [Fact]
    public void RemoveTag_RemovesExistingTag()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add<Arrested>();
        scope.WithAnchor("target", entity);

        new RemoveTag<Arrested>("target").Apply(scope);

        entity.Has<Arrested>().Should().BeFalse();
    }

    [Fact]
    public void RemoveTag_NoTag_DoesNotThrow()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new RemoveTag<Arrested>("target").Apply(scope);

        entity.Has<Arrested>().Should().BeFalse();
    }

    [Fact]
    public void RemoveTag_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new RemoveTag<Arrested>("nobody").Apply(scope);
    }

    [Fact]
    public void RemoveTag_Describe_ReturnsLocalizableWithTraitName()
    {
        var scope = CreateScope();
        var desc = new RemoveTag<Arrested>("target").Describe(scope);

        desc.Key.Should().Be("effect.remove_trait");
        desc.Args["trait"].Should().Be("arrested");
    }

    #endregion

    #region SetStat

    [Fact]
    public void SetStat_SetsStatToExactValue()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Stress { Amount = 75 });
        scope.WithAnchor("target", entity);

        new SetStat<Stress>("target", 30).Apply(scope);

        entity.Ref<Stress>().Amount.Should().Be(30);
    }

    [Fact]
    public void SetStat_SetsToZero()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Stress { Amount = 50 });
        scope.WithAnchor("target", entity);

        new SetStat<Stress>("target", 0).Apply(scope);

        entity.Ref<Stress>().Amount.Should().Be(0);
    }

    [Fact]
    public void SetStat_ClampsToMax()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Stress { Amount = 50 });
        scope.WithAnchor("target", entity);

        new SetStat<Stress>("target", 200).Apply(scope);

        entity.Ref<Stress>().Amount.Should().Be(100);
    }

    [Fact]
    public void SetStat_ClampsToMin()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Stress { Amount = 50 });
        scope.WithAnchor("target", entity);

        new SetStat<Stress>("target", -10).Apply(scope);

        entity.Ref<Stress>().Amount.Should().Be(0);
    }

    [Fact]
    public void SetStat_MissingStat_DoesNotThrow()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new SetStat<Stress>("target", 50).Apply(scope);

        entity.Has<Stress>().Should().BeFalse();
    }

    [Fact]
    public void SetStat_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new SetStat<Stress>("nobody", 50).Apply(scope);
    }

    [Fact]
    public void SetStat_Describe_ReturnsLocalizableWithStatAndValue()
    {
        var scope = CreateScope();
        var desc = new SetStat<Stress>("target", 42).Describe(scope);

        desc.Key.Should().Be("effect.set_stat");
        desc.Args["value"].Should().Be("42");
        desc.Args["stat"].Should().Be("stress");
    }

    #endregion

    #region RemoveMemory

    [Fact]
    public void RemoveMemory_RemovesMatchingMemories()
    {
        var scope = CreateScope();
        var evaluator = SpawnEntity();
        var target = SpawnEntity();
        scope.WithAnchor("root", evaluator).WithAnchor("target", target);

        new AddMemory("root", "target", new OpinionMemory { DefinitionId = "betrayed_me", Amount = -30, ExpiresOn = new GameDate(1930, 6, 1) }).Apply(scope);
        new AddMemory("root", "target", new OpinionMemory { DefinitionId = "saved_my_life", Amount = 40, ExpiresOn = new GameDate(1932, 1, 1) }).Apply(scope);

        new RemoveMemory("root", "target", "betrayed_me").Apply(scope);

        var memories = evaluator.Ref<MemoriesOf>(target).Memories;
        memories.Should().HaveCount(1);
        memories[0].DefinitionId.Should().Be("saved_my_life");
    }

    [Fact]
    public void RemoveMemory_RemovesAllWithMatchingId()
    {
        var scope = CreateScope();
        var evaluator = SpawnEntity();
        var target = SpawnEntity();
        scope.WithAnchor("root", evaluator).WithAnchor("target", target);

        new AddMemory("root", "target", new OpinionMemory { DefinitionId = "grudge", Amount = -10, ExpiresOn = new GameDate(1930, 1, 1) }).Apply(scope);
        new AddMemory("root", "target", new OpinionMemory { DefinitionId = "grudge", Amount = -20, ExpiresOn = new GameDate(1931, 1, 1) }).Apply(scope);

        new RemoveMemory("root", "target", "grudge").Apply(scope);

        evaluator.Ref<MemoriesOf>(target).Memories.Should().BeEmpty();
    }

    [Fact]
    public void RemoveMemory_NoMatchingId_LeavesOthersIntact()
    {
        var scope = CreateScope();
        var evaluator = SpawnEntity();
        var target = SpawnEntity();
        scope.WithAnchor("root", evaluator).WithAnchor("target", target);

        new AddMemory("root", "target", new OpinionMemory { DefinitionId = "favor", Amount = 20, ExpiresOn = new GameDate(1930, 1, 1) }).Apply(scope);

        new RemoveMemory("root", "target", "nonexistent").Apply(scope);

        evaluator.Ref<MemoriesOf>(target).Memories.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveMemory_NoMemoriesRelation_DoesNotThrow()
    {
        var scope = CreateScope();
        var evaluator = SpawnEntity();
        var target = SpawnEntity();
        scope.WithAnchor("root", evaluator).WithAnchor("target", target);

        new RemoveMemory("root", "target", "anything").Apply(scope);
    }

    [Fact]
    public void RemoveMemory_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new RemoveMemory("nobody", "ghost", "anything").Apply(scope);
    }

    #endregion

    #region ClearMemories

    [Fact]
    public void ClearMemories_RemovesAllMemoriesTowardTarget()
    {
        var scope = CreateScope();
        var evaluator = SpawnEntity();
        var target = SpawnEntity();
        scope.WithAnchor("root", evaluator).WithAnchor("target", target);

        new AddMemory("root", "target", new OpinionMemory { DefinitionId = "a", Amount = -10, ExpiresOn = new GameDate(1930, 1, 1) }).Apply(scope);
        new AddMemory("root", "target", new OpinionMemory { DefinitionId = "b", Amount = 20, ExpiresOn = new GameDate(1931, 1, 1) }).Apply(scope);

        new ClearMemories("root", "target").Apply(scope);

        evaluator.Ref<MemoriesOf>(target).Memories.Should().BeEmpty();
    }

    [Fact]
    public void ClearMemories_DoesNotAffectOtherTargets()
    {
        var scope = CreateScope();
        var evaluator = SpawnEntity();
        var targetA = SpawnEntity();
        var targetB = SpawnEntity();
        scope.WithAnchor("root", evaluator).WithAnchor("a", targetA).WithAnchor("b", targetB);

        new AddMemory("root", "a", new OpinionMemory { DefinitionId = "x", Amount = -10, ExpiresOn = new GameDate(1930, 1, 1) }).Apply(scope);
        new AddMemory("root", "b", new OpinionMemory { DefinitionId = "y", Amount = 20, ExpiresOn = new GameDate(1930, 1, 1) }).Apply(scope);

        new ClearMemories("root", "a").Apply(scope);

        evaluator.Ref<MemoriesOf>(targetA).Memories.Should().BeEmpty();
        evaluator.Ref<MemoriesOf>(targetB).Memories.Should().HaveCount(1);
    }

    [Fact]
    public void ClearMemories_NoMemoriesRelation_DoesNotThrow()
    {
        var scope = CreateScope();
        var evaluator = SpawnEntity();
        var target = SpawnEntity();
        scope.WithAnchor("root", evaluator).WithAnchor("target", target);

        new ClearMemories("root", "target").Apply(scope);
    }

    [Fact]
    public void ClearMemories_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new ClearMemories("nobody", "ghost").Apply(scope);
    }

    #endregion

    #region ChangeNickname

    [Fact]
    public void ChangeNickname_UpdatesNickname()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Identity("Salvatore", "Sonny", 35, Gender.Male));
        scope.WithAnchor("target", entity);

        new ChangeNickname("target", "The Bull").Apply(scope);

        entity.Ref<Identity>().NickName.Should().Be("The Bull");
    }

    [Fact]
    public void ChangeNickname_PreservesOtherFields()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Identity("Salvatore", "Sonny", 35, Gender.Male));
        scope.WithAnchor("target", entity);

        new ChangeNickname("target", "The Bull").Apply(scope);

        var id = entity.Ref<Identity>();
        id.Name.Should().Be("Salvatore");
        id.Age.Should().Be(35);
        id.Gender.Should().Be(Gender.Male);
    }

    [Fact]
    public void ChangeNickname_NoIdentityComponent_DoesNotThrow()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new ChangeNickname("target", "Ghost").Apply(scope);

        entity.Has<Identity>().Should().BeFalse();
    }

    [Fact]
    public void ChangeNickname_InvalidPath_DoesNotThrow()
    {
        var scope = CreateScope();

        new ChangeNickname("nobody", "Ghost").Apply(scope);
    }

    [Fact]
    public void ChangeNickname_Describe_ReturnsLocalizableWithNickname()
    {
        var scope = CreateScope();
        var desc = new ChangeNickname("target", "The Bull").Describe(scope);

        desc.Key.Should().Be("effect.change_nickname");
        desc.Args["nickname"].Should().Be("The Bull");
    }

    #endregion
}
