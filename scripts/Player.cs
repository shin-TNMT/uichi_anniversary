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
    private float walkAnimTimer = 0.0f;
    private int walkAnimIndex = 0;
    private float walkAnimInterval = 0.2f;
    private Vector2 baseSpritePos = Vector2.Zero;
    private double bobTimer = 0.0;

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
        if (sprite != null)
        {
            baseSpritePos = sprite.Position;
        }
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
        MoveAndSlide();

        // Procedural bobbing when moving
        bool isMoving = input.Length() > 0.0f;
        if (isMoving)
        {
            bobTimer += delta * BobSpeed;
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

            // Decide front/back
            bool useBack = input.Y < 0;

            if (isMoving)
            {
                // animate walk frames
                walkAnimTimer += (float)delta;
                if (walkAnimTimer >= walkAnimInterval)
                {
                    walkAnimTimer = 0.0f;
                    walkAnimIndex = (walkAnimIndex + 1) % 2;
                }
                if (!useBack)
                {
                    sprite.Texture = (walkAnimIndex == 0 && texFrontWalk1 != null) ? texFrontWalk1 : (texFrontWalk2 != null ? texFrontWalk2 : texFrontStand);
                }
                else
                {
                    sprite.Texture = (walkAnimIndex == 0 && texBackWalk1 != null) ? texBackWalk1 : (texBackWalk2 != null ? texBackWalk2 : texBackStand);
                }
            }
            else
            {
                // standing
                walkAnimIndex = 0;
                walkAnimTimer = 0.0f;
                sprite.Texture = useBack ? (texBackStand ?? texFrontStand) : (texFrontStand ?? texBackStand);
            }

            // Flip sprite horizontally based on input direction.
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
    }
}
