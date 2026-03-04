using Mafia.Core.Content.Parsers.Dtos;

namespace Mafia.EventEditor.Preview;

/// <summary>
/// Resolves event IDs to their TOML files and deserializes them.
/// Uses a lazy cache built on first access; call <see cref="Invalidate"/> to refresh.
/// </summary>
public class EventFileResolver
{
    private readonly string _contentRootPath;
    private Dictionary<string, string>? _cache;

    public EventFileResolver(string contentRootPath)
    {
        _contentRootPath = contentRootPath;
    }

    public bool TryResolve(string eventId, out EventDto? dto)
    {
        dto = null;
        EnsureCache();

        if (!_cache!.TryGetValue(eventId, out var filePath))
            return false;

        if (!File.Exists(filePath))
        {
            _cache.Remove(eventId);
            return false;
        }

        try
        {
            var toml = File.ReadAllText(filePath);
            dto = TomlSerializer.Deserialize(toml);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Invalidate() => _cache = null;

    private void EnsureCache()
    {
        if (_cache != null) return;

        _cache = new Dictionary<string, string>();

        if (!Directory.Exists(_contentRootPath)) return;

        foreach (var file in Directory.EnumerateFiles(_contentRootPath, "*.toml", SearchOption.AllDirectories))
        {
            // Skip non-event files like content_pack.toml
            var fileName = Path.GetFileName(file);
            if (fileName.Equals("content_pack.toml", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var toml = File.ReadAllText(file);
                var dto = TomlSerializer.Deserialize(toml);
                if (!string.IsNullOrWhiteSpace(dto.Id))
                    _cache.TryAdd(dto.Id, file);
            }
            catch
            {
                // Skip files that can't be parsed as events
            }
        }
    }
}
