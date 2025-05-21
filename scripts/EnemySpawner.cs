using Godot;
using System.Collections.Generic;

public partial class EnemySpawner : Node2D
{
    [Export] public PackedScene EnemyScene;
    [Export] public NodePath NavigationRegionPath;
    [Export] public NodePath GoodPlaceTargetPath;
    [Export] public NodePath SpawnTileMapLayerPath;

    public Vector2 LastSpawnPosition { get; private set; }
    
    private Node2D _goodPlaceTarget;
    private NavigationRegion2D _navRegion;
    private TileMapLayer _spawnTileMapLayer;
    private List<Vector2> _spawnPoints = new();
    private int _spawnIndex = 0;


    public override void _Ready()
    {

        if (NavigationRegionPath != null)
            _navRegion = GetNode<NavigationRegion2D>(NavigationRegionPath);
        if (GoodPlaceTargetPath != null)
            _goodPlaceTarget = GetNode<Node2D>(GoodPlaceTargetPath);

         if (SpawnTileMapLayerPath != null)
            _spawnTileMapLayer = GetNode<TileMapLayer>(SpawnTileMapLayerPath);

        if (_spawnTileMapLayer == null)
        {
            GD.PrintErr("❌ Spawn TileMapLayer not found!");
            return;
        }



            // Find all cells with spawn metadata
        foreach (Vector2I cell in _spawnTileMapLayer.GetUsedCells()) // Assuming layer 0
        {
            var tileData = _spawnTileMapLayer.GetCellTileData(cell);

            if (tileData != null)
            {
                var customData = tileData.GetCustomData("spawn_type");
                 GD.Print($"🧩 Cell: {cell}, spawn_type: {customData}, VariantType: {customData.VariantType}");

                if (tileData != null && customData.VariantType == Variant.Type.String && customData.AsString() == "bad_place_spawn")
                {
                    Vector2 worldPos = _spawnTileMapLayer.MapToLocal(cell) + _spawnTileMapLayer.TileSet.TileSize / 2;
                    _spawnPoints.Add(worldPos);
                    GD.Print($"✅ Added spawn point at {worldPos}");
                }
            }
            else
            {
                GD.Print($"⚠️ No tile data for cell {cell}");
            }
        }

        GD.Print($"📍 Found {_spawnPoints.Count} enemy spawn points.");
    }

    public void SpawnNextEnemy(int round)
{
	if (_spawnPoints.Count == 0)
	{
		GD.PrintErr("⚠️ No spawn points available.");
		return;
	}

	// Wrap around if needed
	if (_spawnIndex >= _spawnPoints.Count)
		_spawnIndex = 0;

	Vector2 spawnPos = _spawnPoints[_spawnIndex];
	_spawnIndex++;

	SpawnEnemy(spawnPos, round);
}


    public RangedSwarmingEnemy SpawnEnemy(Vector2 position, int round)
    {
        if (EnemyScene == null)
        {
            GD.PrintErr("EnemyScene is not set!");
            return null;
        }

        LastSpawnPosition = position;

        RangedSwarmingEnemy enemyInstance = EnemyScene.Instantiate<RangedSwarmingEnemy>();
        enemyInstance.GlobalPosition = position;
        enemyInstance.SetGoodPlaceTarget(_goodPlaceTarget);
        enemyInstance.InitializeHealthForRound(round);


        GetTree().CurrentScene.AddChild(enemyInstance);

        return enemyInstance;
    }
}
