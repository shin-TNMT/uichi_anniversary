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

    private AnimatedSprite2D sprite;
    private Vector2 baseSpritePos = Vector2.Zero;
    private double bobTimer = 0.0;

    public override void _Ready()
    {
        sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite");
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

            // Decide which animation set to use: front vs back
            string orient = "front";
            if (input.Y < 0) orient = "back"; // moving up -> back view

            if (isMoving)
            {
                try { sprite.Play(orient + "_walk"); } catch { }
            }
            else
            {
                try { sprite.Play(orient + "_stand"); } catch { }
            }

            // Flip sprite horizontally based on input direction.
            // Respect `SpriteFacesRight` so the art's base facing can be configured.
            if (input.X < 0)
            {
                // moving left -> face left
                bool faceRight = false;
                sprite.FlipH = SpriteFacesRight ? !faceRight : faceRight;
            }
            else if (input.X > 0)
            {
                // moving right -> face right
                bool faceRight = true;
                sprite.FlipH = SpriteFacesRight ? !faceRight : faceRight;
            }
        }
    }
}
