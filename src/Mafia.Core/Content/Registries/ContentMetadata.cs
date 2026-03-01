namespace Mafia.Core.Content.Registries;

public sealed record ContentMetadata(string TitleKey, string DescriptionKey);

public sealed class ContentMetadataRegistry
{
    private readonly Dictionary<string, ContentMetadata> _entries = new(StringComparer.Ordinal);

    public void Register(string normalizedKey, ContentMetadata metadata)
    {
        _entries[normalizedKey] = metadata;
    }

    public ContentMetadata? Get(string normalizedKey) =>
        _entries.GetValueOrDefault(normalizedKey);
}

public sealed class ContentMetadataStore
{
    public ContentMetadataRegistry Tags { get; } = new();
    public ContentMetadataRegistry Stats { get; } = new();
    public ContentMetadataRegistry Relations { get; } = new();
}
