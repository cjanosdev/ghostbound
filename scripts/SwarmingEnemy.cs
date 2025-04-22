using Godot;
using System;
using System.Collections.Generic;

public partial class SwarmingEnemy : CharacterBody2D
{
    [Export] public float MoveSpeed = 100f;
    [Export] public float StopDistance = 30f;
    [Export] public float EncircleRadius = 100f;

    protected NavigationAgent2D _navAgent;
    protected ProgressBar _healthBar;
    protected Node2D _targetPlayer;

    private static List<SwarmingEnemy> AllSwarmers = new();
    private int _ringIndex = -1;

    [Signal] public delegate void DiedEventHandler();

    public override void _Ready()
    {
        _targetPlayer = GetTree().GetFirstNodeInGroup("Player") as Node2D;
        if (_targetPlayer == null)
        {
            GD.PrintErr("SwarmingEnemy could not find player in group 'Player'!");
            return;
        }

        _navAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");
        _navAgent.VelocityComputed += OnVelocityComputed;
        _navAgent.PathDesiredDistance = 8f;
        _navAgent.TargetDesiredDistance = StopDistance;

        _ringIndex = AllSwarmers.Count; // Lock slot
        AllSwarmers.Add(this);

        FindHealthBar();
    }

    public override void _ExitTree()
    {
        AllSwarmers.Remove(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsBusy() || AllowRepositionWhileBusy())
        {
            MoveToEncirclementPosition();
        }

       // QueueRedraw();
    }

    protected virtual bool IsBusy() => false;
    protected virtual bool AllowRepositionWhileBusy() => false;

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

        //  _navAgent.TargetPosition = _targetPlayer.GlobalPosition;

        // if (_navAgent.IsNavigationFinished()) return;

        // Vector2 nextPathPos = _navAgent.GetNextPathPosition();
        // Vector2 direction = (nextPathPos - GlobalPosition).Normalized();

        // Velocity = direction * MoveSpeed;
        // MoveAndSlide();
    }

    private void OnVelocityComputed(Vector2 safeVelocity)
    {
        Velocity = safeVelocity.Normalized() * MoveSpeed;
        MoveAndSlide();
    }

    public override void _Draw()
    {
        // if (_targetPlayer != null)
        // {
        //     Vector2 localPlayerPos = ToLocal(_targetPlayer.GlobalPosition);
        //     DrawCircle(localPlayerPos, EncircleRadius, new Color(1, 1, 1, 0.1f));

        //     int total = AllSwarmers.Count;
        //     float angle = (Mathf.Tau / Math.Max(total, 1)) * _ringIndex;
        //     Vector2 ringOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * EncircleRadius;
        //     Vector2 targetOnRing = localPlayerPos + ringOffset;

        //     DrawCircle(targetOnRing, 6f, Colors.Red); // Target point
        //     DrawLine(Vector2.Zero, targetOnRing - GlobalPosition, Colors.Red, 1.5f); // From self to target
        // }
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
}
