namespace Mafia.Core.Ecs.Blueprints;

public sealed class SeededRandom
{
    private readonly Random _rng;

    public SeededRandom(int seed)
    {
        _rng = new Random(seed);
    }

    public int Next(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);

    public int Next(int maxExclusive) => _rng.Next(maxExclusive);

    public double NextDouble() => _rng.NextDouble();

    public bool Chance(double probability) => _rng.NextDouble() < probability;

    public T Pick<T>(IReadOnlyList<T> list) => list[_rng.Next(list.Count)];

    public int Poisson(double lambda)
    {
        // Knuth algorithm
        var l = Math.Exp(-lambda);
        var k = 0;
        var p = 1.0;

        do
        {
            k++;
            p *= _rng.NextDouble();
        } while (p > l);

        return k - 1;
    }

    public double Normal(double mean, double stddev)
    {
        // Box-Muller transform
        var u1 = 1.0 - _rng.NextDouble();
        var u2 = _rng.NextDouble();
        var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        return mean + stddev * z;
    }

    public int StatRoll(double mean = 5.0, double stddev = 1.5)
    {
        var value = (int)Math.Round(Normal(mean, stddev));
        return Math.Clamp(value, 1, 10);
    }

    public void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
