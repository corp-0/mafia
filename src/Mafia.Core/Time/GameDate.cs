using System.Globalization;

namespace Mafia.Core.Time;

/// <summary>
/// A lightweight calendar date for the game world.
/// Internally stores a total day count for fast comparison and arithmetic.
/// </summary>
public readonly struct GameDate : IEquatable<GameDate>, IComparable<GameDate>
{
    private readonly int _totalHours;

    public int Year { get; }
    public int Month { get; }
    public int Day { get; }
    public int Hour { get; }

    public GameDate(int year, int month, int day, int hour = 0)
    {
        Year = year;
        Month = month;
        Day = day;
        Hour = hour;
        _totalHours = ToDays(year, month, day) * 24 + hour;
    }

    private GameDate(int year, int month, int day, int hour, int totalHours)
    {
        Year = year;
        Month = month;
        Day = day;
        Hour = hour;
        _totalHours = totalHours;
    }

    public int DaysSince(GameDate other) => (_totalHours - other._totalHours) / 24;

    public int HoursSince(GameDate other) => _totalHours - other._totalHours;

    public GameDate AddDays(int days) => AddHours(days * 24);

    public GameDate AddHours(int hours)
    {
        var newTotal = _totalHours + hours;
        var totalDays = Math.DivRem(newTotal, 24, out var newHour);
        var dt = DateTime.MinValue.AddDays(totalDays);
        return new GameDate(dt.Year, dt.Month, dt.Day, newHour, newTotal);
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
    public int CompareTo(GameDate other) => _totalHours.CompareTo(other._totalHours);
    public bool Equals(GameDate other) => _totalHours == other._totalHours;
    public override bool Equals(object? obj) => obj is GameDate other && Equals(other);
    public override int GetHashCode() => _totalHours;

    public static bool operator ==(GameDate left, GameDate right) => left._totalHours == right._totalHours;
    public static bool operator !=(GameDate left, GameDate right) => left._totalHours != right._totalHours;
    public static bool operator <(GameDate left, GameDate right) => left._totalHours < right._totalHours;
    public static bool operator >(GameDate left, GameDate right) => left._totalHours > right._totalHours;
    public static bool operator <=(GameDate left, GameDate right) => left._totalHours <= right._totalHours;
    public static bool operator >=(GameDate left, GameDate right) => left._totalHours >= right._totalHours;

    public override string ToString() => Hour == 0
        ? $"{Year:D4}-{Month:D2}-{Day:D2}"
        : $"{Year:D4}-{Month:D2}-{Day:D2} {Hour:D2}:00";

    private DateTime ToDateTime() => new(Year, Month, Day);

    private static int ToDays(int year, int month, int day) =>
        (int)(new DateTime(year, month, day).Ticks / TimeSpan.TicksPerDay);
}
