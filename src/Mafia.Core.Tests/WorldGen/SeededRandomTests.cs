using FluentAssertions;
using Mafia.Core.WorldGen;
using Xunit;

namespace Mafia.Core.Tests.WorldGen;

public class SeededRandomTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var a = new SeededRandom(123);
        var b = new SeededRandom(123);

        var seqA = Enumerable.Range(0, 100).Select(_ => a.Next(1000)).ToList();
        var seqB = Enumerable.Range(0, 100).Select(_ => b.Next(1000)).ToList();

        seqA.Should().Equal(seqB);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var a = new SeededRandom(1);
        var b = new SeededRandom(2);

        var seqA = Enumerable.Range(0, 20).Select(_ => a.Next(1000)).ToList();
        var seqB = Enumerable.Range(0, 20).Select(_ => b.Next(1000)).ToList();

        seqA.Should().NotEqual(seqB);
    }

    [Fact]
    public void Poisson_MeanApproximatesLambda()
    {
        var rng = new SeededRandom(42);
        const double lambda = 3.0;
        const int samples = 10000;

        var sum = Enumerable.Range(0, samples).Sum(_ => rng.Poisson(lambda));
        var mean = (double)sum / samples;

        mean.Should().BeApproximately(lambda, 0.2);
    }

    [Fact]
    public void StatRoll_AlwaysInRange()
    {
        var rng = new SeededRandom(42);

        for (var i = 0; i < 1000; i++)
        {
            var value = rng.StatRoll();
            value.Should().BeInRange(1, 10);
        }
    }

    [Fact]
    public void Chance_RespectsApproximateProbability()
    {
        var rng = new SeededRandom(42);
        const int samples = 10000;

        var hits = Enumerable.Range(0, samples).Count(_ => rng.Chance(0.3));
        var rate = (double)hits / samples;

        rate.Should().BeApproximately(0.3, 0.05);
    }

    [Fact]
    public void Pick_ReturnsElementFromList()
    {
        var rng = new SeededRandom(42);
        var list = new[] { "a", "b", "c", "d" };

        for (var i = 0; i < 100; i++)
        {
            var pick = rng.Pick(list);
            list.Should().Contain(pick);
        }
    }
}
