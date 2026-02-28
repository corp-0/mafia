using FluentAssertions;
using Mafia.Core.Content.Registries;
using Mafia.Core.Events.Conditions;
using Mafia.Core.Events.Conditions.Interfaces;
using Mafia.Core.Opinions;
using Xunit;

namespace Mafia.Core.Tests.Content.Registries;

public class OpinionRuleRepositoryTests
{
    private readonly OpinionRuleRepository _repo = new();

    private static OpinionRuleDefinition MakeRule(string id = "rule_1") => new()
    {
        Id = id,
        Modifier = 10,
        TooltipKey = $"opinion.{id}",
        Conditions = new HasCustomTag("family_member", "target"),
    };

    // ═══════════════════════════════════════════════
    //  Register / GetById
    // ═══════════════════════════════════════════════

    [Fact]
    public void Register_StoresRuleById()
    {
        var rule = MakeRule();
        _repo.Register(rule);

        _repo.GetById("rule_1").Should().BeSameAs(rule);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        _repo.GetById("nonexistent").Should().BeNull();
    }

    [Fact]
    public void Register_DuplicateId_ReplacesOldEntry()
    {
        var original = MakeRule("dup");
        _repo.Register(original);

        var replacement = new OpinionRuleDefinition
        {
            Id = "dup",
            Modifier = 99,
            TooltipKey = "opinion.replaced",
            Conditions = new HasCustomTag("rival", "target"),
        };
        _repo.Register(replacement);

        _repo.GetById("dup").Should().BeSameAs(replacement);
        _repo.GetAll().Should().HaveCount(1);
    }

    // ═══════════════════════════════════════════════
    //  GetAll
    // ═══════════════════════════════════════════════

    [Fact]
    public void GetAll_ReturnsAllRegisteredRules()
    {
        _repo.Register(MakeRule("r1"));
        _repo.Register(MakeRule("r2"));
        _repo.Register(MakeRule("r3"));

        _repo.GetAll().Should().HaveCount(3);
    }

    [Fact]
    public void GetAll_NoRulesRegistered_ReturnsEmptyList()
    {
        _repo.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void GetAll_AfterUpsert_ContainsOnlyLatestVersion()
    {
        _repo.Register(MakeRule("r1"));
        _repo.Register(MakeRule("r2"));

        var replacement = new OpinionRuleDefinition
        {
            Id = "r1",
            Modifier = -5,
            TooltipKey = "opinion.updated",
            Conditions = new HasCustomTag("enemy", "target"),
        };
        _repo.Register(replacement);

        _repo.GetAll().Should().HaveCount(2);
        _repo.GetAll().Should().Contain(replacement);
    }
}
