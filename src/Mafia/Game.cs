using fennecs;
using Godot;
using Mafia.Content;
using Mafia.Core;
using Mafia.Core.Time;
using Mafia.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using static Serilog.Events.LogEventLevel;

namespace Mafia;

[GlobalClass]
public partial class Game : Node
{
    private SimulationTicker _ticker = null!;
    private ILogger<Game> _logger = null!;

    public override void _Ready()
    {
        ConfigureSerilog();

        var world = new World();
        var gameState = new GameState(new GameDate(1920, 1, 1));

        var services = new ServiceCollection();
        services.AddSingleton(world);
        services.AddSingleton(gameState);
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });
        CoreStartup.ConfigureServices(services);
        GameServices.Initialize(services);

        _logger = GameServices.Get<ILogger<Game>>();

        ContentBootstrapper.LoadAllContent();

        _ticker = new SimulationTicker();
        AddChild(_ticker);
        _ticker.Initialize();

        _logger.LogInformation("Simulation ready at {CurrentDate}", gameState.CurrentDate);
    }

    public override void _ExitTree()
    {
        Log.CloseAndFlush();
    }

    private static void ConfigureSerilog()
    {
        var logPath = ResolveLogPath();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(Debug)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logPath,
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 10 * 1024 * 1024)
            .CreateLogger();
    }

    private static string ResolveLogPath()
    {
        var baseDir = OS.HasFeature("editor") 
            ? ProjectSettings.GlobalizePath("res://") 
            : Path.GetDirectoryName(OS.GetExecutablePath())!;

        var logsDir = Path.Combine(baseDir, "logs");
        Directory.CreateDirectory(logsDir);
        return Path.Combine(logsDir, "mafia-.log");
    }
}
