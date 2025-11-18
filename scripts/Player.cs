using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [Export]
    public int Speed { get; set; } = 160;

    // Bobbing parameters (procedural animation for single-frame sprite)
    [Export]
    public float BobAmplitude { get; set; } = 4.0f;
    [Export]
    public float BobSpeed { get; set; } = 8.0f;

    // Whether the sprite image faces right by default (unflipped).
    // Set to false if your source sprite faces left, to invert flip logic.
    [Export]
    public bool SpriteFacesRight { get; set; } = true;

    private Sprite2D sprite;
    private Texture2D texFrontStand;
    private Texture2D texFrontWalk1;
    private Texture2D texFrontWalk2;
    private Texture2D texBackStand;
    private Texture2D texBackWalk1;
    private Texture2D texBackWalk2;
    // Left/right directional frames (some assets provide 3 frames: left-1..3 / right-1..3)
    private Texture2D[] texLeft = new Texture2D[3];
    private Texture2D[] texRight = new Texture2D[3];
    private float walkAnimTimer = 0.0f;
    private int walkAnimIndex = 0;
    private float walkAnimInterval = 0.2f;
    private Vector2 baseSpritePos = Vector2.Zero;
    private double bobTimer = 0.0;
    private RayCast2D obstacleRay;
    private float playerCollisionRadius = 8.0f;
    // remember last non-zero input direction so we can choose a standing frame
    private Vector2 lastInputDirection = Vector2.Zero;

    public override void _Ready()
    {
        sprite = GetNodeOrNull<Sprite2D>("Sprite");
        // load textures
        try { texFrontStand = GD.Load<Texture2D>("res://assets/characters/uichi/uichi-front-stand.png"); } catch { }
        try { texFrontWalk1 = GD.Load<Texture2D>("res://assets/characters/uichi/uichi-front-walk1.png"); } catch { }
        try { texFrontWalk2 = GD.Load<Texture2D>("res://assets/characters/uichi/uichi-front-walk2.png"); } catch { }
        try { texBackStand = GD.Load<Texture2D>("res://assets/characters/uichi/uichi-back-stand.png"); } catch { }
        try { texBackWalk1 = GD.Load<Texture2D>("res://assets/characters/uichi/uichi-back-walk1.png"); } catch { }
        try { texBackWalk2 = GD.Load<Texture2D>("res://assets/characters/uichi/uichi-back-walk2.png"); } catch { }
        // load left/right frames if present
        try { texLeft[0] = GD.Load<Texture2D>("res://assets/characters/uichi/left-1.png"); } catch { }
        try { texLeft[1] = GD.Load<Texture2D>("res://assets/characters/uichi/left-2.png"); } catch { }
        try { texLeft[2] = GD.Load<Texture2D>("res://assets/characters/uichi/left-3.png"); } catch { }
        try { texRight[0] = GD.Load<Texture2D>("res://assets/characters/uichi/right-1.png"); } catch { }
        try { texRight[1] = GD.Load<Texture2D>("res://assets/characters/uichi/right-2.png"); } catch { }
        try { texRight[2] = GD.Load<Texture2D>("res://assets/characters/uichi/right-3.png"); } catch { }
        if (sprite != null)
        {
            baseSpritePos = sprite.Position;
        }
        // detect player collision radius from CollisionShape2D if available
        try
        {
            var pCol = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
            if (pCol != null && pCol.Shape is CircleShape2D cshape)
                playerCollisionRadius = cshape.Radius;
        }
        catch { }
        // create a RayCast2D to detect obstacles ahead (e.g., NPC static bodies)
        try
        {
            obstacleRay = GetNodeOrNull<RayCast2D>("ObstacleCheck");
            if (obstacleRay == null)
            {
                obstacleRay = new RayCast2D();
                obstacleRay.Name = "ObstacleCheck";
                obstacleRay.Enabled = true;
                // Only check layer 2 (NPCs) — use Set to be safe across bindings
                try { obstacleRay.Set("collision_mask", 2); } catch { }
                AddChild(obstacleRay);
            }
        }
        catch { }
        GD.Print("Player ready at: ", GlobalPosition);
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = Vector2.Zero;
        if (Input.IsActionPressed("ui_right")) input.X += 1;
        if (Input.IsActionPressed("ui_left")) input.X -= 1;
        if (Input.IsActionPressed("ui_down")) input.Y += 1;
        if (Input.IsActionPressed("ui_up")) input.Y -= 1;

        if (input.Length() > 0)
        {
            input = input.Normalized();
        }

        Velocity = input * Speed;
        // Use shape-based query to test if moving by this frame's displacement would collide
        var displacement = Velocity * (float)delta;
        bool blocked = false;
        try
        {
            if (displacement.Length() > 0.0f)
            {
                blocked = IsMoveBlocked(displacement);
            }
        }
        catch { blocked = false; }

        if (!blocked)
            MoveAndSlide();
        else
        {
            // cancel movement when blocked
            Velocity = Vector2.Zero;
        }

        // Procedural bobbing when moving
        bool isMoving = input.Length() > 0.0f;
        if (isMoving)
        {
            bobTimer += delta * BobSpeed;
            // remember last movement direction to determine facing when stopping
            lastInputDirection = input;
        }
        else
        {
            // decay the bob timer slowly to avoid jump when restarting
            bobTimer = bobTimer * 0.9;
        }

        if (sprite != null)
        {
            float offsetY = (float)(Math.Sin(bobTimer) * BobAmplitude * (isMoving ? 1.0 : 0.0));
            sprite.Position = baseSpritePos + new Vector2(0, offsetY);

            // Decide front/back. When not moving, use lastInputDirection so stopping preserves facing.
            bool useBack = (isMoving) ? (input.Y < 0) : (lastInputDirection.Y < 0);

            if (isMoving)
            {
                // animate walk frames
                walkAnimTimer += (float)delta;
                if (walkAnimTimer >= walkAnimInterval)
                {
                    walkAnimTimer = 0.0f;
                    // advance index; maximum frames depends on which directional frames are available
                    walkAnimIndex = walkAnimIndex + 1;
                }

                // Horizontal movement has priority for left/right frames
                if (Math.Abs(input.X) > 0.0f && (texLeft[0] != null || texRight[0] != null))
                {
                    // choose side frames
                    Texture2D[] frames = input.X < 0 ? texLeft : texRight;
                    // count how many frames are available
                    int available = 0;
                    for (int i = 0; i < frames.Length; i++) if (frames[i] != null) available++;
                    if (available == 0)
                    {
                        // fallback to front/back animation
                        if (!useBack)
                        {
                            sprite.Texture = (walkAnimIndex % 2 == 0 && texFrontWalk1 != null) ? texFrontWalk1 : (texFrontWalk2 != null ? texFrontWalk2 : texFrontStand);
                        }
                        else
                        {
                            sprite.Texture = (walkAnimIndex % 2 == 0 && texBackWalk1 != null) ? texBackWalk1 : (texBackWalk2 != null ? texBackWalk2 : texBackStand);
                        }
                    }
                    else
                    {
                        // wrap index within available frames
                        int idx = (available > 0) ? (walkAnimIndex % available) : 0;
                        sprite.Texture = frames[idx] ?? texFrontStand;
                    }
                }
                else
                {
                    // vertical movement: use front/back frames
                    if (!useBack)
                    {
                        sprite.Texture = (walkAnimIndex % 2 == 0 && texFrontWalk1 != null) ? texFrontWalk1 : (texFrontWalk2 != null ? texFrontWalk2 : texFrontStand);
                    }
                    else
                    {
                        sprite.Texture = (walkAnimIndex % 2 == 0 && texBackWalk1 != null) ? texBackWalk1 : (texBackWalk2 != null ? texBackWalk2 : texBackStand);
                    }
                }
            }
            else
            {
                // standing
                walkAnimIndex = 0;
                walkAnimTimer = 0.0f;
                // prefer directional stand frames if available; otherwise fallback
                // use lastInputDirection.X to determine left/right when stopped
                if (Math.Abs(lastInputDirection.X) > 0 && (texLeft[0] != null || texRight[0] != null))
                {
                    if (lastInputDirection.X < 0 && texLeft[1] != null) sprite.Texture = texLeft[1];
                    else if (lastInputDirection.X > 0 && texRight[1] != null) sprite.Texture = texRight[1];
                    else sprite.Texture = useBack ? (texBackStand ?? texFrontStand) : (texFrontStand ?? texBackStand);
                }
                else
                {
                    sprite.Texture = useBack ? (texBackStand ?? texFrontStand) : (texFrontStand ?? texBackStand);
                }
            }

            // Flip sprite horizontally when using front/back assets only.
            bool hasHorizontalFrames = (texLeft[0] != null || texRight[0] != null);
            if (!hasHorizontalFrames)
            {
                if (input.X < 0)
                {
                    bool faceRight = false;
                    sprite.FlipH = SpriteFacesRight ? !faceRight : faceRight;
                }
                else if (input.X > 0)
                {
                    bool faceRight = true;
                    sprite.FlipH = SpriteFacesRight ? !faceRight : faceRight;
                }
            }
            else
            {
                // we are using dedicated left/right images — ensure no horizontal flip
                sprite.FlipH = false;
            }
        }
    }

    // Test whether moving by 'displacement' would overlap NPCs or other obstacles on layer 2
    private bool IsMoveBlocked(Vector2 displacement)
    {
        try
        {
            var ds = GetWorld2D().DirectSpaceState;
            var shape = new CircleShape2D();
            try { shape.Radius = playerCollisionRadius; } catch { }

            var paramsObj = new PhysicsShapeQueryParameters2D();
            try { paramsObj.Set("shape", shape); } catch { paramsObj.Shape = shape; }
            // position the test at the would-be global position
            var testPos = GlobalPosition + displacement;
            try { paramsObj.Set("transform", new Transform2D(0, testPos)); } catch { paramsObj.Transform = new Transform2D(0, testPos); }
            // check only collision layer 2 (NPCs)
            try { paramsObj.Set("collision_mask", 2); } catch { paramsObj.CollisionMask = 2; }
            // exclude self
            try { paramsObj.Set("exclude", new Godot.Collections.Array { this }); } catch { }

            var results = ds.IntersectShape(paramsObj, 1);
            if (results != null && results.Count > 0)
                return true;
        }
        catch
        {
            // fallback: not blocked
        }
        return false;
    }
}
