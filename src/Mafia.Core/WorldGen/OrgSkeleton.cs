using Mafia.Core.Ecs.Components.Rank;

namespace Mafia.Core.WorldGen;

public class OrgSkeleton
{
    public required string Surname { get; init; }
    public OrgSlot Boss { get; init; } = new() { Rank = RankId.Boss };
    public List<CapoSkeleton> Capos { get; init; } = [];

    public int CountSlots()
    {
        var count = 1; // Boss
        foreach (var capo in Capos)
        {
            count++; // Capo
            foreach (var soldier in capo.Soldiers)
            {
                count++; // Soldier
                count += soldier.Associates.Count;
            }
        }
        return count;
    }
}

public class CapoSkeleton
{
    public OrgSlot Slot { get; init; } = new() { Rank = RankId.Caporegime };
    public bool IsUnderboss { get; init; }
    public List<SoldierSkeleton> Soldiers { get; init; } = [];
}

public class SoldierSkeleton
{
    public OrgSlot Slot { get; init; } = new() { Rank = RankId.Soldier };
    public List<OrgSlot> Associates { get; init; } = [];
}

public class OrgSlot
{
    public string Id { get; set; } = "";
    public RankId Rank { get; init; } = RankId.Associate;
}
