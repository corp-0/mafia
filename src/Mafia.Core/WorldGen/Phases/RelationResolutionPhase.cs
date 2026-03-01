using Mafia.Core.Ecs.Systems;

namespace Mafia.Core.WorldGen.Phases;

public class RelationResolutionPhase : IGenerationPhase
{
    public void Execute(WorldGenerationContext ctx)
    {
        FamilyRelationDeriver.DeriveAll(ctx.Roster.Values);
    }
}
