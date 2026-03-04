using fennecs;
using Mafia.Core.Content.Registries;
using Mafia.Core.WorldGen.Phases;
using Microsoft.Extensions.Logging;

namespace Mafia.Core.WorldGen;

public static class WorldGenerator
{
    public static Dictionary<string, Entity> Generate(World world, INameRepository nameRepository, WorldConfig? config = null, ILogger? logger = null)
    {
        config ??= new WorldConfig();

        var rng = new SeededRandom(1018);
        var nameGen = new NameGenerator(rng, nameRepository);
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
            foreach (var error in validationErrors)
                logger?.LogWarning("World validation: {Warning}", error);
        }

        return ctx.Roster;
    }
}
