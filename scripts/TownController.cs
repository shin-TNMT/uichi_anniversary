using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

[Tool]
public partial class TownController : Node2D
{
    [Export]
    public PackedScene PlayerScene { get; set; }

    // number of tiles to pad camera limits by (in tiles)
    [Export]
    public int CameraMarginTiles { get; set; } = 1;

    // Camera follow tuning
    [Export]
    public bool CameraSmoothingEnabled { get; set; } = true;

    [Export]
    public float CameraSmoothingSpeed { get; set; } = 6.0f;

    [Export]
    public Vector2 CameraZoom { get; set; } = new Vector2(1.0f, 1.0f);

    // Editor-only trigger to apply camera limits immediately (toggle in Inspector)
    [Export]
    public bool ApplyCameraLimitsNow { get; set; } = false;

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

        // If a Player node already exists in the scene (e.g. placed in editor), use it
        // instead of instantiating a second copy. This prevents duplicate players at runtime.
        CharacterBody2D player = null;
        var existing = GetNodeOrNull<Node2D>("Player");
        if (existing != null)
        {
            player = existing as CharacterBody2D;
            if (player == null)
            {
                GD.Print("WARN: Existing 'Player' node found but is not a CharacterBody2D. Instantiating new player.");
            }
            else
            {
                GD.Print("Found existing Player node in scene; using it instead of spawning.");
                player.Position = spawn.Position;
            }
        }

        if (player == null)
        {
            player = PlayerScene.Instantiate<CharacterBody2D>();
            AddChild(player);
            player.Position = spawn.Position;
        }

        // Auto-place 3 NPCs under NPCs node if NPC scene exists
        var npcsNode = GetNodeOrNull<Node2D>("NPCs");
        var npcScene = GD.Load<PackedScene>("res://scenes/props/NPC.tscn");
        if (npcsNode != null && npcScene != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var npc = npcScene.Instantiate<Node2D>();
                npcsNode.AddChild(npc);
                // simple spread positions relative to spawn
                npc.Position = spawn.Position + new Vector2( (i+1)*32, 0 );
            }
            GD.Print($"Placed 3 NPCs under NPCs node at around {spawn.Position}");
        }

        // Reparent Camera2D under player so it follows automatically
        Camera2D camera = null;
        // Try to find a Camera2D at root first, then search descendants
        camera = GetNodeOrNull<Camera2D>("Camera2D");
        if (camera == null)
        {
            // fallback: search children recursively
            foreach (var n in GetChildren())
            {
                if (n is Camera2D c)
                {
                    camera = c;
                    break;
                }
            }
        }

        if (camera != null)
        {
            // Only reparent if not already a child of the player
            if (camera.GetParent() != player)
            {
                // keep current transform by converting to local coordinates
                var world_pos = camera.GlobalPosition;
                RemoveChild(camera);
                player.AddChild(camera);
                camera.Position = player.ToLocal(world_pos);
            }

            // Apply follow tuning
            try
            {
                // Use generic Set to avoid binding differences across C# API versions
                camera.Set("smoothing_enabled", CameraSmoothingEnabled);
                camera.Set("smoothing_speed", CameraSmoothingSpeed);
                camera.Set("zoom", CameraZoom);
            }
            catch (Exception)
            {
                // ignore if properties are not present or Set fails
            }

            camera.MakeCurrent();
        }
        // After camera is parented to player, configure camera limits to the TileMap bounds
        if (camera != null)
        {
            try
            {
                ConfigureCameraLimits(camera);
            }
            catch (Exception e)
            {
                GD.PrintErr("Failed to configure camera limits: ", e.Message);
            }
        }
        GD.Print("Spawned player at: ", player.Position);
    }

    public override void _Process(double delta)
    {
        // Allow applying limits from the inspector while editing the scene
        if (Engine.IsEditorHint() && ApplyCameraLimitsNow)
        {
            var cam = GetNodeOrNull<Camera2D>("Camera2D");
            if (cam != null)
            {
                ConfigureCameraLimits(cam);
            }
            ApplyCameraLimitsNow = false;
        }
    }

    private void ConfigureCameraLimits(Camera2D camera)
    {
        // Collect all TileMapLayer descendants under this Town node
        var layers = new List<TileMapLayer>();

        void Collect(Node node)
        {
            foreach (var c in node.GetChildren())
            {
                if (c is TileMapLayer tml)
                    layers.Add(tml);
                if (c is Node nd)
                    Collect(nd);
            }
        }

        Collect(this);

        if (layers.Count == 0)
        {
            GD.Print("No TileMapLayer found in Town to compute camera limits.");
            return;
        }

        // Merge used rects from all layers (in cell coordinates)
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var l in layers)
        {
            var u = l.GetUsedRect();
            var p = u.Position;
            var s = u.Size;
            if (s.X == 0 || s.Y == 0) // skip empty layers
                continue;
            minX = Math.Min(minX, p.X);
            minY = Math.Min(minY, p.Y);
            maxX = Math.Max(maxX, p.X + s.X);
            maxY = Math.Max(maxY, p.Y + s.Y);
        }

        if (minX == int.MaxValue)
        {
            GD.Print("All TileMapLayer layers are empty; skipping camera limit setup.");
            return;
        }

        // Attempt to detect tile size from TileSet file first, fallback to reflection or 32
        int tileSize = 32;
        try
        {
            int detected = 0;
            foreach (var l in layers)
            {
                var ts = l.TileSet;
                if (ts == null)
                    continue;
                var resPath = ts.ResourcePath;
                if (string.IsNullOrEmpty(resPath))
                    continue;
                try
                {
                    var abs = ProjectSettings.GlobalizePath(resPath);
                    if (!File.Exists(abs))
                        continue;
                    var txt = File.ReadAllText(abs);
                    var m = Regex.Match(txt, @"tile_size\s*=\s*Vector2i\(\s*(\d+)\s*,\s*(\d+)\s*\)");
                    if (m.Success)
                    {
                        detected = int.Parse(m.Groups[1].Value);
                        break;
                    }
                    m = Regex.Match(txt, @"texture_region_size\s*=\s*Vector2i\(\s*(\d+)\s*,\s*(\d+)\s*\)");
                    if (m.Success)
                    {
                        detected = int.Parse(m.Groups[1].Value);
                        break;
                    }
                }
                catch
                {
                    // continue to next layer
                }
            }
            if (detected > 0)
                tileSize = detected;
            else
            {
                // fallback to reflection from first layer
                var prop = layers[0].GetType().GetProperty("CellSize");
                if (prop != null)
                {
                    var val = prop.GetValue(layers[0]);
                    if (val is Vector2 v)
                        tileSize = (int)Math.Abs(v.X);
                }
            }
        }
        catch
        {
            // ignore and use default
        }

        // Compute world-space rect. Use the first layer's global position as origin for layer transforms.
        var origin = layers[0].GlobalPosition;
        int padding = CameraMarginTiles * tileSize;
        var topLeft = origin + new Vector2(minX * tileSize - padding, minY * tileSize - padding);
        var bottomRight = origin + new Vector2(maxX * tileSize + padding, maxY * tileSize + padding);

        camera.LimitLeft = (int)Math.Floor(topLeft.X);
        camera.LimitTop = (int)Math.Floor(topLeft.Y);
        camera.LimitRight = (int)Math.Ceiling(bottomRight.X);
        camera.LimitBottom = (int)Math.Ceiling(bottomRight.Y);

        GD.Print($"Camera limits set to L:{camera.LimitLeft} T:{camera.LimitTop} R:{camera.LimitRight} B:{camera.LimitBottom} (tileSize={tileSize}, marginTiles={CameraMarginTiles})");
    }
}
