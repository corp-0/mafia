using Mafia.Core.Content.Parsers.Dtos;
using Tomlyn;

namespace Mafia.Core.Content.Parsers;

public static class ContentPackManifestReader
{
    public const string MANIFEST_FILE_NAME = "content_pack.toml";

    public static ContentPackDefinition Read(string tomlContent, string directoryPath)
    {
        var dto = Toml.ToModel<ContentPackManifestDto>(tomlContent, options: TomlOptions.Default);

        return new ContentPackDefinition(
            Id: dto.Id,
            Name: dto.Name,
            Version: dto.Version,
            Author: dto.Author,
            Description: dto.Description,
            LoadOrder: dto.LoadOrder,
            DirectoryPath: directoryPath);
    }
}
