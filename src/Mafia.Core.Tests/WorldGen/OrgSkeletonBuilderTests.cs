using FluentAssertions;
using Mafia.Core.WorldGen;
using Mafia.Core.Ecs.Components.Rank;
using Xunit;

namespace Mafia.Core.Tests.WorldGen;

public class OrgSkeletonBuilderTests
{
    [Fact]
    public void BuildAll_CreatesCorrectNumberOfOrgs()
    {
        var config = new WorldConfig { OrgCount = 5 };
        var rng = new SeededRandom(42);
        var nameGen = new NameGenerator(rng, new TestNameRepository());
        var builder = new OrgSkeletonBuilder(rng, config);

        var orgs = builder.BuildAll(nameGen);

        orgs.Should().HaveCount(5);
    }

    [Fact]
    public void BuildAll_EachOrgHasUniqueSurname()
    {
        var config = new WorldConfig { OrgCount = 5 };
        var rng = new SeededRandom(42);
        var nameGen = new NameGenerator(rng, new TestNameRepository());
        var builder = new OrgSkeletonBuilder(rng, config);

        var orgs = builder.BuildAll(nameGen);
        var surnames = orgs.Select(o => o.Surname).ToHashSet();

        surnames.Should().HaveCount(5);
    }

    [Fact]
    public void BuildAll_EachOrgHasBossAndCapos()
    {
        var config = new WorldConfig { OrgCount = 3, MinCapos = 2, MaxCapos = 4 };
        var rng = new SeededRandom(42);
        var nameGen = new NameGenerator(rng, new TestNameRepository());
        var builder = new OrgSkeletonBuilder(rng, config);

        var orgs = builder.BuildAll(nameGen);

        foreach (var org in orgs)
        {
            org.Boss.Rank.Should().Be(RankId.Boss);
            org.Capos.Count.Should().BeInRange(2, 4);
            org.Capos[0].IsUnderboss.Should().BeTrue();

            foreach (var capo in org.Capos.Skip(1))
                capo.IsUnderboss.Should().BeFalse();
        }
    }

    [Fact]
    public void CountSlots_ReturnsCorrectTotal()
    {
        var config = new WorldConfig
        {
            OrgCount = 1, MinCapos = 2, MaxCapos = 2,
            MinSoldiersPerCapo = 2, MaxSoldiersPerCapo = 2,
            MinAssociatesPerSoldier = 1, MaxAssociatesPerSoldier = 1
        };
        var rng = new SeededRandom(42);
        var nameGen = new NameGenerator(rng, new TestNameRepository());
        var builder = new OrgSkeletonBuilder(rng, config);

        var orgs = builder.BuildAll(nameGen);
        // 1 boss + 2 capos + 4 soldiers + 4 associates = 11
        orgs[0].CountSlots().Should().Be(11);
    }
}
