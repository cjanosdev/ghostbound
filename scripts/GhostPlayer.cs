using Godot;
using System;

public partial class GhostPlayer : CharacterBody2D
{
	public const float Speed = 300.0f;

	private AnimatedSprite2D _animatedSprite;

	public int MaxHealth = 100;
	private int _currentHealth;

	public static GhostPlayer Instance { get; private set; }

	private Timer regenDelayTimer;
    private Timer regenTickTimer;

	private Vector2 _lastDirection = Vector2.Right;
	private bool _isVomiting = false;
	private float _projectileCooldownTimer = 0f;

	[Export] public PackedScene VomitProjectileScene;
	[Export] public float ProjectileCooldown = 0.5f;


	public override void _Ready()
	{
		Instance = this;
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		_currentHealth = MaxHealth;

		// Create regen delay timer (3 sec before regen starts)
        regenDelayTimer = new Timer();
        regenDelayTimer.WaitTime = 3.0;
        regenDelayTimer.OneShot = true;
        regenDelayTimer.Timeout += StartRegen;
        AddChild(regenDelayTimer);

        // Create regen tick timer (regains health gradually)
        regenTickTimer = new Timer();
        regenTickTimer.WaitTime = 0.5;
        regenTickTimer.OneShot = false;
        regenTickTimer.Timeout += RegenHealth;
        AddChild(regenTickTimer);

		 // ✅ Delay HUD update until the scene is fully built
    	CallDeferred(nameof(UpdateHUDLater));
	}

	private void UpdateHUDLater()
{
    GameManager.Instance?.UpdateHUD();
}

	public override void _PhysicsProcess(double delta)
	{
		_projectileCooldownTimer -= (float)delta;

		if (_isVomiting)
		{
			// Prevent movement and other inputs while attacking
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 direction = Input.GetVector("left", "right", "up", "down");

		if (direction != Vector2.Zero)
		{
			_lastDirection = direction;
			Velocity = direction.Normalized() * Speed;

			// Directional movement animation
			if (direction.X < 0)
				_animatedSprite.Play("ghost_left");
			else if (direction.X > 0)
				_animatedSprite.Play("ghost_right");
			else if (direction.Y < 0)
				_animatedSprite.Play("ghost_backward");
			else if (direction.Y > 0)
				_animatedSprite.Play("ghost_forward");
		}
		else
		{
			Velocity = Vector2.Zero;
			_animatedSprite.Stop();
		}

		// Handle attack input
		if (Input.IsActionJustPressed("attack") && _projectileCooldownTimer <= 0f)
		{
			_isVomiting = true;
			ShootProjectile();
			_projectileCooldownTimer = ProjectileCooldown;

			// Unlock controls after a short time (match your vomit animation duration)
			GetTree().CreateTimer(0.4f).Timeout += () => _isVomiting = false;
		}

		MoveAndSlide();
	}

	private void PlayVomitAnimation()
	{
		 if (_lastDirection.Y < 0)
		{
			_animatedSprite.Play("vomit_backward");
		}
		else if (_lastDirection.Y > 0)
		{
			_animatedSprite.Play("vomit_forward");
		}
		else if (_lastDirection.X < 0)
		{
			_animatedSprite.FlipH = false; // just in case
			_animatedSprite.Play("vomit_left");
		}
		else if (_lastDirection.X > 0)
		{
			_animatedSprite.FlipH = false; // not flipped if you have a dedicated right anim
			_animatedSprite.Play("vomit_right");
		}
	}

	private void ShootProjectile()
	{
		if (VomitProjectileScene == null)
		{
			GD.PrintErr("VomitProjectileScene not assigned!");
			return;
		}

		// Play the vomit animation
    	PlayVomitAnimation();

		GetTree().CreateTimer(0.2f).Timeout += () =>
		{
			var projectile = VomitProjectileScene.Instantiate<VomitProjectile>();
			Vector2 direction = _lastDirection != Vector2.Zero ? _lastDirection.Normalized() : Vector2.Right;

			Vector2 spawnOffset = direction * 75f;
			projectile.Position = GlobalPosition + spawnOffset;
			projectile.SetDirection(direction);

			GetTree().CurrentScene.AddChild(projectile);
		};
	}

	public void TakeDamage(int amount)
	{
		_currentHealth -= amount;
		if (_currentHealth < 0)
			_currentHealth = 0;


		GameManager.Instance?.UpdateHUD();

		regenDelayTimer.Start();   // restart regen delay
        regenTickTimer.Stop();     // cancel any ongoing regen

		if (_currentHealth <= 0)
			Die();
	}



	private void Die()
	{
		GD.Print("Ghost is dead!");
		GameManager.Instance?.OnPlayerDeath();
	}

	 private void StartRegen()
    {
        if (_currentHealth < MaxHealth)
            regenTickTimer.Start();
    }

    private void RegenHealth()
    {
        if (_currentHealth < MaxHealth)
        {
            _currentHealth += 2;
            if (_currentHealth > MaxHealth)
                _currentHealth = MaxHealth;

            GameManager.Instance?.UpdateHUD();
        }
        else
        {
            regenTickTimer.Stop(); // stop when full
        }
    }

    public int GetHealth() => _currentHealth;
}
