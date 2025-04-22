using Godot;
using System.Collections.Generic;

public partial class RoundManager : Node
{
    [Export] public NodePath EnemySpawnerPath;
    [Export] public int InitialRoundTime = 30;
    [Export] public float SpawnInterval = 1.5f;
    [Export] public int PreparationTime = 3;
	[Export] public NodePath FlickerOverlayPath;
	[Export] public NodePath CameraPath;

	[Export] public PackedScene EnemySpawnEffectScene;


private ColorRect _flickerOverlay;
private Camera2D _camera;

    private EnemySpawner _enemySpawner;
    private Timer _roundTimer;
    private Timer _spawnTimer;

    private int _currentRound = 0;
    private float _timeLeft = 0;
    private bool _roundActive = false;


	public static RoundManager Instance { get; private set; }

    public override void _Ready()
    {
		// Singleton setup
		Instance = this;
		GD.Print($"In round manager");
        _enemySpawner = GetNode<EnemySpawner>(EnemySpawnerPath);
		GD.Print($"Getting enemy spawner and round manager:{_enemySpawner}");

		if (FlickerOverlayPath != null)
        	_flickerOverlay = GetNode<ColorRect>(FlickerOverlayPath);

		if (CameraPath != null)
			_camera = GetNode<Camera2D>(CameraPath);

		// Setup Timers
        _roundTimer = new Timer { Name = "RoundTimer", OneShot = true };
        _spawnTimer = new Timer { Name = "SpawnTimer", OneShot = false };

        AddChild(_roundTimer);
        AddChild(_spawnTimer);

        _roundTimer.Timeout += OnRoundTimerFinished;
        _spawnTimer.Timeout += SpawnEnemy;

        StartPreparationPhase();
    }

	  private void StartPreparationPhase()
    {
        _roundActive = false;
        GameManager.Instance?.AdvanceRound(_currentRound);

        GD.Print($"🛒 Preparation phase before Round {_currentRound} begins.");
        // You could enable the Veiled Vender here
        HUD.Instance?.ShowRoundCountdown(PreparationTime); // optional

        // Start countdown to next round
        GetTree().CreateTimer(PreparationTime).Timeout += StartCombatPhase;
    }

	private void StartCombatPhase()
    {
        _roundActive = true;


        _timeLeft = InitialRoundTime + (_currentRound * 5); // increase each round
        GD.Print($"⚔️ Combat phase for {_timeLeft} seconds!");

        _roundTimer.WaitTime = _timeLeft;
        _roundTimer.Start();
		PlayNewRoundSFX();

        _spawnTimer.WaitTime = SpawnInterval;
        _spawnTimer.Start();
    }

	  private void OnRoundTimerFinished()
    {
        GD.Print($"✅ Round {_currentRound} complete!");

        _roundActive = false;
        _spawnTimer.Stop();

		 // 🔊 Play spooky ghost cleanup sound
    	PlayGhostlyCleanupSFX();

		ScreenShake(3.0f, 30f);
    	FlickerScreenEffect();

        // Optional: Clean up lingering enemies or let them stay
		 // Clean up enemies
		foreach (Node node in GetTree().GetNodesInGroup("Enemies"))
		{
				if (node is RangedSwarmingEnemy enemy)
				{
					GameManager.Instance?.UnregisterEnemy(enemy);
					enemy.PlayCleanupEffectAndDie();
				}
				else
				{
					GD.PrintErr($"⚠️ Node in 'Enemies' group is not a SwarmingEnemy: {node}");
				}
		}


        StartPreparationPhase(); // Begin next round after intermission
    }

	private async void ScreenShake(float duration = 3.0f, float intensity = 50f)
	{
		if (_camera == null)
		{
			GD.PrintErr("⚠️ Camera not found for screen shake!");
			return;
		}

		Vector2 originalOffset = _camera.Offset;
		float timePassed = 0;

		while (timePassed < duration)
		{
			float x = (float)(GD.RandRange(-1, 1) * intensity);
			float y = (float)(GD.RandRange(-1, 1) * intensity);
			_camera.Offset = new Vector2(x, y);

			await ToSignal(GetTree().CreateTimer(0.02f), "timeout");
			timePassed += 0.02f;
		}

		_camera.Offset = originalOffset;
	}

	private async void FlickerScreenEffect()
	{
		 if (_flickerOverlay == null)
		{
			GD.PrintErr("⚠️ Flicker overlay is not set!");
			return;
		}

		_flickerOverlay.Modulate = new Color(0, 0, 0, 3.0f); // fade in

		var tween = CreateTween();
		tween.TweenProperty(_flickerOverlay, "modulate:a", 0.0f, 3.0f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);

		await ToSignal(tween, "finished");
	}

	private void PlayGhostlyCleanupSFX()
	{
		var ghostSound = GD.Load<AudioStream>("res://sounds/spooky_breeze.mp3"); // replace with your actual sound path

		var player = new AudioStreamPlayer();
		player.Stream = ghostSound;
		AddChild(player); // attach to the RoundManager node
		player.Play();

		// Optional: auto-clean up the AudioStreamPlayer node after playing
		player.Finished += () => player.QueueFree();
	}

	 private void SpawnEnemy()
    {
        if (_enemySpawner == null) return;

       Vector2 spawnPosition = GetValidSpawnPosition();
		_enemySpawner.SpawnEnemy(spawnPosition);

		PlayEnemySpawnSFX(spawnPosition);
    }

	private void PlayNewRoundSFX()
	{
		var sound = GD.Load<AudioStream>("res://sounds/spooky_gong.mp3"); // Update with your file
		var player = new AudioStreamPlayer2D();
		player.Stream = sound;

		AddChild(player);
		player.Play();

		// Auto cleanup
		player.Finished += () => player.QueueFree();
	}

	private void PlayEnemySpawnSFX(Vector2 position)
	{
		var sound = GD.Load<AudioStream>("res://sounds/sinus_bomb.mp3"); // Update with your file
		var player = new AudioStreamPlayer2D();
		player.Stream = sound;
		player.GlobalPosition = position;

		AddChild(player);
		player.Play();

		// Auto cleanup
		player.Finished += () => player.QueueFree();
	}

    private Vector2 GetRandomSpawnPosition()
    {
        // Customize this with better logic/spawn zones
        return new Vector2(GD.Randf() * 800, GD.Randf() * 800);
    }

	private Vector2 GetValidSpawnPosition()
	{
		int maxTries = 10;
		float minDistance = 80f;

		for (int i = 0; i < maxTries; i++)
		{
			Vector2 tryPos = GetRandomSpawnPosition();
			bool tooClose = false;

			foreach (Node node in GetTree().GetNodesInGroup("Enemies"))
			{
				if (node is Node2D existing)
				{
					if (existing.GlobalPosition.DistanceTo(tryPos) < minDistance)
					{
						tooClose = true;
						break;
					}
				}
			}

			if (!tooClose)
				return tryPos;
		}

		// If we couldn't find a safe spot, just return a random one
		return GetRandomSpawnPosition();
	}



	public int GetRound() => _currentRound;
}
