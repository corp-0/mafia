namespace Mafia.Core.Ecs.Components.Tags;

public record struct CustomTags
{
    public HashSet<string> Tags { get; init; }

    public CustomTags()
    {
        Tags = new HashSet<string>(StringComparer.Ordinal);
    }

    public bool Contains(string tag) => Tags.Contains(tag);

    public void Add(string tag) => Tags.Add(tag);

    public bool Remove(string tag) => Tags.Remove(tag);
}
