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
    public string InteractionText { get; set; } = "話す [Enter]";

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

    [Export]
    public bool AutoStartOnEnter { get; set; } = false;

    private Sprite2D sprite;
    private Vector2 basePos = Vector2.Zero;
    private double bobTimer = 0.0;
    // interaction prompt/UI (shared HUD is used; local fallback removed)
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
        GD.Print($"NPC '{DisplayName}': looking for Area node -> {(area != null ? "found" : "NOT FOUND")}");
        if (area != null)
        {
            // diagnostic: print area properties
            try { GD.Print($"Area.Monitoring={area.Monitoring} Monitorable={area.Monitorable}"); } catch { }
            try { GD.Print($"Area.CollisionLayer={area.CollisionLayer} CollisionMask={area.CollisionMask}"); } catch
            {
                try { GD.Print($"Area collision_layer={area.Get("collision_layer")} collision_mask={area.Get("collision_mask")} "); } catch { }
            }
            // Connect signals in a safe way
            area.BodyEntered += OnBodyEntered;
            area.BodyExited += OnBodyExited;
            GD.Print($"NPC '{DisplayName}': connected Area.BodyEntered/Exited signals");
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
                bool pressed = false;
                // prefer ui_accept (exists in default project settings)
                try { if (Input.IsActionJustPressed("ui_accept")) pressed = true; } catch { }
                // only check 'interact' if the action exists to avoid engine error logs
                try
                {
                    if (!pressed && InputMap.HasAction("interact") && Input.IsActionJustPressed("interact"))
                        pressed = true;
                }
                catch { }

                if (pressed)
                {
                    GD.Print($"NPC '{DisplayName}': interact pressed while nearbyPlayer present");
                    // start dialogue and hide prompt
                    HidePromptShared();
                    TryStartDialogueForPlayer(nearbyPlayer);
                }
            }
            catch { }
        }

        // No debug teleport in production build
    }

    private void OnBodyEntered(Node body)
    {
        // only respond to CharacterBody2D (player) for interaction prompt
        if (body is CharacterBody2D cb)
        {
            GD.Print($"NPC '{DisplayName}': Player entered: {body.Name}");
            FaceTowards(cb.GlobalPosition);
            // NOTE: removed automatic repositioning of the player here so the Area enter/exit
            // does not toggle repeatedly. Press-to-talk will simply show the prompt.
            // emit signal for UI or controller
            EmitSignal("PlayerInteracted", body);
            // set nearby player and show press-to-talk prompt (shared HUD)
            nearbyPlayer = cb;
            ShowPromptShared();
            // If configured, auto-start dialogue immediately (useful for debugging)
            try
            {
                if (AutoStartOnEnter)
                {
                    GD.Print($"NPC '{DisplayName}': AutoStartOnEnter is true, starting dialogue automatically");
                    HidePromptShared();
                    TryStartDialogueForPlayer(cb);
                }
            }
            catch { }
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
                    HidePromptShared();
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
            GD.Print($"NPC '{DisplayName}': DialogManager lookup in CurrentScene -> {(dm != null ? "found" : "not found")}");
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
            GD.Print($"NPC '{DisplayName}': DialogManager lookup in parents -> {(dm != null ? "found" : "not found")}");
            if (dm != null)
            {
                var method = dm.GetType().GetMethod("StartDialogue");
                GD.Print($"NPC '{DisplayName}': StartDialogue method lookup -> {(method != null ? "found" : "NOT FOUND")}");
                if (method != null)
                {
                    var pathToUse = DialoguePath;
                    if (string.IsNullOrEmpty(pathToUse) && !string.IsNullOrEmpty(NpcId))
                        pathToUse = $"res://dialogues/{NpcId}.json";
                    // Defensive check: avoid invoking StartDialogue with an empty resource path
                    if (string.IsNullOrEmpty(pathToUse))
                    {
                        GD.PrintErr($"NPC '{DisplayName}': dialogue resource path is empty; skipping StartDialogue (NpcId='{NpcId}', DialoguePath='{DialoguePath}')");
                        return;
                    }
                    GD.Print($"NPC '{DisplayName}': invoking StartDialogue with resource '{pathToUse}' and id '{NpcId}'");
                    var result = method.Invoke(dm, new object[] { pathToUse, NpcId });
                    GD.Print($"NPC '{DisplayName}': StartDialogue invoke result -> {result}");
                }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("Dialog start failed: ", e.Message);
        }
        GD.Print($"NPC '{DisplayName}' says: {InteractionText}");
    }

    private Node FindDialogManagerNode()
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
            return dm;
        }
        catch { return null; }
    }

    private void ShowPromptShared()
    {
        try
        {
            var dm = FindDialogManagerNode();
            if (dm != null)
            {
                var method = dm.GetType().GetMethod("ShowNPCPrompt");
                if (method != null)
                {
                    method.Invoke(dm, new object[] { InteractionText });
                    return;
                }
            }
        }
        catch { }
        // no local fallback: rely on DialogManager/scene to provide prompt
    }

    private void HidePromptShared()
    {
        try
        {
            var dm = FindDialogManagerNode();
            if (dm != null)
            {
                var method = dm.GetType().GetMethod("HideNPCPrompt");
                if (method != null)
                {
                    method.Invoke(dm, null);
                    return;
                }
            }
        }
        catch { }
        // no local fallback: rely on DialogManager/scene to hide prompt
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
