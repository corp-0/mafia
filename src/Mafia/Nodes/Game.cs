using Godot;

namespace Mafia.Nodes;

[GlobalClass]
public partial class Game : Node
{
    public override void _Ready()
    {
        Console.WriteLine("hello world");
    }
}