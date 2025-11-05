using Godot;
using System;

public partial class MapManager : Node
{
    public override void _Ready()
    {
        GD.Print("MapManager ready");
        // Spawn a demo NPC if the scene exists
        var npcScene = ResourceLoader.Load<PackedScene>("res://scenes/props/NPC.tscn");
        if (npcScene != null)
        {
            var npc = npcScene.Instantiate<Node2D>();
            if (npc != null)
            {
                AddChild(npc);
                GD.Print("Spawned NPC instance in MapManager");
            }
        }
    }

    // Placeholder for map loading/unloading and spawn control logic.
}
