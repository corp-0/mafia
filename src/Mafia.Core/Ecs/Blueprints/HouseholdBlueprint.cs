namespace Mafia.Core.Ecs.Blueprints;

public record HouseholdBlueprint
{
    public required string Id { get; init; }
    public required List<string> MemberIds { get; init; }
    public List<Marriage> Marriages { get; init; } = [];
    public List<Parentage> Parentages { get; init; } = [];
}

public record Marriage(string Spouse1Id, string Spouse2Id);

public record Parentage(string Parent1Id, string Parent2Id, List<string> ChildrenIds);
