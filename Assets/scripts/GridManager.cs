using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public int width = 20;
    public int height = 20;
    public float tileSize = 5f;

    private float seedX;
    private float seedZ;
    public static GridManager Instance;
    public GameObject tilePrefab;

    public int seed = 12345;
    public List<TileObject> districtTiles = new List<TileObject>();

    // ─────────────────────────────────────────
    // TILE CACHE
    // Built once during GenerateGrid().
    // All managers read this instead of calling
    // FindObjectsOfType<TileObject>() every tick.
    // Call RegisterTile / UnregisterTile when
    // buildings are placed or demolished.
    // ─────────────────────────────────────────
    public List<TileObject> allTiles = new List<TileObject>();

    public WorldTile[,] grid;
    public GameObject roadStraightPrefab;
    public GameObject roadCornerPrefab;
    public GameObject roadTPrefab;
    public GameObject roadCrossPrefab;
    public GameObject roadEndPrefab;

    [Header("Decorations")]
    public GameObject[] mountainPrefabs;
    public GameObject[] sandProps;
    public GameObject[] forestPrefabs;
    public GameObject[] grassPrefabs;

    [Header("Citizens")]
    public GameObject citizenPrefab;
    int activeCitizens = 0;
    public int citizenCount = 20;

    [Header("Traffic")]
    public GameObject carPrefab;
    int activeCars = 0;
    public int carCount = 10;

    [Header("Resource Indicators")]
    public GameObject woodResourcePrefab;
    public GameObject oilResourcePrefab;
    public GameObject farmingResourcePrefab;
    public GameObject mineralsResourcePrefab;
    public GameObject ironResourcePrefab;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        seedX = Random.Range(0f, 10000f);
        seedZ = Random.Range(0f, 10000f);
        GenerateGrid();
    }

    void GenerateGrid()
    {
        grid = new WorldTile[width, height];
        allTiles.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                TerrainType type = GetTerrainType(x, z);
                WorldTile tile = new WorldTile(pos, type);
                AssignResourceToTile(tile);
                GenerateTileResource(tile);

                Vector3 worldPos = new Vector3(
                    x * tileSize - (width * tileSize) / 2f + tileSize / 2f,
                    0,
                    z * tileSize - (height * tileSize) / 2f + tileSize / 2f
                );

                GameObject tileGO = Instantiate(
                    tilePrefab, worldPos, Quaternion.identity, transform);

                TileObject tileObj = tileGO.GetComponent<TileObject>();
                tileObj.SetTile(tile, this);
                tileObj.ApplyTerrainColor();
                tileObj.SpawnDecoration();
                tileObj.SpawnResourceVisual();
                tile.visual = tileGO;
                grid[x, z] = tile;

                // Add to cache
                allTiles.Add(tileObj);
            }
        }

        // All visuals exist — re-stamp any city borders
        // that were registered before grid generation finished.
        if (CityManager.Instance != null)
        {
            foreach (var city in CityManager.Instance.cities)
                CityManager.Instance.RefreshCityBorderTiles(city);
        }
    }

    // ─────────────────────────────────────────
    // TILE CACHE REGISTRATION
    // TileObject calls these on build/demolish
    // so the cache stays accurate without ever
    // needing FindObjectsOfType again.
    // ─────────────────────────────────────────
    public void RegisterTile(TileObject tile)
    {
        if (!allTiles.Contains(tile))
            allTiles.Add(tile);
    }

    public void UnregisterTile(TileObject tile)
    {
        allTiles.Remove(tile);
    }

    // ─────────────────────────────────────────

    TerrainType GetTerrainType(int x, int z)
    {
        float scale = 0.1f;
        float noise = Mathf.PerlinNoise(
            (x + seedX) * scale, (z + seedZ) * scale);

        if (noise < 0.3f) return TerrainType.Water;
        else if (noise < 0.4f) return TerrainType.Sand;
        else if (noise < 0.7f) return TerrainType.Clear;
        else if (noise < 0.85f) return TerrainType.Forest;
        else return TerrainType.Mountain;
    }

    public TileObject GetTileAt(int x, int z)
    {
        if (x < 0 || x >= width || z < 0 || z >= height) return null;
        return grid[x, z].visual.GetComponent<TileObject>();
    }

    bool trafficStarted = false;

    public void RegisterDistrict(TileObject tile)
    {
        if (!districtTiles.Contains(tile))
            districtTiles.Add(tile);

        if (!trafficStarted && districtTiles.Count >= 2)
        {
            StartCoroutine(TrafficLoop());
            trafficStarted = true;
        }
    }

    public TileObject FindConnectedDistrict(TileObject start)
    {
        foreach (var other in districtTiles)
        {
            if (other == start) continue;
            if (IsConnectedByRoad(start, other))
                return other;
        }
        return null;
    }

    public bool IsConnectedByRoad(TileObject a, TileObject b)
    {
        if (a == null || b == null) return false;

        HashSet<TileObject> visited = new HashSet<TileObject>();
        Queue<TileObject> queue = new Queue<TileObject>();

        queue.Enqueue(a);
        visited.Add(a);

        while (queue.Count > 0)
        {
            TileObject current = queue.Dequeue();
            if (current == b) return true;

            foreach (var n in GetNeighbors(current))
            {
                if (visited.Contains(n)) continue;
                if (n.HasRoad() || n == b)
                {
                    visited.Add(n);
                    queue.Enqueue(n);
                }
            }
        }

        return false;
    }

    // =============================================
    // GET ROAD PATH
    // Same BFS as IsConnectedByRoad but returns
    // the actual tile path so visuals can be drawn.
    // Used by RouteVisualizer. Returns null if no
    // road connection exists.
    // =============================================
    public List<TileObject> GetRoadPath(
        TileObject a, TileObject b)
    {
        if (a == null || b == null) return null;

        Dictionary<TileObject, TileObject> cameFrom =
            new Dictionary<TileObject, TileObject>();

        Queue<TileObject> queue = new Queue<TileObject>();

        queue.Enqueue(a);
        cameFrom[a] = null;

        TileObject found = null;

        while (queue.Count > 0)
        {
            TileObject current = queue.Dequeue();

            if (current == b)
            {
                found = current;
                break;
            }

            foreach (var n in GetNeighbors(current))
            {
                if (n == null) continue;
                if (cameFrom.ContainsKey(n)) continue;
                if (n.HasRoad() || n == b)
                {
                    cameFrom[n] = current;
                    queue.Enqueue(n);
                }
            }
        }

        if (found == null) return null;

        // Reconstruct path
        List<TileObject> path = new List<TileObject>();
        TileObject step = found;

        while (step != null)
        {
            path.Add(step);
            cameFrom.TryGetValue(step, out step);
        }

        path.Reverse();
        return path;
    }

    List<TileObject> GetNeighbors(TileObject tile)
    {
        return new List<TileObject>()
        {
            GetTileAt(tile.gridPosition.x + 1, tile.gridPosition.y),
            GetTileAt(tile.gridPosition.x - 1, tile.gridPosition.y),
            GetTileAt(tile.gridPosition.x, tile.gridPosition.y + 1),
            GetTileAt(tile.gridPosition.x, tile.gridPosition.y - 1)
        };
    }

    void SpawnCar(TileObject origin)
    {
        if (activeCars >= carCount) return;
        TileObject startTile = GetRandomRoadTile();
        TileObject destination = GetRandomDistrict();
        if (startTile == null || destination == null) return;

        GameObject car = Instantiate(carPrefab);
        activeCars++;
        var agent = car.GetComponent<CarAgent>();
        agent.InitPath(startTile, destination, this);
        StartCoroutine(TrackCar(car));
    }

    void SpawnCitizen(TileObject start)
    {
        if (activeCitizens >= citizenCount) return;
        GameObject citizen = Instantiate(citizenPrefab);
        activeCitizens++;
        var agent = citizen.GetComponent<CitizenAgent>();
        agent.Init(start, this);
        StartCoroutine(TrackCitizen(citizen));
    }

    IEnumerator TrafficLoop()
    {
        while (true)
        {
            if (districtTiles.Count > 1)
            {
                TileObject from =
                    districtTiles[Random.Range(0, districtTiles.Count)];
                SpawnCar(from);
                SpawnCitizen(from);
            }
            yield return new WaitForSeconds(2f);
        }
    }

    TileObject GetRandomRoadTile()
    {
        for (int i = 0; i < 100; i++)
        {
            int x = Random.Range(0, width);
            int z = Random.Range(0, height);
            TileObject tile = GetTileAt(x, z);
            if (tile != null && tile.HasRoad()) return tile;
        }
        return null;
    }

    IEnumerator TrackCar(GameObject car)
    {
        while (car != null) yield return null;
        activeCars--;
    }

    IEnumerator TrackCitizen(GameObject c)
    {
        while (c != null) yield return null;
        activeCitizens--;
    }

    public TileObject GetRandomDistrict()
    {
        if (districtTiles.Count == 0) return null;
        return districtTiles[Random.Range(0, districtTiles.Count)];
    }

    void GenerateTileResource(WorldTile tile)
    {
        switch (tile.terrainType)
        {
            case TerrainType.Forest:
                tile.resourceType = ResourceType.Wood;
                tile.resourceAmount = int.MaxValue; // infinite
                break;
            case TerrainType.Sand:
                if (Random.value < 0.25f)
                {
                    tile.resourceType = ResourceType.Oil;
                    tile.resourceAmount = Random.Range(600, 1400); // doubled
                }
                break;
            case TerrainType.Clear:
                if (Random.value < 0.35f)
                {
                    tile.resourceType = ResourceType.Farming;
                    tile.resourceAmount = int.MaxValue; // infinite
                }
                break;
            case TerrainType.Mountain:
                if (Random.value < 0.5f)
                {
                    tile.resourceType = ResourceType.Minerals;
                    tile.resourceAmount = Random.Range(600, 1200); // doubled
                }
                break;
        }

        if (tile.terrainType != TerrainType.Water)
        {
            if (Random.value < 0.08f)
            {
                tile.resourceType = ResourceType.Iron;
                tile.resourceAmount = Random.Range(500, 1000); // doubled
            }
        }
    }

    void AssignResourceToTile(WorldTile tile)
    {
        switch (tile.terrainType)
        {
            case TerrainType.Forest:
                tile.resourceType = ResourceType.Wood;
                tile.resourceAmount = int.MaxValue; // infinite
                break;
            case TerrainType.Sand:
                if (Random.value < 0.25f)
                {
                    tile.resourceType = ResourceType.Oil;
                    tile.resourceAmount = Random.Range(800, 2000); // doubled
                }
                break;
            case TerrainType.Clear:
                tile.resourceType = ResourceType.Farming;
                tile.resourceAmount = int.MaxValue; // infinite
                if (Random.value < 0.08f)
                {
                    tile.resourceType = ResourceType.Iron;
                    tile.resourceAmount = Random.Range(600, 1600); // doubled
                }
                break;
            case TerrainType.Mountain:
                tile.resourceType = ResourceType.Minerals;
                tile.resourceAmount = Random.Range(800, 1800); // doubled
                break;
        }
    }
}