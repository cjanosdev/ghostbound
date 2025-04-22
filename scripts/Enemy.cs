// using Godot;
// using System;

// public partial class Enemy : SwarmingEnemy
// {
// 	private AnimatedSprite2D _animatedSprite;
// 	private ProgressBar _healthBar;

// 	private Vector2 _lastDirection = Vector2.Right;
// 	private bool _isFiring = false;

// 	[Signal] public delegate void DiedEventHandler();


// 	[Export] public PackedScene FireProjectileScene;

// private bool _isAttacking = false;

// 	private CpuParticles2D _particles;

// public override void _Ready()
// {
// 	base._Ready(); 
//     _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
//     _animatedSprite.Visible = false; // Start hidden

//     _particles = GetNodeOrNull<CpuParticles2D>("Particles");
//     if (_particles == null)
//     {
//         GD.PrintErr("❌ Could not find 'Particles' node");
//     }

//     AddToGroup("Enemies");
//     FindHealthBar();
//     CallDeferred(nameof(DeferredInit));
// }

// private void DeferredInit()
// {
//     GD.Print("🔁 DeferredInit running");

//     FindPlayer();
//     GameManager.Instance?.RegisterEnemy(this);

//     // Start particles now
//     if (_particles != null)
//     {
//         GD.Print("✨ Emitting particles (smoke)");
//         _particles.Emitting = false; // force restart
//         _particles.Emitting = true;
//     }

//     // Wait before revealing the sprite — smoke builds up
//     Timer timer = new Timer();
//     timer.OneShot = true;
//     timer.WaitTime = 0.3f;
//     AddChild(timer);
//     timer.Timeout += OnRevealSprite;
//     timer.Start();
// }

// private void OnRevealSprite()
// {
//     GD.Print("👻 Revealing enemy through smoke!");
//     _animatedSprite.Visible = true;
// }


// public override void _PhysicsProcess(double delta)
// {

// 	base._PhysicsProcess(delta);
// 	if (_isFiring)
// 	{
// 		// Prevent movement and other inputs while attacking
// 		Velocity = Vector2.Zero;
// 		MoveAndSlide();
// 		return;
// 	}
// 	if (_targetPlayer == null) return;
	
// 		float distanceToPlayer = GlobalPosition.DistanceTo(_targetPlayer.GlobalPosition);

// 		if (distanceToPlayer <= 900) // 🔥 attack range
// 		{
// 			//AttemptAttack();
// 		} 

// }

// public void PlayCleanupEffectAndDie()
// {
//     var tween = CreateTween();

//     tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f)
//         .SetTrans(Tween.TransitionType.Sine)
//         .SetEase(Tween.EaseType.In);

//     tween.TweenProperty(this, "scale", Vector2.Zero, 0.5f)
//         .SetTrans(Tween.TransitionType.Back)
//         .SetEase(Tween.EaseType.In);

//     tween.TweenCallback(Callable.From(() => QueueFree()));
// }


// private void FindPlayer()
// {
// 	//GD.Print("Looking for player via group...");
// 	_targetPlayer = GetTree().GetFirstNodeInGroup("Player") as Node2D;

// 	if (_targetPlayer == null)
// 		GD.PrintErr("Player not found via group!");
// 	else
// 		GD.Print("Player found at: " + _targetPlayer.GlobalPosition);
// }

// private float _attackCooldown = 1.5f; // seconds between attacks
// private float _attackTimer = 0f;

// private void AttemptAttack()
// {
// 	if (_isAttacking) return;

// 	_attackTimer -= (float)GetPhysicsProcessDeltaTime();

// 	if (_attackTimer <= 0f)
// 	{
// 		_attackTimer = _attackCooldown;
// 		_isAttacking = true;

// 		_isFiring = true;

// 		TryHitPlayer();

// 		// Reset attack state after animation duration
// 		GetTree().CreateTimer(2.0f).Timeout += () => {
// 		_isAttacking = false;
// 		_isFiring = false;
// 		_animatedSprite.Stop();
// };
// 	}
// }

// 	private void TryHitPlayer()
// 	{
// 		float hitDistance = 1000f;

// 		if (FireProjectileScene == null)
// 		{
// 			GD.PrintErr("FireProjectileScene not assigned!");
// 			return;
// 		}


// 		if (_targetPlayer.GlobalPosition.DistanceTo(GlobalPosition) <= hitDistance)
// 		{
// 			if (_targetPlayer is GhostPlayer ghost)
// 			{
// 				// Play the vomit animation
// 				PlayFireAnimation();

// 				GetTree().CreateTimer(0.2f).Timeout += () =>
// 				{
// 					var projectile = FireProjectileScene.Instantiate<FireProjectile>();
// 					Vector2 direction = _lastDirection != Vector2.Zero ? _lastDirection.Normalized() : Vector2.Right;

// 					Vector2 spawnOffset = direction * 100f;
// 					projectile.Position = GlobalPosition + spawnOffset;
// 					projectile.SetDirection(direction);

// 					GetTree().CurrentScene.AddChild(projectile);
// 				};
// 			}
// 		}
// 	}

// private void PlayFireAnimation()
// {

// 	GD.Print("Enemy attacks!");
// 	_animatedSprite.Play("staff_animation");
// }



// 	private void FindHealthBar()
// 	{
// 		_healthBar = GetNode<ProgressBar>("HealthBar");
// 		if (_healthBar != null)
// 		{
// 			_healthBar.MaxValue = 50;
// 			_healthBar.Value = 50;
// 		}
// 	}

	
// 	private void DebugDrawLineToTarget()
// 	{
// 		var line = new Line2D();
// 		line.Points = new Vector2[] { Vector2.Zero, _targetPlayer.GlobalPosition - GlobalPosition };
// 		line.Width = 2;
// 		line.DefaultColor = Colors.Red;
// 		AddChild(line);

// 		// Remove the line after a short delay
// 		GetTree().CreateTimer(0.2).Timeout += () => line.QueueFree();
// 	}

// 	public void TakeDamage(int amount)
// 	{
// 		GameManager.Instance?.DamageEnemy(this, amount);
		
// 	}


// 	public void UpdateHealthBar(int current, int max)
// 	{

// 		 if (_healthBar == null)
//         return;

//     	_healthBar.MaxValue = max;
// 		_healthBar.Value = current;
// 			// Animate the health bar value using CreateTween
// 		var tween = CreateTween(); // ✅ Godot 4 style
// 		tween.TweenProperty(_healthBar, "value", current, 0.3f)
// 			.SetTrans(Tween.TransitionType.Sine)
// 			.SetEase(Tween.EaseType.Out);

// 	}

// 	protected override bool IsBusy()
// 	{
// 		return _isFiring; // disables movement while attacking
// 	}
// }
