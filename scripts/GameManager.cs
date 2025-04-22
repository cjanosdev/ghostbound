using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node2D
{
    public static GameManager Instance { get; private set; }

    private int _playerCurrency = 0;

    private Dictionary<SwarmingEnemy, int> _enemyHealthMap = new Dictionary<SwarmingEnemy, int>();
    private const int MaxEnemyHealth = 50;

    public override void _Ready()
    {
        // Singleton setup
        Instance = this;
        HUD.Instance?.UpdateHealth(GhostPlayer.Instance?.GetHealth() ?? 0);
        HUD.Instance?.UpdateCurrency(_playerCurrency);
        HUD.Instance?.UpdateRound(RoundManager.Instance?.GetRound() ?? 0);
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

    private void GoToGameOver()
    {
        GetTree().ChangeSceneToFile("res://scenes/GameOver.tscn");
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
    public void AdvanceRound(int _currentRound)
    {
        _currentRound += 1;
        GD.Print($"Round is {_currentRound}");
        HUD.Instance?.UpdateRound(_currentRound);

        // Optional: Spawn more enemies, scale difficulty, etc.
        GD.Print($"Advanced to Round {_currentRound}!");
    }

    // Manual refresh if needed
    public void UpdateHUD()
    {
        GD.Print($"In game manager update hud!");
        HUD.Instance?.UpdateHealth(GhostPlayer.Instance?.GetHealth() ?? 0);
        HUD.Instance?.UpdateCurrency(_playerCurrency);
        HUD.Instance?.UpdateRound(RoundManager.Instance?.GetRound() ?? 0);
    }
}
