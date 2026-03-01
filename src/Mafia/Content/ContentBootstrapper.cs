using Godot;
using Mafia.Core;
using Mafia.Core.Content;
using Microsoft.Extensions.Logging;

namespace Mafia.Content;

public static class ContentBootstrapper
{
    public static void LoadAllContent()
    {
        var logger = GameServices.Get<ILoggerFactory>().CreateLogger(typeof(ContentBootstrapper));

        var contentPath = ResolveContentPath();
        logger.LogInformation("Loading content from: {ContentPath}", contentPath);

        var loader = GameServices.Get<ContentLoader>();
        var packs = loader.DiscoverPacks(contentPath);

        logger.LogInformation("Discovered {PackCount} pack(s)", packs.Count);
        foreach (ContentPackDefinition pack in packs)
            logger.LogInformation("  - {PackName} v{PackVersion} (load_order={LoadOrder})", pack.Name, pack.Version, pack.LoadOrder);

        loader.LoadPacks(packs);
        logger.LogInformation("All packs loaded");
    }

    public static string ResolveContentPath()
    {
        if (OS.HasFeature("editor"))
            return ProjectSettings.GlobalizePath("res://content");

        var exeDir = Path.GetDirectoryName(OS.GetExecutablePath())!;
        return Path.Combine(exeDir, "content");
    }
}
