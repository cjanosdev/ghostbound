using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node2D
{
    public static GameManager Instance { get; private set; }

    private int _playerCurrency = 0;
    [Export] public NodePath AimCursorPath;

    private Dictionary<SwarmingEnemy, int> _enemyHealthMap = new Dictionary<SwarmingEnemy, int>();
    private const int MaxEnemyHealth = 50;
    private Sprite2D _aimCursor;

    public override void _Ready()
    {
        // Singleton setup
        Instance = this;

        // Set custom OS mouse cursor
        // var cursorTexture = GD.Load<Texture2D>("res://cursors/cursor_ghost.png");
        // if (cursorTexture != null)
        //     Input.SetCustomMouseCursor(cursorTexture, Input.CursorShape.Arrow);

        // Optionally hide the OS cursor (if using animated in-world cursor)
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        // Get the animated in-world cursor sprite
        if (AimCursorPath != null)
            _aimCursor = GetNodeOrNull<Sprite2D>(AimCursorPath);

        HUD.Instance?.UpdateHealth(GhostPlayer.Instance?.GetHealth() ?? 0);
        HUD.Instance?.UpdateBadBar(RoundManager.Instance?.GetBadBarProgress() ?? 0);
        HUD.Instance?.UpdateGoodBar(RoundManager.Instance?.GetGoodBarProgress() ?? 0);
        HUD.Instance?.UpdateCurrency(_playerCurrency);
        HUD.Instance?.UpdateRound(RoundManager.Instance?.GetRound() ?? 0);
    }

public override void _Input(InputEvent @event)
{
    if (@event is InputEventMouseMotion mouseEvent)
    {
        // Ensure mouse stays hidden while in game
        if (GetViewport().GetMousePosition().X < 0 || GetViewport().GetMousePosition().Y < 0)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            if (_aimCursor != null)
                _aimCursor.Visible = false;
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Hidden;
            if (_aimCursor != null)
                _aimCursor.Visible = true;
        }
    }
}

    public override void _Process(double delta)
    {
       // Only update cursor if it's valid
        if (_aimCursor != null)
            _aimCursor.GlobalPosition = GetGlobalMousePosition();
    }

    public void UnregisterEnemy(SwarmingEnemy enemy)
    {
        if (_enemyHealthMap.ContainsKey(enemy))
        {
            _enemyHealthMap.Remove(enemy);
            GD.Print($"☠️ Enemy {enemy.Name} unregistered.");
        }
    }

    // Called when the player takes damage (from an enemy, hazard, etc.)
    public void DamagePlayer(int amount)
    {
        GhostPlayer.Instance.TakeDamage(amount);
    }

    // Player death logic
    public void OnPlayerDeath()
    {
        GD.Print("Game Over! Player died.");
        CallDeferred(nameof(GoToGameOver));

    }

    public void EnemyReachedGoal(SwarmingEnemy enemy)
    {
        GD.Print("❗Enemy escaped to the Good Place. Increasing bad progress.");

        RoundManager.Instance?.IncrementBadProgress(20); // you decide how much
    }

    private void GoToGameOver()
    {
        GetTree().ChangeSceneToFile("res://scenes/BadPlaceBackground.tscn");
    }

    // Register an enemy into the health tracking system
    public void RegisterEnemy(SwarmingEnemy enemy)
    {
        if (!_enemyHealthMap.ContainsKey(enemy))
        {
            _enemyHealthMap.Add(enemy, MaxEnemyHealth);
            enemy.UpdateHealthBar(MaxEnemyHealth, MaxEnemyHealth);
            GD.Print($"✅ Enemy registered: {enemy.Name}");
        }
        else
        {
            GD.PrintErr($"⚠️ Enemy {enemy.Name} already registered!");
        }
    }

    // Deal damage to an enemy and remove them if they die
    public void DamageEnemy(SwarmingEnemy enemy, int amount)
    {
        if (!_enemyHealthMap.ContainsKey(enemy))
            return;

        _enemyHealthMap[enemy] -= amount;
        if (_enemyHealthMap[enemy] < 0)
            _enemyHealthMap[enemy] = 0;
        
        GD.Print($"Enemy damaged by {amount} with {_enemyHealthMap[enemy]} remaining");

        enemy.UpdateHealthBar(_enemyHealthMap[enemy], MaxEnemyHealth);

        if (_enemyHealthMap[enemy] == 0)
        {
            GD.Print("Enemy defeated!");

            // Emit the death signal so RoundManager knows
            enemy.EmitSignal(nameof(SwarmingEnemy.Died));
            enemy.QueueFree();
            _enemyHealthMap.Remove(enemy);

            AddCurrency(10); // Reward player for defeating an enemy
        }
    }

    // Add currency and update the HUD
    public void AddCurrency(int amount)
    {
        _playerCurrency += amount;
        HUD.Instance?.UpdateCurrency(_playerCurrency);
    }

    // Advance to the next round
    public void AdvanceRound(int _currentRound, int _currentGoodProgress)
    {
            GD.Print($"Round is {_currentRound}");
            HUD.Instance?.UpdateRound(_currentRound);

            // sruvived another round update good progress
            AdvanceGoodProgress(_currentGoodProgress);

            // Optional: Spawn more enemies, scale difficulty, etc.
            GD.Print($"Advanced to Round {_currentRound}!");
    }

    public void AdvanceGoodProgress(int _currentGoodProgress)
    {
        GD.Print($"Good progress advanced to {_currentGoodProgress}");
        HUD.Instance?.UpdateGoodBar(_currentGoodProgress);
    }

    // Manual refresh if needed
    public void UpdateHUD()
    {
        GD.Print($"In game manager update hud!");
        HUD.Instance?.UpdateHealth(GhostPlayer.Instance?.GetHealth() ?? 0);
        HUD.Instance?.UpdateCurrency(_playerCurrency);
        HUD.Instance?.UpdateRound(RoundManager.Instance?.GetRound() ?? 0);
        HUD.Instance?.UpdateGoodBar(RoundManager.Instance?.GetGoodBarProgress() ?? 0);
        HUD.Instance?.UpdateBadBar(RoundManager.Instance?.GetBadBarProgress() ?? 0);
    }
}
