using Godot;
using System;

public partial class NPC : Node2D
{
    public override void _Ready()
    {
        GD.Print("NPC ready at position: ", this.Position);
        var area = GetNodeOrNull<Area2D>("Area");
        if (area != null)
        {
            area.BodyEntered += OnBodyEntered;
        }
    }

    private void OnBodyEntered(Node body)
    {
        GD.Print("NPC: Player or body entered: ", body.Name);
        // Simple placeholder interaction: print and emit a signal
        // In future, hook up dialog UI here.
    }
}
