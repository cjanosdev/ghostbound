using Godot;

public partial class EnemySpawner : Node2D
{
    [Export] public PackedScene EnemyScene;
    [Export] public NodePath NavigationRegionPath;

    private NavigationRegion2D _navRegion;

    public override void _Ready()
    {
        if (NavigationRegionPath != null)
            _navRegion = GetNode<NavigationRegion2D>(NavigationRegionPath);
    }

    public RangedSwarmingEnemy SpawnEnemy(Vector2 position)
    {
        if (EnemyScene == null)
        {
            GD.PrintErr("EnemyScene is not set!");
            return null;
        }

        // Clamp the position to the nearest walkable location
        if (_navRegion != null)
        {
            var navMap = _navRegion.GetNavigationMap();
            position = NavigationServer2D.MapGetClosestPoint(navMap, position);
        }

        RangedSwarmingEnemy enemyInstance = EnemyScene.Instantiate<RangedSwarmingEnemy>();
        enemyInstance.GlobalPosition = position;
        GetTree().CurrentScene.AddChild(enemyInstance);

        return enemyInstance;
    }
}
