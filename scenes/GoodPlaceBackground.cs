using Godot;
using System;

public partial class GoodPlaceBackground : Node2D
{
	public override void _Ready()
	{
		GetNode<Button>("CanvasLayer/Play").Pressed += OnPlayButtonPressed;
		Input.MouseMode = Input.MouseModeEnum.Visible;
    	Input.SetCustomMouseCursor(null); 
	}

	private void OnPlayButtonPressed()
	{
		// Replace with your actual game scene path
		GetTree().ChangeSceneToFile("res://scenes/main.tscn");
	}
}
