using fennecs;

namespace Mafia.Core.Ecs.Blueprints;

public class WorldGenerationContext
{
    public required World World { get; init; }
    public required WorldConfig Config { get; init; }
    public required SeededRandom Rng { get; init; }
    public required NameGenerator NameGen { get; init; }
    public required StatRoller StatRoller { get; init; }
    public required CharacterFactory CharacterFactory { get; init; }

    public Dictionary<string, Entity> Roster { get; } = new();
    public List<OrgSkeleton> Orgs { get; set; } = [];
    public Dictionary<string, Entity> OrgBossEntities { get; } = new();
    public Dictionary<string, Entity> OrgEntities { get; } = new();

    public int PopulationCount => Roster.Count;

    private int _nextId;

    public string NextId()
    {
        _nextId++;
        return $"c{_nextId:D4}";
    }

    public Entity SpawnCharacter(CharacterBlueprint blueprint)
    {
        var id = NextId();
        var entity = CharacterFactory.Spawn(blueprint);
        Roster[id] = entity;
        return entity;
    }
}
