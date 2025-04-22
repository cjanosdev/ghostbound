using Godot;
using System;

public partial class GameOver : Control
{
	public override void _Ready()
	{
		GetNode<Button>("VBoxContainer/Restart").Pressed += OnRestartPressed;
		GetNode<Button>("VBoxContainer/Quit").Pressed += OnQuitPressed;
	}

	private void OnRestartPressed()
	{
		GD.Print("Restart pressed!");
		GetTree().ChangeSceneToFile("res://scenes/main.tscn"); // 👈 use your actual main scene path

	}

	private void OnQuitPressed()
	{
		GD.Print("Quit pressed!");
		GetTree().Quit();
	}
}
