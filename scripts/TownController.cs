using Godot;
using System;

public partial class TownController : Node2D
{
    [Export]
    public PackedScene PlayerScene { get; set; }

    public override void _Ready()
    {
        // Spawn player at PlayerSpawn if PlayerScene provided
    var spawn = GetNodeOrNull<Node2D>("PlayerSpawn");
        if (spawn == null)
        {
            GD.PrintErr("PlayerSpawn node not found in Town scene.");
            return;
        }

        if (PlayerScene == null)
        {
            // Attempt to load default path
            PlayerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
        }

        if (PlayerScene == null)
        {
            GD.PrintErr("Player scene could not be loaded. Provide PlayerScene export or ensure res://scenes/player/Player.tscn exists.");
            return;
        }

        var player = PlayerScene.Instantiate<CharacterBody2D>();
        AddChild(player);
        player.Position = spawn.Position;

        // Reparent Camera2D under player so it follows automatically
        var camera = GetNodeOrNull<Camera2D>("Camera2D");
        if (camera != null)
        {
            RemoveChild(camera);
            player.AddChild(camera);
            camera.Position = Vector2.Zero;
            camera.MakeCurrent();
        }

        GD.Print("Spawned player at: ", player.Position);
    }
}
