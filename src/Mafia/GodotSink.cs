using Godot;
using Serilog.Core;
using Serilog.Events;

namespace Mafia;

/// <summary>
/// Serilog sink that routes log output to Godot's <see cref="GD.Print"/> so it
/// appears in the editor Output panel.
/// </summary>
public sealed class GodotSink(IFormatProvider? formatProvider = null) : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage(formatProvider);

        var source = logEvent.Properties.TryGetValue("SourceContext", out var ctx)
            ? ctx.ToString().Trim('"')
            : "";

        // Shorten fully-qualified type names to just the class name
        var dot = source.LastIndexOf('.');
        if (dot >= 0) source = source[(dot + 1)..];

        var prefix = $"[{logEvent.Level:u3}]";
        var tag = string.IsNullOrEmpty(source) ? "" : $" [{source}]";

        GD.Print($"{prefix}{tag} {message}");

        if (logEvent.Exception is { } ex)
            GD.PrintErr(ex.ToString());
    }
}
