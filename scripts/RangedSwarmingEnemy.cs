using Godot;
using System;

public partial class RangedSwarmingEnemy : SwarmingEnemy
{
    [Export] public PackedScene FireProjectileScene;
    [Export] public float AttackRange = 200f;
    [Export] public float AttackCooldown = 1.5f;
    [Export] public float RetreatDistance = 250f;
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
        GD.Print($"Global position of enemy is {GlobalPosition}");
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
        PlayDamageSound();
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

            GD.Print($"📦 Spawning projectile at {projectile.Position} with dir {direction}");

            GetTree().CurrentScene.AddChild(projectile);
            GD.Print($"🎯 Projectile added to scene: {projectile.Name}");

            // Auto-clean the projectile after 3 seconds
            GetTree().CreateTimer(1.0).Timeout += () =>
            {
                if (IsInstanceValid(projectile))
                    projectile.QueueFree();
            };
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
        if (_targetPlayer == null)
        return;

    Vector2 toPlayer = _targetPlayer.GlobalPosition - GlobalPosition;
    _lastDirection = toPlayer.Normalized();

    string anim = toPlayer.X < 0 ? "attack_left" : "attack_right";
    // Optional: Confirm animation exists in the sprite's list
    if (!_animatedSprite.SpriteFrames.HasAnimation(anim))
    {
        GD.PrintErr($"❌ Animation '{anim}' not found in AnimatedSprite2D!");
        return;
    }

    // Ensure sprite is visible
    if (!_animatedSprite.Visible)
    {
        GD.Print("⚠️ AnimatedSprite2D was hidden during attack. Making it visible.");
        _animatedSprite.Visible = true;
    }

    // Play the animation
    _animatedSprite.Play(anim);
    _animatedSprite.Frame = 0;

    GD.Print($"🧙‍♂️ Playing fire animation: {anim} | Direction: {_lastDirection}");
    }

    protected override bool IsBusy() => _isFiring;
    protected override bool AllowRepositionWhileBusy() => true;

    public override void TakeDamage(int amount)
    {
        GameManager.Instance?.DamageEnemy(this, amount);
        base.TakeDamage(amount);            // Handles health + death
        FlashDamageEffect();
        // PlayHitSound();
    }

    private void PlayDamageSound()
    {
        var hitSFX = GD.Load<AudioStream>("res://sounds/lazer.mp3");
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

}
