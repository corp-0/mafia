using fennecs;
using Mafia.Core.Ecs.Blueprints.Phases;

namespace Mafia.Core.Ecs.Blueprints;

public static class WorldGenerator
{
    public static Dictionary<string, Entity> Generate(World world, WorldConfig? config = null)
    {
        config ??= new WorldConfig();

        var rng = new SeededRandom(1018);
        var nameGen = new NameGenerator(rng);
        var statRoller = new StatRoller(rng, config);
        var factory = new CharacterFactory(world);

        var ctx = new WorldGenerationContext
        {
            World = world,
            Config = config,
            Rng = rng,
            NameGen = nameGen,
            StatRoller = statRoller,
            CharacterFactory = factory
        };

        IGenerationPhase[] phases =
        [
            new OrgSkeletonPhase(),
            new AnchorSpawningPhase(),
            new HouseholdPhase(),
            new FillerPopulationPhase(),
            new CrossFamilyLinksPhase(),
            new NepotismPhase(),
            new RelationResolutionPhase(),
            new HouseholdEntityPhase()
        ];

        foreach (var phase in phases)
            phase.Execute(ctx);

        var validationErrors = WorldValidator.Validate(ctx);
        if (validationErrors.Count > 0)
        {
            Console.WriteLine("=== WORLD VALIDATION WARNINGS ===");
            foreach (var error in validationErrors)
                Console.WriteLine($"  WARNING: {error}");
        }

        return ctx.Roster;
    }
}
