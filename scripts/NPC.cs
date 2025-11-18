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

    [Export]
    public bool AutoStartOnEnter { get; set; } = true;

    private Sprite2D sprite;
    private Vector2 basePos = Vector2.Zero;
    private double bobTimer = 0.0;
    // interaction prompt/UI
    private CanvasLayer promptLayer = null;
    private Label promptLabel = null;
    private CharacterBody2D nearbyPlayer = null;
    private bool lastF6Pressed = false;

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
                if (Input.IsActionJustPressed("ui_accept"))
                {
                    GD.Print($"NPC '{DisplayName}': ui_accept pressed while nearbyPlayer present");
                    // start dialogue and hide prompt
                    HidePrompt();
                    TryStartDialogueForPlayer(nearbyPlayer);
                }
            }
            catch { }
        }

        // Debug: press F6 to teleport Player to NPC and force-start dialogue
        try
        {
            bool now = Input.IsKeyPressed(Godot.Key.F6);
            if (now && !lastF6Pressed)
            {
                GD.Print($"NPC '{DisplayName}': F6 pressed - attempting debug teleport and start");
                var player = FindPlayerNode();
                if (player != null)
                {
                    try
                    {
                        player.GlobalPosition = GlobalPosition + new Vector2(0, 16);
                        GD.Print($"NPC '{DisplayName}': teleported Player to {player.GlobalPosition}");
                    }
                    catch { }
                    TryStartDialogueForPlayer(player);
                }
                else
                {
                    GD.Print($"NPC '{DisplayName}': debug teleport failed - Player node not found");
                }
            }
            lastF6Pressed = now;
        }
        catch { }
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
            // set nearby player and show press-to-talk prompt
            nearbyPlayer = cb;
            ShowPrompt();
            // If configured, auto-start dialogue immediately (useful for debugging)
            try
            {
                if (AutoStartOnEnter)
                {
                    GD.Print($"NPC '{DisplayName}': AutoStartOnEnter is true, starting dialogue automatically");
                    HidePrompt();
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

    private CharacterBody2D FindPlayerNode()
    {
        // Try current scene first
        try
        {
            var cs = GetTree().CurrentScene;
            if (cs != null)
            {
                var p = cs.GetNodeOrNull<CharacterBody2D>("Player");
                if (p != null) return p;
            }
        }
        catch { }
        // Fallback: recursive search from root
        try
        {
            var root = GetTree().Root;
            var queue = new System.Collections.Generic.Queue<Node>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                if (n is CharacterBody2D cb && n.Name == "Player") return cb;
                foreach (Node child in n.GetChildren()) queue.Enqueue(child);
            }
        }
        catch { }
        return null;
    }

    private void ShowPrompt()
    {
        try
        {
            if (promptLayer == null)
            {
                promptLayer = new CanvasLayer();
                promptLayer.Name = "NPCPromptLayer";
                // panel to hold the label with background
                var panel = new Panel();
                panel.Name = "NPC_PromptPanel";
                // anchor to bottom center (use relative anchors)
                try
                {
                    panel.AnchorLeft = 0.35f;
                    panel.AnchorRight = 0.65f;
                    panel.AnchorTop = 0.88f;
                    panel.AnchorBottom = 0.96f;
                }
                catch { }

                var label = new Label();
                label.Name = "NPC_TalkPrompt";
                label.Text = "Press Enter to talk";
                try { label.HorizontalAlignment = HorizontalAlignment.Center; } catch { }
                try { label.AddThemeColorOverride("font_color", new Color(1,1,1)); } catch { }
                // ensure label fills the panel
                try { label.AnchorLeft = 0.0f; label.AnchorTop = 0.0f; label.AnchorRight = 1.0f; label.AnchorBottom = 1.0f; } catch { }
                panel.AddChild(label);
                promptLabel = label;
                promptLayer.AddChild(panel);
                GetTree().Root.AddChild(promptLayer);
            }
            if (promptLabel != null)
            {
                promptLabel.Visible = true;
                // ensure parent panel is visible as well
                try { var p = promptLabel.GetParent() as CanvasItem; if (p != null) p.Visible = true; } catch { }
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
