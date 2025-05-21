using Godot;
using System;

public partial class VomitProjectile : Area2D
{
    [Export] public int Damage = 25;
    [Export] public float Speed = 600f;

    private Vector2 _direction;

    public override void _Ready()
    {
        GetNode<Timer>("Timer").Timeout += () => QueueFree();
        BodyEntered += OnBodyEntered;

		var vomitSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		if (vomitSprite != null)
		{
			vomitSprite.Play("vomit_animation"); 
		}
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += _direction * Speed * (float)delta;
    }

    public void SetDirection(Vector2 dir)
    {

        _direction = dir.Normalized();
        // Rotate the visual sprite only
        var sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        sprite.Rotation = _direction.Angle() - Mathf.Pi / 2;
        
    }

    private void OnBodyEntered(Node body)
    {
        if (body is RangedSwarmingEnemy enemy)
        {
            GD.Print("💚 Vomit hit enemy!");
            enemy.TakeDamage(Damage);
            QueueFree(); // Remove the projectile after hitting
        }
    }
}


