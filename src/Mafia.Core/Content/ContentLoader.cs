using Mafia.Core.Content.Parsers;
using Mafia.Core.Content.Registries;

namespace Mafia.Core.Content;

public sealed class ContentLoader(IEventDefinitionRepository eventRepository, IOpinionRuleRepository opinionRuleRepository)
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
            LoadEventsFromPack(pack);
            LoadOpinionRulesFromPack(pack);
            
            ContentLoaded?.Invoke();
        }
    }

    private void LoadEventsFromPack(ContentPackDefinition pack)
    {
        var eventsDirectory =  Path.Combine(pack.DirectoryPath, "Events");
        if (!Directory.Exists(eventsDirectory)) return;
        
        foreach (var file in Directory.EnumerateFiles(eventsDirectory, "*.toml", SearchOption.AllDirectories))
        {
            var toml = File.ReadAllText(file);
            var definition = EventTomlReader.Read(toml);
            eventRepository.Register(definition);
        }
    }

    private void LoadOpinionRulesFromPack(ContentPackDefinition pack)
    {
        var opinionsDirectory = Path.Combine(pack.DirectoryPath, "Opinions");
        if (!Directory.Exists(opinionsDirectory)) return;
        foreach (var file in Directory.EnumerateFiles(opinionsDirectory, "*.toml", SearchOption.AllDirectories))
        {
            var toml = File.ReadAllText(file);
            var definition = OpinionRuleTomlReader.Read(toml);
            opinionRuleRepository.Register(definition);
        }
    }
}
