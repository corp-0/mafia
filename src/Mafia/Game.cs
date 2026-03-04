using fennecs;
using Godot;
using Mafia.Content;
using Mafia.Core;
using Mafia.Core.Content.Registries;
using Mafia.Core.Time;
using Mafia.Core.WorldGen;
using Mafia.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mafia;

[GlobalClass]
public partial class Game : Node
{
    private SimulationTicker _ticker = null!;
    private ILogger<Game> _logger = null!;
    private GameState _gameState = null!;

    public void ConfigureServices(ServiceCollection services)
    {
        var world = new World();
        _gameState = new GameState(new GameDate(1920, 1, 1));

        services.AddSingleton(world);
        services.AddSingleton(_gameState);
        CoreStartup.ConfigureServices(services);
        GameServices.Initialize(services);
    }

    public override void _Ready()
    {
        _logger = GameServices.Get<ILogger<Game>>();

        ContentBootstrapper.LoadAllContent();

        var world = GameServices.Get<World>();
        var nameRepo = GameServices.Get<INameRepository>();
        var roster = WorldGenerator.Generate(world, nameRepo, logger: _logger);
        WorldPrinter.Print(roster, msg => _logger.LogInformation("{WorldPrint}", msg));

        _ticker = new SimulationTicker();
        AddChild(_ticker);
        _ticker.Initialize();

        _logger.LogInformation("Simulation ready at {CurrentDate}", _gameState.CurrentDate);
    }
}
