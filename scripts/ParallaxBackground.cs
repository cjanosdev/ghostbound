using Godot;
using System;

public partial class ParallaxBackground : Godot.ParallaxBackground
{
	[Export] public float ScrollSpeed = 30f;
	[Export] public float MaxOffsetY = 100f;
	[Export] public float MinOffsetY = 0f;
	[Export] public float MaxOffsetX = 100f;
	[Export] public float MinOffsetX = 0f;

	private bool scrollingUp = true;
	private bool scrollingRight = true;

	public override void _Process(double delta)
	{
		float offsetY = ScrollOffset.Y;
		float offsetX = ScrollOffset.X;

		// Vertical scrolling
		if (scrollingUp)
		{
			offsetY -= (float)(ScrollSpeed * delta);
			if (offsetY <= -MaxOffsetY)
				scrollingUp = false;
		}
		else
		{
			offsetY += (float)(ScrollSpeed * delta);
			if (offsetY >= MinOffsetY)
				scrollingUp = true;
		}

		// Horizontal scrolling
		if (scrollingRight)
		{
			offsetX += (float)(ScrollSpeed * delta);
			if (offsetX >= MaxOffsetX)
				scrollingRight = false;
		}
		else
		{
			offsetX -= (float)(ScrollSpeed * delta);
			if (offsetX <= -MinOffsetX)
				scrollingRight = true;
		}

		ScrollOffset = new Vector2(offsetX, offsetY);
	}
}
