namespace Mafia.Core.WorldGen.Phases;

public class OrgSkeletonPhase : IGenerationPhase
{
    public void Execute(WorldGenerationContext ctx)
    {
        var builder = new OrgSkeletonBuilder(ctx.Rng, ctx.Config);
        ctx.Orgs = builder.BuildAll(ctx.NameGen);

        var totalSlots = ctx.Orgs.Sum(o => o.CountSlots());
        var maxAllowed = (int)(ctx.Config.TargetPopulation * 0.7);

        if (totalSlots > maxAllowed)
            throw new InvalidOperationException(
                $"Org slots ({totalSlots}) exceed 70% of target population ({maxAllowed}). Reduce org config.");
    }
}
