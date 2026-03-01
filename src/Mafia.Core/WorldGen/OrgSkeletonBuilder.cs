using Mafia.Core.Ecs.Components.Rank;

namespace Mafia.Core.WorldGen;

public class OrgSkeletonBuilder(SeededRandom rng, WorldConfig config)
{
    public List<OrgSkeleton> BuildAll(NameGenerator nameGen)
    {
        var orgs = new List<OrgSkeleton>();
        var usedSurnames = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < config.OrgCount; i++)
        {
            var surname = nameGen.PickUniqueSurname(usedSurnames);
            usedSurnames.Add(surname);
            orgs.Add(BuildOrg(surname));
        }

        return orgs;
    }

    private OrgSkeleton BuildOrg(string surname)
    {
        var capoCount = rng.Next(config.MinCapos, config.MaxCapos + 1);
        var capos = new List<CapoSkeleton>();

        for (var c = 0; c < capoCount; c++)
        {
            var soldierCount = rng.Next(config.MinSoldiersPerCapo, config.MaxSoldiersPerCapo + 1);
            var soldiers = new List<SoldierSkeleton>();

            for (var s = 0; s < soldierCount; s++)
            {
                var associateCount = rng.Next(config.MinAssociatesPerSoldier, config.MaxAssociatesPerSoldier + 1);
                var associates = new List<OrgSlot>();

                for (var a = 0; a < associateCount; a++)
                    associates.Add(new OrgSlot { Rank = RankId.Associate });

                soldiers.Add(new SoldierSkeleton
                {
                    Slot = new OrgSlot { Rank = RankId.Soldier },
                    Associates = associates
                });
            }

            capos.Add(new CapoSkeleton
            {
                Slot = new OrgSlot { Rank = RankId.Caporegime },
                IsUnderboss = c == 0,
                Soldiers = soldiers
            });
        }

        return new OrgSkeleton
        {
            Surname = surname,
            Boss = new OrgSlot { Rank = RankId.Boss },
            Capos = capos
        };
    }
}
