namespace Mafia.Core.Time;

public class GameClock(GameState state)
{
    private static readonly Dictionary<SimulationSpeed, double> SecondsPerDay = new()
    {
        [SimulationSpeed.Paused] = 0,
        [SimulationSpeed.Normal] = 10.0,
        [SimulationSpeed.Fast] = 5.0,
        [SimulationSpeed.VeryFast] = 2.0,
        [SimulationSpeed.Ultra] = 1.0,
    };

    private double _accumulatedSeconds;

    public GameState State { get; } = state;
    public SimulationSpeed Speed { get; set; } = SimulationSpeed.Normal;

    /// <summary>
    /// Advances the clock by the given real-time delta.
    /// Returns the number of hour-ticks to process.
    /// </summary>
    public int Advance(double realDeltaSeconds)
    {
        if (Speed == SimulationSpeed.Paused)
            return 0;

        _accumulatedSeconds += realDeltaSeconds;

        var secondsPerHour = SecondsPerDay[Speed] / 24.0;
        var ticks = (int)(_accumulatedSeconds / secondsPerHour);
        _accumulatedSeconds -= ticks * secondsPerHour;
        return ticks;
    }

    public void ResetAccumulator() => _accumulatedSeconds = 0;
}
