using Mafia.Core.Content.Parsers;
using Mafia.Core.Content.Registries;
using Microsoft.Extensions.Logging;

namespace Mafia.Core.Content;

public sealed class ContentLoader(
    IEventDefinitionRepository eventRepository,
    IOpinionRuleRepository opinionRuleRepository,
    ContentMetadataStore metadata,
    ILogger<ContentLoader> logger)
{
    public event Action? ContentLoaded;
    
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
    /// Load all content from the specified content packs.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when two or more packs share the same ID.</exception>
    public void LoadPacks(IEnumerable<ContentPackDefinition> packs)
    {
        var sorted = packs
            .OrderBy(p => p.LoadOrder)
            .ToList();

        var duplicates = sorted
            .GroupBy(p => p.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                $"Duplicate content pack IDs found: {string.Join(", ", duplicates)}");

        foreach (ContentPackDefinition pack in sorted)
        {
            LoadMetadataFromPack(pack, "Tags", "tags.toml", metadata.Tags);
            LoadMetadataFromPack(pack, "Relations", "relations.toml", metadata.Relations);
            LoadMetadataFromPack(pack, "Stats", "stats.toml", metadata.Stats);
            LoadDirectoryContent(pack, "Events", EventTomlReader.Read, eventRepository.Register);
            LoadDirectoryContent(pack, "Opinions", OpinionRuleTomlReader.Read, opinionRuleRepository.Register);
            
            ContentLoaded?.Invoke();
        }
    }

    private static void LoadMetadataFromPack(
        ContentPackDefinition pack, string subdirectory, string filename, ContentMetadataRegistry registry)
    {
        var file = Path.Combine(pack.DirectoryPath, subdirectory, filename);
        if (!File.Exists(file)) return;

        var toml = File.ReadAllText(file);
        var entries = MetadataTomlReader.Read(toml);
        foreach (var (key, meta) in entries)
            registry.Register(key, meta);
    }

    private static void LoadDirectoryContent<T>(
        ContentPackDefinition pack, string subdirectory, Func<string, T> reader, Action<T> register)
    {
        var directory = Path.Combine(pack.DirectoryPath, subdirectory);
        if (!Directory.Exists(directory)) return;

        foreach (var file in Directory.EnumerateFiles(directory, "*.toml", SearchOption.AllDirectories))
        {
            var toml = File.ReadAllText(file);
            register(reader(toml));
        }
    }
}
