namespace Mafia.Core.Ecs.Blueprints;

public sealed record WorldConfig
{
    public int TargetPopulation { get; init; } = 500;
    public double MaleRatio { get; init; } = 0.52;
    public double MarriageRate { get; init; } = 0.7;
    public double ChildrenLambda { get; init; } = 2.5;

    // Age constraints
    public int MinAdultAge { get; init; } = 18;
    public int MaxAge { get; init; } = 80;
    public int MinMarriageAge { get; init; } = 20;
    public int MaxSpouseAgeDiff { get; init; } = 12;
    public int MinParentChildAgeDiff { get; init; } = 16;

    // Org structure
    public int OrgCount { get; init; } = 5;
    public int MinCapos { get; init; } = 3;
    public int MaxCapos { get; init; } = 6;
    public int MinSoldiersPerCapo { get; init; } = 3;
    public int MaxSoldiersPerCapo { get; init; } = 8;
    public int MinAssociatesPerSoldier { get; init; } = 0;
    public int MaxAssociatesPerSoldier { get; init; } = 3;
    public double AssociateSharesSurnameProbability { get; init; } = 0.3;

    // Nepotism
    public double SonJoinsFatherOrgProbability { get; init; } = 0.4;
    public double RankInheritanceBoost { get; init; } = 0.3;

    // Cross-family
    public double CrossOrgMarriageProbability { get; init; } = 0.05;

    public int Seed { get; init; } = 42;
}
