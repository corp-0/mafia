using System.Globalization;

namespace Mafia.Core.Time;

/// <summary>
/// A lightweight calendar date for the game world.
/// Internally stores a total day count for fast comparison and arithmetic.
/// </summary>
public readonly struct GameDate : IEquatable<GameDate>, IComparable<GameDate>
{
    private readonly int _totalDays;

    public int Year { get; }
    public int Month { get; }
    public int Day { get; }

    public GameDate(int year, int month, int day)
    {
        Year = year;
        Month = month;
        Day = day;
        _totalDays = ToDays(year, month, day);
    }

    private GameDate(int year, int month, int day, int totalDays)
    {
        Year = year;
        Month = month;
        Day = day;
        _totalDays = totalDays;
    }

    public int DaysSince(GameDate other) => _totalDays - other._totalDays;

    public GameDate AddDays(int days)
    {
        var dt = ToDateTime().AddDays(days);
        return new GameDate(dt.Year, dt.Month, dt.Day, _totalDays + days);
    }

    public static GameDate Parse(string s)
    {
        var dt = DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new GameDate(dt.Year, dt.Month, dt.Day);
    }

    public static bool TryParse(string s, out GameDate result)
    {
        if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            result = new GameDate(dt.Year, dt.Month, dt.Day);
            return true;
        }

        result = default;
        return false;
    }

    // Comparison operators
    public int CompareTo(GameDate other) => _totalDays.CompareTo(other._totalDays);
    public bool Equals(GameDate other) => _totalDays == other._totalDays;
    public override bool Equals(object? obj) => obj is GameDate other && Equals(other);
    public override int GetHashCode() => _totalDays;

    public static bool operator ==(GameDate left, GameDate right) => left._totalDays == right._totalDays;
    public static bool operator !=(GameDate left, GameDate right) => left._totalDays != right._totalDays;
    public static bool operator <(GameDate left, GameDate right) => left._totalDays < right._totalDays;
    public static bool operator >(GameDate left, GameDate right) => left._totalDays > right._totalDays;
    public static bool operator <=(GameDate left, GameDate right) => left._totalDays <= right._totalDays;
    public static bool operator >=(GameDate left, GameDate right) => left._totalDays >= right._totalDays;

    public override string ToString() => $"{Year:D4}-{Month:D2}-{Day:D2}";

    private DateTime ToDateTime() => new(Year, Month, Day);

    private static int ToDays(int year, int month, int day) =>
        (int)(new DateTime(year, month, day).Ticks / TimeSpan.TicksPerDay);
}
