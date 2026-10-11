using UnityEngine;

public class WorldTile
{
    public Vector2Int gridPos;

    public TerrainType terrainType;
    public BuildingType buildingType;
    public bool hasPower = false;
    public GameObject visual;
    public RoadType roadType;
    public bool isOccupied;
    public ResourceType resourceType = ResourceType.None;
    public int resourceAmount = 0;
    public float crimeLevel;
    public bool hasPoliceCoverage;

    // =============================================
    // CITY OWNERSHIP
    // -1 = unclaimed, 0+ = city index in CityManager
    // =============================================
    public int cityId = -1;

    public WorldTile(Vector2Int pos, TerrainType terrain)
    {
        gridPos = pos;
        terrainType = terrain;
        buildingType = BuildingType.None;
        roadType = RoadType.None;
    }

    public bool IsBuildable()
    {
        return terrainType != TerrainType.Water &&
               terrainType != TerrainType.Mountain;
    }

    public bool IsWalkable()
    {
        return terrainType != TerrainType.Water;
    }
}