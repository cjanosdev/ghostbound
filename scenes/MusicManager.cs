using Godot;

public partial class MusicManager : Node
{
    private AudioStreamPlayer2D _musicPlayer;

    public override void _Ready()
    {
		foreach (Node child in GetChildren())
		{
			GD.Print($"Child: {child.Name}");
		}
        _musicPlayer = GetNode<AudioStreamPlayer2D>("MusicPlayer");

        if (!_musicPlayer.Playing)
            _musicPlayer.Play();
    }
}
