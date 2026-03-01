using Humanizer;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.Core.Content.Registries;
using Tomlyn;

namespace Mafia.Core.Content.Parsers;

public static class MetadataTomlReader
{
    public static Dictionary<string, MetadataEntryDto> Deserialize(string toml)
        => Toml.ToModel<Dictionary<string, MetadataEntryDto>>(toml, options: TomlOptions.Default);

    public static List<(string NormalizedKey, ContentMetadata Metadata)> Read(string toml)
    {
        var dtos = Deserialize(toml);
        var results = new List<(string, ContentMetadata)>(dtos.Count);

        foreach (var (snakeCaseKey, dto) in dtos)
        {
            var normalized = snakeCaseKey.Pascalize().ToLower();
            results.Add((normalized, new ContentMetadata(dto.TitleKey, dto.DescriptionKey)));
        }

        return results;
    }
}
