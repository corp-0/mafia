namespace Mafia.Core.WorldGen.Phases;

public interface IGenerationPhase
{
    void Execute(WorldGenerationContext ctx);
}
