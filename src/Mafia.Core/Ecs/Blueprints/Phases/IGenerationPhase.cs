namespace Mafia.Core.Ecs.Blueprints.Phases;

public interface IGenerationPhase
{
    void Execute(WorldGenerationContext ctx);
}
