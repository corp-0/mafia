namespace Mafia.Core.Time;

public class GameState(GameDate startDate)
{
    public GameDate CurrentDate { get; set; } = startDate;
}
