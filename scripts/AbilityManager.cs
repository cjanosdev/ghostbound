using Godot;
using System;
using System.Collections.Generic;

public partial class AbilityManager : Node
{
	private const int AbilityCount = 4;
	private float[] _cooldowns = new float[AbilityCount];
	private float[] _cooldownTimers = new float[AbilityCount];

	private HUD _hud;

	public override void _Ready()
	{
		 var hud = GetNode<HUD>("../HUD");
		hud.AbilityPressed += UseAbility;
	}

	public override void _Process(double delta)
	{
		for (int i = 0; i < AbilityCount; i++)
		{
			if (_cooldownTimers[i] > 0)
			{
				_cooldownTimers[i] -= (float)delta;
				if (_cooldownTimers[i] <= 0)
				{
					GD.Print($"Ability {i + 1} is ready!");
					_hud?.SetAbilityEnabled(i + 1, true);
				}
			}
		}
	}

	public void UseAbility(int index)
	{
		if (index < 1 || index > AbilityCount)
			return;

		int i = index - 1;

		if (_cooldownTimers[i] > 0)
		{
			GD.Print($"Ability {index} is on cooldown!");
			return;
		}

		// Trigger the ability effect here
		GD.Print($"Ability {index} used!");

		// Start cooldown
		_cooldowns[i] = 5f; // You can customize this per ability
		_cooldownTimers[i] = _cooldowns[i];
		_hud?.SetAbilityEnabled(index, false);
	}
}
