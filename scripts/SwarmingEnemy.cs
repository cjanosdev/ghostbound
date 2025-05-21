using Godot;
using System;
using System.Collections.Generic;

public partial class SwarmingEnemy : CharacterBody2D
{
    [Export] public float MoveSpeed = 70f;
    [Export] public float StopDistance = 30f;
    [Export] public float EncircleRadius = 100f;
    private Node2D _goodPlaceDoor;

    protected enum EnemyState { MovingToGoal, EngagingPlayer }
    protected EnemyState CurrentState = EnemyState.MovingToGoal;

    [Export] public float EngageRadius = 150f;
    [Export] public float DisengageRadius = 200f;
    [Export] public int BaseHealth = 50;
    [Export] public int HealthPerRound = 30;

    private List<Vector2> _path = new();
    private int _pathIndex = 0;
    private int _stuckCounter = 0;
    private NavigationAgent2D _navAgent;
    private Timer _path_find_timer;
    private NavigationRegion2D _navRegion;

    private Vector2 _lastPathPosition;
private int _stuckFrameCount = 0;
private const int StuckThresholdFrames = 30; 





    protected ProgressBar _healthBar;
    protected Node2D _targetPlayer;

    private static List<SwarmingEnemy> AllSwarmers = new();
    protected int _maxHealth;
    protected int _currentHealth;
    private int _ringIndex = -1;
    private bool _isDying = false;

    private List<Vector2> _currentPath = new();
    private int _currentPathIndex = 0;



    [Signal] public delegate void DiedEventHandler();

    public void SetGoodPlaceTarget(Node2D target)
    {
        _goodPlaceDoor = target;
    }



    private void BeginPathTo(Vector2 target)
{
    if (_navAgent == null) return;

    _navAgent.TargetPosition = target;
    GD.Print($"📍 Enemy {_ringIndex} set path target to: {target}");
}


public override void _Ready()
{
    _targetPlayer = GetTree().GetFirstNodeInGroup("Player") as Node2D;
    _navAgent = GetNode<NavigationAgent2D>("NavAgent");

    if (_targetPlayer == null || _navAgent == null || _goodPlaceDoor == null)
    {
        GD.PrintErr("Missing one or more required nodes.");
        return;
    }

    _ringIndex = AllSwarmers.Count;
    AllSwarmers.Add(this);

    FindHealthBar();

    BeginPathTo(_goodPlaceDoor.GlobalPosition);
    GD.Print($"🧭 Target: {_navAgent.TargetPosition}");

}

public override void _PhysicsProcess(double delta)
{
    if (_isDying) return;
    if (!RoundManager.Instance.IsRoundActive()) return;
    if (_navAgent == null) return;

    if (_targetPlayer == null) return;

    float distanceToPlayer = GlobalPosition.DistanceTo(_targetPlayer.GlobalPosition);

    switch (CurrentState)
    {
        case EnemyState.MovingToGoal:
            if (distanceToPlayer <= EngageRadius)
            {
                GD.Print($"👁️ Enemy {_ringIndex} noticed player! Switching to Engage.");
                CurrentState = EnemyState.EngagingPlayer;
            }
            break;

            
        case EnemyState.EngagingPlayer:
            SetEncirclementTarget();

            if (distanceToPlayer > DisengageRadius){
                 GD.Print($"👻 Enemy {_ringIndex} lost interest in player. Returning to goal.");
                 CurrentState = EnemyState.MovingToGoal;
                 BeginPathTo(_goodPlaceDoor.GlobalPosition);
            }
            break;

    }

    if (_goodPlaceDoor != null && GlobalPosition.DistanceTo(_goodPlaceDoor.GlobalPosition) < 30f)
    {
        HandleReachedDoor();
        return;
    }

    FollowPathStep();
}

private void FollowPathStep()
{
    if (_navAgent.IsNavigationFinished()) return;

    Vector2 next = _navAgent.GetNextPathPosition();
    if (next == Vector2.Zero) return; // Avoid weird teleport bugs

    Vector2 dir = (next - GlobalPosition).Normalized();

    Velocity = dir * MoveSpeed;
    //GD.Print($"[Enemy {_ringIndex}] Moving toward: {next}, dist: {dir:F2}, vel: {Velocity}");

    MoveAndSlide();


    // Stuck detection
    float movementDelta = GlobalPosition.DistanceTo(_lastPathPosition);

    if (movementDelta < 0.5f) // Moved less than half a pixel this frame
    {
        _stuckFrameCount++;
        if (_stuckFrameCount >= StuckThresholdFrames)
        {
            GD.PrintErr($"🧱 Enemy {_ringIndex} appears stuck. Re-pathing...");
            ForceRepath();
            _stuckFrameCount = 0;
        }
    }
    else
    {
        _stuckFrameCount = 0;
    }

    _lastPathPosition = GlobalPosition;
}

private void ForceRepath()
{
    if (_goodPlaceDoor == null || _navAgent == null || _navRegion == null)
        return;

    var navMap = _navRegion.GetNavigationMap();

    Vector2 snappedPosition = NavigationServer2D.MapGetClosestPoint(navMap, GlobalPosition);
    Vector2 snappedTarget = NavigationServer2D.MapGetClosestPoint(navMap, _goodPlaceDoor.GlobalPosition);

    GlobalPosition = snappedPosition; // Optional: re-snap
    _navAgent.TargetPosition = snappedTarget;

    GD.Print($"🔁 Enemy {_ringIndex} recalculated path to {snappedTarget}");
}



private void HandleReachedDoor()
{
    if (_isDying) return;
    _isDying = true;

    if (RoundManager.Instance?.IsRoundActive() == true)
    {
        GD.Print($"🚪 Enemy {_ringIndex} reached the Good Place door!");
        GameManager.Instance?.EnemyReachedGoal(this);
    }
    else
    {
        GD.Print($"🕒 Enemy {_ringIndex} reached door during prep — ignoring.");
    }

    PlayCleanupEffectAndDie();
}




public override void _Draw()
{
    if (_path == null || _path.Count < 2)
        return;

    for (int i = 0; i < _path.Count - 1; i++)
    {
        Vector2 localStart = ToLocal(_path[i]);
        Vector2 localEnd = ToLocal(_path[i + 1]);

        DrawLine(localStart, localEnd, Colors.Red, 2f);
    }

    DrawCircle(ToLocal(_path[0]), 4f, Colors.Green); // Start
    DrawCircle(ToLocal(_path[^1]), 4f, Colors.Blue); // End
    DrawCircle(ToLocal(_path[_pathIndex]), 6f, Colors.Yellow); // current target

}


public void InitializeHealthForRound(int round)
{
    _maxHealth = BaseHealth + (round - 1) * HealthPerRound;
    _currentHealth = _maxHealth;
    UpdateHealthBar(_currentHealth, _maxHealth);

    GD.Print($"💀 Enemy initialized with {_currentHealth}/{_maxHealth} HP (Round {round})");
}

    public override void _ExitTree()
    {
        AllSwarmers.Remove(this);
    }

    protected virtual bool IsBusy() => false;
    protected virtual bool AllowRepositionWhileBusy() => false;

    private void SetEncirclementTarget()
{
    if (_targetPlayer == null || _navAgent == null) return;

    int total = AllSwarmers.Count;
    if (_ringIndex < 0 || total == 0) return;

    float angle = (Mathf.Tau / total) * _ringIndex;
    Vector2 ringOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * EncircleRadius;
    Vector2 ringTarget = _targetPlayer.GlobalPosition + ringOffset;

    _navAgent.TargetPosition = ringTarget;
}


private void FollowPath()
{
    if (_navAgent == null || _navAgent.IsNavigationFinished()) return;

    Vector2 nextPathPos = _navAgent.GetNextPathPosition();
    Vector2 direction = (nextPathPos - GlobalPosition).Normalized();
    Velocity = direction * MoveSpeed;
    MoveAndSlide();
}

    private void MoveToEncirclementPosition()
    {
        
        if (_targetPlayer == null || _navAgent == null) return;

        int total = AllSwarmers.Count;
        if (_ringIndex < 0 || total == 0) return;

        // Calculate this enemy's target ring position
        float angle = (Mathf.Tau / total) * _ringIndex;
        Vector2 ringOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * EncircleRadius;
        Vector2 ringTarget = _targetPlayer.GlobalPosition + ringOffset;

        _navAgent.TargetPosition = ringTarget;

        float distanceToTarget = GlobalPosition.DistanceTo(ringTarget);
        if (distanceToTarget <= StopDistance || _navAgent.IsNavigationFinished())
        {
            GD.Print($"🛑 Enemy {_ringIndex} stopping at distance: {distanceToTarget:F2}");
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        Vector2 nextPathPos = _navAgent.GetNextPathPosition();
        Vector2 direction = (nextPathPos - GlobalPosition).Normalized();

        Velocity = direction * MoveSpeed;
        MoveAndSlide();
    }

    protected void FindHealthBar()
    {
        _healthBar = GetNodeOrNull<ProgressBar>("HealthBar");
        if (_healthBar != null)
        {
            _healthBar.MaxValue = 50;
            _healthBar.Value = 50;
        }
    }

    public virtual void UpdateHealthBar(int current, int max)
    {
        if (_healthBar == null) return;

        _healthBar.MaxValue = max;
        _healthBar.Value = current;

        var tween = CreateTween();
        tween.TweenProperty(_healthBar, "value", current, 0.3f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    public virtual void TakeDamage(int amount)
{
    _currentHealth -= amount;
    _currentHealth = Mathf.Max(_currentHealth, 0);

    UpdateHealthBar(_currentHealth, _maxHealth);

    if (_currentHealth <= 0)
    {
        EmitSignal(SignalName.Died);
        QueueFree(); // Or call PlayCleanupEffectAndDie()
    }
}

   public void PlayCleanupEffectAndDie()
    {
        

        var sound = GD.Load<AudioStream>("res://sounds/wind-chimes.mp3");
        var player = new AudioStreamPlayer2D();
		player.Stream = sound;
        
        AddChild(player);
		player.Play();

		// Auto cleanup
		player.Finished += () => player.QueueFree();

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
