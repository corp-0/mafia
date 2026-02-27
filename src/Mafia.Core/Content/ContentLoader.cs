using Mafia.Core.Content.Parsers;
using Mafia.Core.Content.Registries;

namespace Mafia.Core.Content;

public sealed class ContentLoader(IEventDefinitionRepository eventRepository)
{
    /// <summary>
    /// Scans subdirectories of <paramref name="rootPath"/> for content_pack.toml manifests.
    /// Returns all discovered packs (unsorted, unfiltered).
    /// </summary>
    public IReadOnlyList<ContentPackDefinition> DiscoverPacks(string rootPath)
    {
        var packs = new List<ContentPackDefinition>();

        foreach (var dir in Directory.EnumerateDirectories(rootPath))
        {
            var manifestPath = Path.Combine(dir, ContentPackManifestReader.MANIFEST_FILE_NAME);
            if (!File.Exists(manifestPath))
                continue;

            var toml = File.ReadAllText(manifestPath);
            var pack = ContentPackManifestReader.Read(toml, dir);
            packs.Add(pack);
        }

        return packs;
    }

    /// <summary>
    /// Resolves same-ID conflicts (keeps highest LoadOrder per ID),
    /// sorts by LoadOrder ascending, and loads all event TOMLs from each pack.
    /// </summary>
    public void LoadPacks(IEnumerable<ContentPackDefinition> packs)
    {
        var sorted = packs
            .OrderBy(p => p.LoadOrder)
            .ToList();

        foreach (var pack in sorted)
            LoadEventsFromPack(pack);
    }

    private void LoadEventsFromPack(ContentPackDefinition pack)
    {
        foreach (var file in Directory.EnumerateFiles(pack.DirectoryPath, "*.toml", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file).Equals(ContentPackManifestReader.MANIFEST_FILE_NAME, StringComparison.OrdinalIgnoreCase))
                continue;

            var toml = File.ReadAllText(file);
            var definition = EventTomlReader.Read(toml);
            eventRepository.Register(definition);
        }
    }
}
