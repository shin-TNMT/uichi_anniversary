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

    // Dialogue resource path (e.g. res://dialogues/merchant.json)
    [Export]
    public string DialoguePath { get; set; } = "";

    // Unique NPC id used for seen/flag tracking
    [Export]
    public string NpcId { get; set; } = "npc_default";

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
            // Ensure player is not overlapping the NPC by pushing them outside the collision extents
            try
            {
                // get player collision radius (if CircleShape2D)
                float playerRadius = 8.0f;
                var pCol = cb.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
                if (pCol != null)
                {
                    if (pCol.Shape is CircleShape2D c)
                        playerRadius = c.Radius;
                }
                // get NPC body collision approximate radius (use max extent)
                float npcRadius = 16.0f;
                var bodyCol = GetNodeOrNull<CollisionShape2D>("Body/BodyCollision");
                if (bodyCol != null)
                {
                    if (bodyCol.Shape is RectangleShape2D r)
                    {
                        npcRadius = Math.Max(r.Extents.X, r.Extents.Y);
                    }
                }
                // compute direction and reposition player just outside NPC
                var dir = (cb.GlobalPosition - GlobalPosition);
                if (dir.Length() == 0) dir = new Vector2(0, -1);
                dir = dir.Normalized();
                var desired = GlobalPosition + dir * (npcRadius + playerRadius + 0.5f);
                try { cb.GlobalPosition = desired; } catch { }
                try { cb.Velocity = Vector2.Zero; } catch { }
            }
            catch (Exception e)
            {
                GD.PrintErr("NPC: error while correcting player overlap: ", e.Message);
            }
            // emit signal for UI or controller
            EmitSignal("PlayerInteracted", body);
            // Attempt to start dialogue via DialogManager instance if available
            try
            {
                Node dm = null;
                // Prefer current scene's DialogManager
                if (GetTree().CurrentScene != null)
                {
                    dm = GetTree().CurrentScene.GetNodeOrNull("DialogManager");
                }
                // Fallback: search upward in parents for a DialogManager node
                if (dm == null)
                {
                    Node cursor = this;
                    while (cursor != null)
                    {
                        dm = cursor.GetNodeOrNull("DialogManager");
                        if (dm != null)
                            break;
                        cursor = cursor.GetParent() as Node;
                    }
                }

                if (dm != null)
                {
                    // call StartDialogue on DialogManager if present
                    var method = dm.GetType().GetMethod("StartDialogue");
                    if (method != null)
                    {
                        // If DialoguePath not set on instance, try a convention-based path using NpcId
                        var pathToUse = DialoguePath;
                        if (string.IsNullOrEmpty(pathToUse) && !string.IsNullOrEmpty(NpcId))
                        {
                            pathToUse = $"res://dialogues/{NpcId}.json";
                        }
                        GD.Print($"NPC '{DisplayName}': starting dialogue with resource '{pathToUse}' and id '{NpcId}'");
                        method.Invoke(dm, new object[] { pathToUse, NpcId });
                    }
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("Dialog start failed: ", e.Message);
            }
            // placeholder: log interaction text
            GD.Print($"NPC '{DisplayName}' says: {InteractionText}");
        }
    }

    private void OnBodyExited(Node body)
    {
        if (body is CharacterBody2D)
        {
            GD.Print($"NPC '{DisplayName}': Player left: {body.Name}");
            EmitSignal("PlayerLeft", body);
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
