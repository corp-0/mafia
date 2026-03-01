using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;

namespace Mafia.Core.WorldGen;

public class StatRoller(SeededRandom rng, WorldConfig config)
{
    public CharacterBlueprint RollOrgMember(string name, string surname, RankId rank)
    {
        var (meanStat, stddev, wealthMin, wealthMax) = rank switch
        {
            RankId.Boss => (7.0, 1.0, 30000, 100000),
            RankId.Caporegime => (6.0, 1.2, 10000, 40000),
            RankId.Soldier => (5.5, 1.5, 2000, 15000),
            _ => (4.5, 1.5, 100, 5000)
        };

        return new CharacterBlueprint
        {
            Name = name,
            Surname = surname,
            Age = RollAge(rank),
            Sex = Sex.Male,
            Rank = rank,
            Muscle = rng.StatRoll(meanStat, stddev),
            Nerve = rng.StatRoll(meanStat, stddev),
            Brains = rng.StatRoll(meanStat, stddev),
            Charm = rng.StatRoll(meanStat, stddev),
            Instinct = rng.StatRoll(meanStat, stddev),
            Wealth = rng.Next(wealthMin, wealthMax + 1)
        };
    }

    public CharacterBlueprint RollCivilian(string name, string surname, Sex sex, int? age = null)
    {
        return new CharacterBlueprint
        {
            Name = name,
            Surname = surname,
            Age = age ?? RollAge(null),
            Sex = sex,
            Muscle = rng.StatRoll(4.5, 1.5),
            Nerve = rng.StatRoll(4.5, 1.5),
            Brains = rng.StatRoll(5.0, 1.5),
            Charm = rng.StatRoll(5.0, 1.5),
            Instinct = rng.StatRoll(4.5, 1.5),
            Wealth = rng.Next(100, 10000)
        };
    }

    public int RollAge(RankId? rank)
    {
        return rank switch
        {
            RankId.Boss => rng.Next(45, config.MaxAge + 1),
            RankId.Caporegime => rng.Next(35, 65),
            RankId.Soldier => rng.Next(25, 55),
            RankId.Associate => rng.Next(config.MinAdultAge, 50),
            _ => rng.Next(config.MinAdultAge, config.MaxAge + 1)
        };
    }

    public int RollSpouseAge(int partnerAge)
    {
        var diff = rng.Next(-config.MaxSpouseAgeDiff, config.MaxSpouseAgeDiff + 1);
        return Math.Clamp(partnerAge + diff, config.MinMarriageAge, config.MaxAge);
    }

    public int RollChildAge(int youngestParentAge)
    {
        var maxChildAge = youngestParentAge - config.MinParentChildAgeDiff;
        if (maxChildAge < 1) return 1;
        return rng.Next(1, Math.Min(maxChildAge, 40) + 1);
    }
}
