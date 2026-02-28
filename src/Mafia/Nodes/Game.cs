using fennecs;
using Godot;
using Mafia.Core.Ecs.Blueprints;

namespace Mafia.Nodes;

[GlobalClass]
public partial class Game : Node
{
    public override void _Ready()
    {
        var world = new World();
        var config = new WorldConfig
        {
            Seed = 42,
            TargetPopulation = 80,
            OrgCount = 2,
            MinCapos = 2,
            MaxCapos = 3,
            MinSoldiersPerCapo = 2,
            MaxSoldiersPerCapo = 3,
            MinAssociatesPerSoldier = 0,
            MaxAssociatesPerSoldier = 1,
        };
        var roster = WorldGenerator.Generate(world, config);
        WorldPrinter.Print(roster, GD.Print);
    }
}