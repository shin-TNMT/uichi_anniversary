using Godot;
using System;

public partial class NPC : Node2D
{
    [Signal]
    public delegate void PlayerInteractedEventHandler(Node player);

    [Signal]
    public delegate void PlayerLeftEventHandler(Node player);

    [Export]
    public string DisplayName { get; set; } = "NPC";

    [Export]
    public float BobAmplitude { get; set; } = 4.0f;

    [Export]
    public float BobSpeed { get; set; } = 3.0f;

    [Export]
    public string InteractionText { get; set; } = "Hello!";

    private Sprite2D sprite;
    private Vector2 basePos = Vector2.Zero;
    private double bobTimer = 0.0;

    public override void _Ready()
    {
        GD.Print($"NPC '{DisplayName}' ready at position: {Position}");
        sprite = GetNodeOrNull<Sprite2D>("Sprite");
        if (sprite != null)
            basePos = sprite.Position;

        var area = GetNodeOrNull<Area2D>("Area");
        if (area != null)
        {
            // Connect signals in a safe way
            area.BodyEntered += OnBodyEntered;
            area.BodyExited += OnBodyExited;
        }
    }

    public override void _Process(double delta)
    {
        // idle bobbing
        bobTimer += delta * BobSpeed;
        if (sprite != null)
        {
            float offsetY = (float)(Math.Sin(bobTimer) * BobAmplitude);
            sprite.Position = basePos + new Vector2(0, offsetY);
        }
    }

    private void OnBodyEntered(Node body)
    {
        // only respond to CharacterBody2D (player) for now
        if (body is CharacterBody2D)
        {
            GD.Print($"NPC '{DisplayName}': Player entered: {body.Name}");
            // face the player
            var cb = body as CharacterBody2D;
            if (cb != null)
                FaceTowards(cb.GlobalPosition);
            // emit signal for UI or controller
            EmitSignal(nameof(PlayerInteractedEventHandler), body);
            // placeholder: log interaction text
            GD.Print($"NPC '{DisplayName}' says: {InteractionText}");
        }
    }

    private void OnBodyExited(Node body)
    {
        if (body is CharacterBody2D)
        {
            GD.Print($"NPC '{DisplayName}': Player left: {body.Name}");
            EmitSignal(nameof(PlayerLeftEventHandler), body);
        }
    }

    private void FaceTowards(Vector2 targetGlobalPos)
    {
        if (sprite == null) return;
        // flip horizontally based on relative X position
        if (targetGlobalPos.X < GlobalPosition.X)
            sprite.FlipH = true;
        else
            sprite.FlipH = false;
    }
}
