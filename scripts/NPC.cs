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

    [Export]
    public float CollisionScale { get; set; } = 1.2f;

    [Export]
    public Vector2 MinCollisionExtents { get; set; } = new Vector2(8, 8);

    private Sprite2D sprite;
    private Vector2 basePos = Vector2.Zero;
    private double bobTimer = 0.0;
    // interaction prompt/UI
    private CanvasLayer promptLayer = null;
    private Label promptLabel = null;
    private CharacterBody2D nearbyPlayer = null;

    public override void _Ready()
    {
        GD.Print($"NPC '{DisplayName}' ready at position: {Position}");
        sprite = GetNodeOrNull<Sprite2D>("Sprite");
        if (sprite != null)
            basePos = sprite.Position;

        // Adjust collision shapes to match sprite texture size when possible
        try
        {
            var bodyCol = GetNodeOrNull<CollisionShape2D>("Body/BodyCollision");
            var areaCol = GetNodeOrNull<CollisionShape2D>("Area/Collision");
            if (sprite != null && sprite.Texture != null)
            {
                Vector2 texSize = new Vector2();
                try
                {
                    // prefer GetSize() if available
                    texSize = sprite.Texture.GetSize();
                }
                catch
                {
                    // fallback to property access
                    try { texSize = (Vector2)sprite.Texture.Get("size"); } catch { }
                }
                // consider sprite scale
                try { texSize = texSize * sprite.Scale; } catch { }
                if (texSize == Vector2.Zero)
                {
                    // fallback to a reasonable default
                    texSize = new Vector2(32, 32);
                }

                // extents = half-size, apply scale and minimum
                var extents = (texSize / 2.0f) * CollisionScale;
                if (extents.X < MinCollisionExtents.X) extents.X = MinCollisionExtents.X;
                if (extents.Y < MinCollisionExtents.Y) extents.Y = MinCollisionExtents.Y;

                if (bodyCol != null && bodyCol.Shape != null)
                {
                    try { bodyCol.Shape.Set("extents", extents); } catch { }
                }
                if (areaCol != null && areaCol.Shape != null)
                {
                    try { areaCol.Shape.Set("extents", extents); } catch { }
                }
                // align collision shape positions with sprite offset if present
                try { if (bodyCol != null) bodyCol.Position = sprite.Position; } catch { }
                try { if (areaCol != null) areaCol.Position = sprite.Position; } catch { }
                GD.Print($"NPC: set collision extents to {extents}");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("NPC: failed to adjust collision extents: ", e.Message);
        }

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
        // if player is nearby, listen for accept to start dialogue
        if (nearbyPlayer != null)
        {
            try
            {
                if (Input.IsActionJustPressed("ui_accept"))
                {
                    // start dialogue and hide prompt
                    HidePrompt();
                    TryStartDialogueForPlayer(nearbyPlayer);
                }
            }
            catch { }
        }
    }

    private void OnBodyEntered(Node body)
    {
        // only respond to CharacterBody2D (player) for interaction prompt
        if (body is CharacterBody2D cb)
        {
            GD.Print($"NPC '{DisplayName}': Player entered: {body.Name}");
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
                    try
                    {
                        var shape = bodyCol.Shape;
                        if (shape != null)
                        {
                            Vector2 ext = new Vector2();
                            try { ext = (Vector2)shape.Get("extents"); } catch { }
                            if (ext == Vector2.Zero)
                            {
                                try { ext = (Vector2)shape.Get("size"); } catch { }
                            }
                            if (ext != Vector2.Zero)
                                npcRadius = Math.Max(ext.X, ext.Y);
                        }
                    }
                    catch { }
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
            // set nearby player and show press-to-talk prompt (do not auto-start dialogue)
            nearbyPlayer = cb;
            ShowPrompt();
        }
    }

    private void OnBodyExited(Node body)
    {
        if (body is CharacterBody2D cb)
        {
            GD.Print($"NPC '{DisplayName}': Player left: {body.Name}");
            if (nearbyPlayer == cb)
            {
                nearbyPlayer = null;
                HidePrompt();
            }
            EmitSignal("PlayerLeft", body);
        }
    }

    private void TryStartDialogueForPlayer(CharacterBody2D player)
    {
        try
        {
            Node dm = null;
            if (GetTree().CurrentScene != null)
                dm = GetTree().CurrentScene.GetNodeOrNull("DialogManager");
            if (dm == null)
            {
                Node cursor = this;
                while (cursor != null)
                {
                    dm = cursor.GetNodeOrNull("DialogManager");
                    if (dm != null) break;
                    cursor = cursor.GetParent() as Node;
                }
            }
            if (dm != null)
            {
                var method = dm.GetType().GetMethod("StartDialogue");
                if (method != null)
                {
                    var pathToUse = DialoguePath;
                    if (string.IsNullOrEmpty(pathToUse) && !string.IsNullOrEmpty(NpcId))
                        pathToUse = $"res://dialogues/{NpcId}.json";
                    GD.Print($"NPC '{DisplayName}': starting dialogue with resource '{pathToUse}' and id '{NpcId}'");
                    method.Invoke(dm, new object[] { pathToUse, NpcId });
                }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("Dialog start failed: ", e.Message);
        }
        GD.Print($"NPC '{DisplayName}' says: {InteractionText}");
    }

    private void ShowPrompt()
    {
        try
        {
            if (promptLayer == null)
            {
                promptLayer = new CanvasLayer();
                promptLayer.Name = "NPCPromptLayer";
                var label = new Label();
                label.Name = "NPC_TalkPrompt";
                label.Text = "Press Enter to talk";
                try { label.AddThemeColorOverride("font_color", new Color(1,1,1)); } catch { }
                // basic placement: rely on default position; scene can override via theme/layout
                promptLabel = label;
                promptLayer.AddChild(promptLabel);
                GetTree().Root.AddChild(promptLayer);
            }
            if (promptLabel != null)
            {
                promptLabel.Visible = true;
            }
        }
        catch { }
    }

    private void HidePrompt()
    {
        try
        {
            if (promptLabel != null) promptLabel.Visible = false;
        }
        catch { }
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
