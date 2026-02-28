using Mafia.Core.Ecs.Relations;

namespace Mafia.Core.Ecs.Blueprints.Phases;

public class RelationResolutionPhase : IGenerationPhase
{
    public void Execute(WorldGenerationContext ctx)
    {
        FamilyRelationDeriver.DeriveAll(ctx.Roster.Values);
    }
}
