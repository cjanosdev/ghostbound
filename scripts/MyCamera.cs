using Godot;
using System;

public partial class MyCamera : Camera2D
{
	[Export]
	Node2D trackingObj;

	 [Export]
    public Vector2 ZoomAmount = new Vector2(1.5f, 1.5f); // Zoom out to see more of the map
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (trackingObj == null)
		{
			GD.PrintErr("⚠️ MyCamera: trackingObj not assigned!");
		}

		 // Set camera zoom
        Zoom = ZoomAmount;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// move the camera to follow the player
		if (trackingObj != null) 
		{
			Position = Position.Lerp(trackingObj.Position, 5f * (float)delta);
		}
	}
}
