using Godot;
using System;

public partial class FireProjectile : Area2D
{
    [Export] public int Damage = 20;
    [Export] public float Speed = 600f;

    private Vector2 _direction;

    public override void _Ready()
    {
        GD.Print($"🚀 Projectile ready at {GlobalPosition}");

        GetNode<Timer>("Timer").Timeout += () => QueueFree();
        BodyEntered += OnBodyEntered;

		var fireSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		if (fireSprite != null)
		{
			fireSprite.Play("fire_animation"); 
		}
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += _direction * Speed * (float)delta;
    }

    public void SetDirection(Vector2 dir)
    {
         _direction = dir.Normalized();
         var sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        sprite.Rotation = _direction.Angle() - Mathf.Pi / 2;
    }

    private void OnBodyEntered(Node body)
    {
        if (body is GhostPlayer player)
        {
            GD.Print("🔥 Fire hit Ghost!");
            player.TakeDamage(Damage);
            QueueFree(); // Remove the projectile after hitting
        }
    }
}
