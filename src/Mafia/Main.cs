using Godot;
using Mafia.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using static Serilog.Events.LogEventLevel;

namespace Mafia;

[GlobalClass]
public partial class Main : Node
{
    [Export] private PackedScene GameScene { get; set; } = null!;
    [Export] private PackedScene EditorScene { get; set; } = null!;

    public override void _Ready()
    {
        ConfigureSerilog();

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        var args = OS.GetCmdlineArgs();
        if (args.Length > 0 && args.Contains("--editor"))
        {
            GameServices.Initialize(services);
            var editor = EditorScene.Instantiate<Node>();
            AddChild(editor);
            return;
        }

        var game = GameScene.Instantiate<Game>();
        game.ConfigureServices(services);
        AddChild(game);
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
            .WriteTo.Sink(new GodotSink())
            .WriteTo.Console()
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
