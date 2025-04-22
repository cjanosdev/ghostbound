using Godot;
using System;

public partial class RangedSwarmingEnemy : SwarmingEnemy
{
    [Export] public PackedScene FireProjectileScene;
    [Export] public float AttackRange = 900f;
    [Export] public float AttackCooldown = 1.5f;
    [Export] public float RetreatDistance = 600f;
    [Export] public float ProjectileSpawnOffset = 50f;

    private AnimatedSprite2D _animatedSprite;
    private CpuParticles2D _particles;

    private bool _isFiring = false;
    private bool _isAttacking = false;
    private float _attackTimer = 0f;
    private Vector2 _lastDirection = Vector2.Right;

    public override void _Ready()
    {
        base._Ready();

        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _animatedSprite.Visible = false;

        _particles = GetNodeOrNull<CpuParticles2D>("Particles");

        AddToGroup("Enemies");

        CallDeferred(nameof(DeferredInit));
    }

    private void DeferredInit()
    {
        GD.Print("🔁 DeferredInit running");

        GameManager.Instance?.RegisterEnemy(this);

        if (_particles != null)
        {
            GD.Print("✨ Emitting particles (smoke)");
            _particles.Emitting = false; // Restart particles
            _particles.Emitting = true;
        }

        // Delay reveal for particle buildup
        Timer revealTimer = new Timer { OneShot = true, WaitTime = 0.3f };
        AddChild(revealTimer);
        revealTimer.Timeout += OnRevealSprite;
        revealTimer.Start();
    }

    private void OnRevealSprite()
    {
        GD.Print("👻 Revealing enemy through smoke!");
        _animatedSprite.Visible = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        _attackTimer -= (float)delta;

        if (_targetPlayer == null || _isAttacking) return;

        float distance = GlobalPosition.DistanceTo(_targetPlayer.GlobalPosition);

        if (distance <= AttackRange && _attackTimer <= 0f)
        {
            AttemptAttack();
        }
    }

    private void AttemptAttack()
    {
        _attackTimer = AttackCooldown;
        _isFiring = true;
        _isAttacking = true;

        PlayFireAnimation();

        GetTree().CreateTimer(0.2f).Timeout += () =>
        {
            if (FireProjectileScene == null)
            {
                GD.PrintErr("❌ FireProjectileScene not assigned!");
                return;
            }

            var projectile = FireProjectileScene.Instantiate<FireProjectile>();
            Vector2 direction = _lastDirection != Vector2.Zero ? _lastDirection.Normalized() : Vector2.Right;
            projectile.Position = GlobalPosition + direction * ProjectileSpawnOffset;
            projectile.SetDirection(direction);

            GetTree().CurrentScene.AddChild(projectile);
        };

        GetTree().CreateTimer(1.0f).Timeout += () =>
        {
            _isFiring = false;
            _isAttacking = false;
            _animatedSprite.Stop();
        };
    }

    private void PlayFireAnimation()
    {
        GD.Print("Enemy attacks!");
        _animatedSprite.Play("staff_animation");
    }

    protected override bool IsBusy() => _isFiring;
    protected override bool AllowRepositionWhileBusy() => true;

    public void TakeDamage(int amount)
    {
        GameManager.Instance?.DamageEnemy(this, amount);
        FlashDamageEffect();
    }

    private void PlayHitSound()
    {
        var hitSFX = GD.Load<AudioStream>("res://sounds/hit.wav");
        var player = new AudioStreamPlayer();
        player.Stream = hitSFX;
        AddChild(player);
        player.Play();
        player.Finished += () => player.QueueFree();
    }

    private void FlashDamageEffect()
    {
        if (_animatedSprite == null) return;

        var tween = CreateTween();
        tween.TweenProperty(_animatedSprite, "modulate", new Color(1, 1, 1, 1), 0.0); // flash white instantly
        tween.TweenProperty(_animatedSprite, "modulate", new Color(1, 1, 1, 0.6f), 0.1); // fade slightly
        tween.TweenProperty(_animatedSprite, "modulate", new Color(1, 1, 1, 1), 0.1); // restore
    }

    public void PlayCleanupEffectAndDie()
    {
        var tween = CreateTween();

        tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);

        tween.TweenProperty(this, "scale", Vector2.Zero, 0.5f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);

        tween.TweenCallback(Callable.From(() => QueueFree()));
    }
}
