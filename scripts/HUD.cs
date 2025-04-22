using Godot;
using System;

public partial class HUD : CanvasLayer
{
    private Label _roundLabel;

     private Label _roundCountdownLabel;
    private Label _currencyLabel;
    private TextureProgressBar _healthBar;


    private TextureButton _ability1;
    private TextureButton _ability2;
    private TextureButton _ability3;
    private TextureButton _ability4;

    public static HUD Instance { get; private set; }

    [Signal] public delegate void AbilityPressedEventHandler(int index);

    public override void _Ready()
    {
        Instance = this;

        // 💡 Updated paths based on your scene tree
        _roundLabel = GetNodeOrNull<Label>("TopBar/RoundLabel");
        _roundCountdownLabel = GetNodeOrNull<Label>("TopBar/RoundCountdownLabel");
        _currencyLabel = GetNodeOrNull<Label>("BottomBar/CurrencyDisplay/CurrencyLabel");
        _healthBar = GetNodeOrNull<TextureProgressBar>("BottomBar/HealthBar");

        _ability1 = GetNodeOrNull<TextureButton>("BottomBar/AbilityBar/Ability1");
        _ability2 = GetNodeOrNull<TextureButton>("BottomBar/AbilityBar/Ability2");
        _ability3 = GetNodeOrNull<TextureButton>("BottomBar/AbilityBar/Ability3");
        _ability4 = GetNodeOrNull<TextureButton>("BottomBar/AbilityBar/Ability4");

        // Debug print to confirm
        GD.Print(_roundLabel == null ? "❌ RoundLabel NOT FOUND" : "✅ RoundLabel OK");
        GD.Print(_currencyLabel == null ? "❌ CurrencyLabel NOT FOUND" : "✅ CurrencyLabel OK");
        GD.Print(_healthBar == null ? "❌ HealthBar NOT FOUND" : "✅ HealthBar OK");
        GD.Print(_ability1 == null ? "❌ Ability1 NOT FOUND" : "✅ Ability1 OK");

        // Connect button signals safely
        if (_ability1 != null) _ability1.Pressed += () => EmitSignal(SignalName.AbilityPressed, 1);
        if (_ability2 != null) _ability2.Pressed += () => EmitSignal(SignalName.AbilityPressed, 2);
        if (_ability3 != null) _ability3.Pressed += () => EmitSignal(SignalName.AbilityPressed, 3);
        if (_ability4 != null) _ability4.Pressed += () => EmitSignal(SignalName.AbilityPressed, 4);
    }

    public void ShowRoundCountdown(int durationSeconds)
    {
         _roundCountdownLabel.Visible = true;
        _roundCountdownLabel.Modulate = new Color(1, 1, 1, 1); // Reset transparency
        _roundCountdownLabel.Scale = Vector2.One;

        int timeLeft = durationSeconds;
        _roundCountdownLabel.Text = $"Round starts in {timeLeft}...";

        Timer countdownTimer = new Timer();
        countdownTimer.OneShot = false;
        countdownTimer.WaitTime = 1.0f;

        countdownTimer.Timeout += () =>
        {
            timeLeft--;

            if (timeLeft > 0)
            {
                _roundCountdownLabel.Text = $"Round starts in {timeLeft}...";
                AnimateCountdownPop();
            }
            else
            {
                _roundCountdownLabel.Text = "Fight!";
                AnimateCountdownPop();
                GetTree().CreateTimer(1.0f).Timeout += () =>
                {
                    _roundCountdownLabel.Visible = false;
                };
                countdownTimer.QueueFree();
            }
        };

        AddChild(countdownTimer);
        countdownTimer.Start();
        AnimateCountdownPop(); // initial
    }

    private void AnimateCountdownPop()
    {
        var tween = CreateTween();
        tween.TweenProperty(_roundCountdownLabel, "scale", new Vector2(1.5f, 1.5f), 0.15f)
            .SetTrans(Tween.TransitionType.Elastic)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_roundCountdownLabel, "scale", Vector2.One, 0.1f)
            .SetDelay(0.15f);
    }

    public void UpdateHealth(int current)
    {
        GD.Print($"[HUD] Animate health bar from {_healthBar.Value} to {current}");
        if (_healthBar != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(_healthBar, "value", current, 0.3f)
                 .SetTrans(Tween.TransitionType.Sine)
                 .SetEase(Tween.EaseType.Out);
        }
    }

    public void UpdateCurrency(int amount)
    {
        if (_currencyLabel != null)
        {
            _currencyLabel.Text = $"${amount}";
        }
    }

    public void UpdateRound(int round)
    {
        if (_roundLabel != null)
        {
            _roundLabel.Text = $"Round {round}";
        }
    }

    public void SetAbilityEnabled(int index, bool enabled)
    {
        switch (index)
        {
            case 1: _ability1.Disabled = !enabled; break;
            case 2: _ability2.Disabled = !enabled; break;
            case 3: _ability3.Disabled = !enabled; break;
            case 4: _ability4.Disabled = !enabled; break;
        }
    }
}
