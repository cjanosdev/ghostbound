using Godot;
using System.Collections.Generic;

public partial class RoundManager : Node
{
    [Export] public NodePath EnemySpawnerPath;
    [Export] public int InitialRoundTime = 20;
    [Export] public float SpawnInterval = 2.5f;
    [Export] public int PreparationTime = 8;
	[Export] public NodePath FlickerOverlayPath;
	[Export] public NodePath CameraPath;

	[Export] public PackedScene EnemySpawnEffectScene;


private ColorRect _flickerOverlay;
private Camera2D _camera;

    private EnemySpawner _enemySpawner;
    private Timer _roundTimer;
    private Timer _spawnTimer;

    private int _currentRound = 1;
	private int _currentBadProgress = 0;
	private int _currentGoodProgress = 0;
    private float _timeLeft = 0;
    private bool _roundActive = false;
	private bool _firstRound = true;


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
		if (!_firstRound)
		{
			GD.Print($"in prep phase the current round is {_currentRound} and current good progress is {_currentGoodProgress}");
			_currentRound += 1;
			_currentGoodProgress += 34;
			if (_currentGoodProgress >= 100)
			{
				// change to win screen
				GetTree().ChangeSceneToFile("res://scenes/GoodPlaceBackground.tscn");
			}
			GD.Print($"Advancing round to {_currentRound} and good progress to {_currentGoodProgress}!");
			GameManager.Instance?.AdvanceRound(_currentRound, _currentGoodProgress);
		}

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
		_firstRound = false;

        _roundActive = false;
        _spawnTimer.Stop();

		 // 🔊 Play spooky ghost cleanup sound
    	PlayGhostlyCleanupSFX();

		ScreenShake(1.0f, 5f);
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

	private async void ScreenShake(float duration = 3.0f, float intensity = 15f)
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

		// Let EnemySpawner manage which spawn point to use
		_enemySpawner.SpawnNextEnemy(_currentRound);

		PlayEnemySpawnSFX(_enemySpawner.LastSpawnPosition);
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

	public void IncrementBadProgress(int amount)
	{
		_currentBadProgress += amount;
		_currentBadProgress = Mathf.Min(_currentBadProgress, 100);

		HUD.Instance?.UpdateBadBar(_currentBadProgress);

		if (_currentBadProgress >= 100)
		{
			GD.Print("💀 Too many enemies escaped. Game over!");
			GetTree().ChangeSceneToFile("res://scenes/BadPlaceBackground.tscn");
		}
	}


	public int GetRound() => _currentRound;

	public int GetGoodBarProgress() => _currentGoodProgress;

	public int GetBadBarProgress() => _currentBadProgress;

	public bool IsRoundActive() => _roundActive;

}
