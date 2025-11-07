using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [Export]
    public int Speed { get; set; } = 160;

    public override void _Ready()
    {
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
    }
}
