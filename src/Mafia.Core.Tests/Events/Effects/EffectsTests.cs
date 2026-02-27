using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects;
using Xunit;

namespace Mafia.Core.Tests.Events.Effects;

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

        Assert.True(scope.HasRelation<FatherOf>("vito", "michael"));
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

        Assert.True(scope.HasRelation<FatherOf>("vito", "michael"));
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

        Assert.Equal("effect.add_relationship", desc.Key);
        Assert.Equal("fatherof", desc.Args["relation"]);
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

        Assert.False(scope.HasRelation<FatherOf>("vito", "michael"));
    }

    [Fact]
    public void RemoveRelationship_NoRelation_DoesNotThrow()
    {
        var scope = CreateScope();
        var a = SpawnEntity();
        var b = SpawnEntity();
        scope.WithAnchor("a", a).WithAnchor("b", b);

        new RemoveRelationship<FatherOf>("a", "b").Apply(scope);

        Assert.False(scope.HasRelation<FatherOf>("a", "b"));
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

        Assert.Equal("effect.remove_relationship", desc.Key);
        Assert.Equal("bossof", desc.Args["relation"]);
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

        Assert.True(scope.HasTag<Arrested>("target"));
    }

    [Fact]
    public void AddTag_AlreadyHasTag_DoesNotThrow()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add<Arrested>();
        scope.WithAnchor("target", entity);

        new AddTag<Arrested>("target").Apply(scope);

        Assert.True(scope.HasTag<Arrested>("target"));
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

        Assert.Equal("effect.add_trait", desc.Key);
        Assert.Equal("arrested", desc.Args["trait"]);
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

        Assert.Equal(8, scope.GetComponent<Muscle>("target")!.Value.Amount);
    }

    [Fact]
    public void ModifyStat_DecreasesStatAmount()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("target", entity);

        new ModifyStat<Muscle>("target", -2).Apply(scope);

        Assert.Equal(3, scope.GetComponent<Muscle>("target")!.Value.Amount);
    }

    [Fact]
    public void ModifyStat_ZeroAmount_NoChange()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        entity.Add(new Muscle(5));
        scope.WithAnchor("target", entity);

        new ModifyStat<Muscle>("target", 0).Apply(scope);

        Assert.Equal(5, scope.GetComponent<Muscle>("target")!.Value.Amount);
    }

    [Fact]
    public void ModifyStat_MissingStat_DoesNotThrow()
    {
        var scope = CreateScope();
        var entity = SpawnEntity();
        scope.WithAnchor("target", entity);

        new ModifyStat<Muscle>("target", 5).Apply(scope);

        Assert.Null(scope.GetComponent<Muscle>("target"));
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

        Assert.Equal(8, michael.Ref<Muscle>().Amount);
    }

    [Fact]
    public void ModifyStat_Describe_PositiveAmount_IncludesPlusSign()
    {
        var scope = CreateScope();
        var desc = new ModifyStat<Muscle>("target", 3).Describe(scope);

        Assert.Equal("effect.modify_stat", desc.Key);
        Assert.Equal("+", desc.Args["sign"]);
        Assert.Equal("3", desc.Args["amount"]);
        Assert.Equal("muscle", desc.Args["stat"]);
    }

    [Fact]
    public void ModifyStat_Describe_NegativeAmount_EmptySign()
    {
        var scope = CreateScope();
        var desc = new ModifyStat<Muscle>("target", -3).Describe(scope);

        Assert.Equal("", desc.Args["sign"]);
        Assert.Equal("3", desc.Args["amount"]);
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

        Assert.Equal(700, vito.Ref<Wealth>().Amount);
        Assert.Equal(500, michael.Ref<Wealth>().Amount);
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

        Assert.Equal(0, a.Ref<Wealth>().Amount);
        Assert.Equal(500, b.Ref<Wealth>().Amount);
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

        Assert.Equal(0, a.Ref<Wealth>().Amount);
        Assert.Equal(100, b.Ref<Wealth>().Amount);
        Assert.True(a.Has<Owes>(b));
        Assert.Equal(150, a.Ref<Owes>(b).Amount);
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

        Assert.Equal(0, a.Ref<Wealth>().Amount);
        Assert.Equal(0, b.Ref<Wealth>().Amount);
        Assert.True(a.Has<Owes>(b));
        Assert.Equal(300, a.Ref<Owes>(b).Amount);
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

        Assert.True(a.Has<Owes>(b));
        Assert.Equal(300, a.Ref<Owes>(b).Amount);
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

        Assert.False(a.Has<Owes>(b));
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

        Assert.Equal(100, b.Ref<Wealth>().Amount);
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

        Assert.Equal(100, a.Ref<Wealth>().Amount);
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

        Assert.Equal("effect.transfer_money.lose", desc.Key);
        Assert.Equal("300", desc.Args["amount"]);
    }

    [Fact]
    public void TransferMoney_Describe_FromNonRoot_ShowsGainKey()
    {
        var scope = CreateScope();
        var desc = new TransferMoney("target", "root", 300).Describe(scope);

        Assert.Equal("effect.transfer_money.gain", desc.Key);
        Assert.Equal("300", desc.Args["amount"]);
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

        Assert.Equal("robbery_aftermath", firedEventId);
        Assert.Null(firedScope);
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

        Assert.Equal("michael_story", firedEventId);
        Assert.NotNull(firedScope);
        Assert.Equal(michael, firedScope!.ResolveAnchor("root"));
    }

    [Fact]
    public void TriggerEvent_WithInvalidNewRoot_DoesNotFire()
    {
        var scope = CreateScope();
        string? firedEventId = null;
        scope.ChainedEventTriggered += (id, _) => firedEventId = id;

        new TriggerEvent("some_event", "nobody").Apply(scope);

        Assert.Null(firedEventId);
    }

    #endregion
}
