using Godot;
using System;

public partial class GhostBoundMenu : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetNode<Button>("CanvasLayer/Play").Pressed += OnPlayButtonPressed;
	}

	private void OnPlayButtonPressed()
	{
		// Replace with your actual game scene path
		GetTree().ChangeSceneToFile("res://scenes/main.tscn");
	}
}
