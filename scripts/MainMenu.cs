using Godot;
using System;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		GetNode<Button>("VBoxContainer/Start").Pressed += OnStartButtonPressed;
		GetNode<Button>("VBoxContainer/Quit").Pressed += OnQuitButtonPressed;
	}

	private void OnStartButtonPressed()
	{
		// Replace with your actual game scene path
		GetTree().ChangeSceneToFile("res://scenes/main.tscn");
	}

	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}
}
