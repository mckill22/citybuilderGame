using UnityEngine;
using System.Collections.Generic;
public class TileObject : MonoBehaviour
{
    public Vector2Int gridPosition;
    public WorldTile tileData;

    private GridManager gridManager;
    private Renderer rend;
    private GameObject coverageVisual;
    private GameObject roadObject;
    public GameObject districtObject;
    private GameObject decorationObject;
    private GameObject resourceObject;
    public ProductionType productionType = ProductionType.None;
    public GameObject buildingObject;
    public BuildingLogistics buildingLogistics;
    public BuildingInventory buildingInventory;

    // ─────────────────────────────────────────
    // UPKEEP STATE
    // ─────────────────────────────────────────
    [HideInInspector] public bool isFunded = true;
    [HideInInspector] public int gracePeriodMonths = 0;

    // Assigned at placement time — logistics UI label
    // e.g. "PowerPlant #2 — Baghdad"
    [HideInInspector] public int buildingNumber = 0;
    void Awake()
    {
        rend = GetComponent<Renderer>();
    }



    public void SetTile(WorldTile tile, GridManager manager)
    {
        tileData = tile;
        gridPosition = tile.gridPos;
        gridManager = manager;
    }

    // =========================
    // VISUALS
    // =========================

    public void Highlight()
    {
        rend.material.color = Color.white;
    }

    public void ResetColor()
    {
        ApplyTerrainColor();
    }

    public void ApplyTerrainColor()
    {
        if (tileData == null) return;

        // If SeasonalColorManager is active, let it
        // handle colors so season + weather tints apply.
        if (SeasonalColorManager.Instance != null)
        {
            SeasonalColorManager.Instance.ApplyTileColor(this);
            return;
        }

        // Fallback: plain terrain colors
        switch (tileData.terrainType)
        {
            case TerrainType.Clear: rend.material.color = new Color(0.3f, 0.8f, 0.3f); break;
            case TerrainType.Forest: rend.material.color = new Color(0.1f, 0.5f, 0.1f); break;
            case TerrainType.Water: rend.material.color = new Color(0.1f, 0.4f, 0.8f); break;
            case TerrainType.Mountain: rend.material.color = new Color(0.5f, 0.5f, 0.5f); break;
            case TerrainType.Sand: rend.material.color = new Color(0.9f, 0.8f, 0.4f); break;
        }
    }

    // =============================================
    // BORDER COLOR
    // Subtle per-city ownership tint layered on
    // top of terrain color. Called by CityManager
    // whenever borders are stamped or expanded.
    // =============================================
    public void ApplyBorderColor(int cityId)
    {
        if (rend == null) return;

        // SeasonalColorManager.ApplyTileColor already
        // handles city tinting — let it do the full job.
        if (SeasonalColorManager.Instance != null)
        {
            SeasonalColorManager.Instance.ApplyTileColor(this);
            return;
        }

        ApplyTerrainColor();
        Color baseColor = rend.material.color;

        Color tint;
        switch (cityId)
        {
            case 0: tint = new Color(0.9f, 0.9f, 1.0f); break; // capital: soft blue-white
            case 1: tint = new Color(1.0f, 0.95f, 0.8f); break; // city 2: warm yellow
            case 2: tint = new Color(0.9f, 1.0f, 0.9f); break; // city 3: light green
            case 3: tint = new Color(1.0f, 0.88f, 0.88f); break; // city 4: soft pink
            default: tint = new Color(0.95f, 0.95f, 0.95f); break;
        }

        // 30% tint keeps terrain type still readable
        rend.material.color = Color.Lerp(baseColor, tint, 0.3f);
    }
    public void SpawnDecoration()
    {
        if (decorationObject != null)
        {
            Destroy(decorationObject);
        }
        if (tileData == null) return;

        GameObject[] pool = null;

        switch (tileData.terrainType)
        {
            case TerrainType.Mountain:
                pool = gridManager.mountainPrefabs;
                break;

            case TerrainType.Sand:
                pool = gridManager.sandProps;
                break;

            case TerrainType.Forest:
                pool = gridManager.forestPrefabs;
                break;

            case TerrainType.Clear:
                pool = gridManager.grassPrefabs;
                break;

            case TerrainType.Water:
                return;
        }

        if (pool == null || pool.Length == 0) return;

        GameObject prefab = pool[Random.Range(0, pool.Length)];

        decorationObject = Instantiate(prefab, transform);

        // ✔ FORCE correct local placement
        decorationObject.transform.localPosition = new Vector3(
            0f,
            0.5f,
            0f
        );

        decorationObject.transform.localRotation = Quaternion.identity;
        decorationObject.transform.localScale = prefab.transform.localScale;
    }

    // =========================
    // BUILDINGS
    // =========================

    public bool HasBuilding()
    {
        return tileData.buildingType != BuildingType.None;
    }

    public bool HasRoad()
    {
        return tileData.roadType != RoadType.None;
    }

    public bool IsEmpty()
    {
        return !HasBuilding() && !HasRoad();
    }

    // =========================
    // DISTRICTS
    // =========================

    public bool HasDistrict()
    {
        return tileData.buildingType != BuildingType.None;
    }

    public void PlaceBuilding(GameObject prefab, float rotationY)
    {

        if (HasDistrict()) return; // only block if district exists
                                   // Allow special resource buildings even on non-buildable terrain
        bool isResourceBuilding =
            prefab.name.Contains("Mine") ||
            prefab.name.Contains("Oil") ||
            prefab.name.Contains("Lumber") ||
            prefab.name.Contains("Iron") ||
            prefab.name.Contains("Farm");

        if (!tileData.IsBuildable() && !isResourceBuilding)
            return;
        Vector3 pos = transform.position + new Vector3(0, 0.05f, 0);
        bool isMine =
      prefab.name.Contains("Mine");
        if (!isMine && decorationObject != null)
            Destroy(decorationObject);
        if (resourceObject != null)
            Destroy(resourceObject);
        districtObject = Instantiate(
         prefab,
         pos,
         Quaternion.Euler(0, rotationY, 0)
     );
        SetCurrentBuilding(districtObject);
        buildingObject = districtObject;
        buildingLogistics = districtObject.GetComponent<BuildingLogistics>();
        buildingInventory = districtObject.GetComponent<BuildingInventory>();
        // 🔥 start construction animation
        StartCoroutine(BuildAnimation(districtObject));

        if (prefab.name.Contains("Residential"))
            tileData.buildingType = BuildingType.Residential;

        else if (prefab.name.Contains("Industrial"))
            tileData.buildingType = BuildingType.Industrial;

        else if (prefab.name.Contains("Farm"))
            tileData.buildingType = BuildingType.Farm;

        else if (prefab.name.Contains("OilRefinery"))
            tileData.buildingType = BuildingType.OilRefinery;

        else if (prefab.name.Contains("Oil"))
            tileData.buildingType = BuildingType.OilExtractor;

        else if (prefab.name.Contains("Mine"))
            tileData.buildingType = BuildingType.Mine;

        else if (prefab.name.Contains("Lumber"))
            tileData.buildingType = BuildingType.LumberYard;

        else if (prefab.name.Contains("Iron"))
            tileData.buildingType = BuildingType.IronMine;
        else if (prefab.name.Contains("Power"))
            tileData.buildingType = BuildingType.PowerPlant;

        else if (prefab.name.Contains("Commercial"))
            tileData.buildingType = BuildingType.Commercial;
        else if (prefab.name.Contains("Heavy"))
            tileData.buildingType = BuildingType.HeavyComplex;

        else if (prefab.name.Contains("GStorage"))
            tileData.buildingType = BuildingType.GeneralStorage;

        else if (prefab.name.Contains("OStorage"))
            tileData.buildingType = BuildingType.OilStorage;

        else if (prefab.name.Contains("IStorage"))
            tileData.buildingType = BuildingType.IndustrialStorage;

        else if (prefab.name.Contains("HStorage"))
            tileData.buildingType = BuildingType.HardwareStorage;
        else if (prefab.name.Contains("Police"))
            tileData.buildingType = BuildingType.PoliceStation;

        else if (prefab.name.Contains("Hospital"))
            tileData.buildingType = BuildingType.Hospital;

        else if (prefab.name.Contains("College"))
            tileData.buildingType = BuildingType.CollegeCampus;

        else if (prefab.name.Contains("GasStation"))
            tileData.buildingType = BuildingType.GasStation;

        else if (prefab.name.Contains("TownHall"))
        {
            tileData.buildingType = BuildingType.TownHall;

            // Found a new city centred on this tile
            if (CityManager.Instance != null)
            {
                CityData newCity =
                    CityManager.Instance.FoundCity(gridPosition);

                // If this is the capital, close setup UI
                if (newCity.isCapital &&
                    GameStartUI.Instance != null)
                {
                    GameStartUI.Instance.OnCapitalFounded();
                }
            }
        }
        if (tileData.buildingType == BuildingType.Industrial && productionType == ProductionType.None)
        {
            productionType = ProductionType.Civilian;
        }

        // Assign building number for logistics UI
        AssignBuildingLabel();
    }

    // Returns e.g. "PowerPlant #2 — Baghdad"
    public string GetBuildingLabel()
    {
        string type = tileData != null
            ? tileData.buildingType.ToString() : "Building";
        if (buildingNumber <= 0) return type;
        int cityId = tileData != null ? tileData.cityId : -1;
        string city = "";
        if (CityManager.Instance != null && cityId >= 0 &&
            cityId < CityManager.Instance.cities.Count)
            city = " — " + CityManager.Instance.cities[cityId].cityName;
        return type + " #" + buildingNumber + city;
    }

    void AssignBuildingLabel()
    {
        if (GridManager.Instance == null) return;

        int cityId = tileData != null ? tileData.cityId : -1;
        BuildingType type = tileData != null ? tileData.buildingType : BuildingType.None;

        string cityName = "";
        if (CityManager.Instance != null && cityId >= 0 &&
            cityId < CityManager.Instance.cities.Count)
            cityName = CityManager.Instance.cities[cityId].cityName;

        int count = 0;
        foreach (var tile in GridManager.Instance.allTiles)
        {
            if (tile == null || tile == this) continue;
            if (tile.tileData == null) continue;
            if (tile.tileData.buildingType != type) continue;
            if (tile.tileData.cityId != cityId) continue;
            count++;
        }
        buildingNumber = count + 1;
    }

    // =========================
    // ROADS
    // =========================

    public void BuildRoad()
    {
        if (HasRoad()) return;

        if (tileData.terrainType == TerrainType.Water ||
            tileData.terrainType == TerrainType.Mountain)
            return;

        tileData.roadType = RoadType.Road;

        UpdateRoadVisual();
        UpdateNeighbors();
        if (decorationObject != null)
            Destroy(decorationObject);
    }

    void UpdateRoadVisual()
    {
        Quaternion modelFix = Quaternion.Euler(0, 180, 0);
        if (roadObject != null)
            Destroy(roadObject);

        bool north = HasRoadAt(0, 1);
        bool south = HasRoadAt(0, -1);
        bool east = HasRoadAt(1, 0);
        bool west = HasRoadAt(-1, 0);

        int connections =
            (north ? 1 : 0) +
            (south ? 1 : 0) +
            (east ? 1 : 0) +
            (west ? 1 : 0);

        GameObject prefab = null;
        Quaternion rot = Quaternion.identity;

        // CROSS
        if (connections == 4)
        {
            prefab = gridManager.roadCrossPrefab;
        }
        // T
        else if (connections == 3)
        {
            prefab = gridManager.roadTPrefab;

            if (!north) rot = Quaternion.Euler(0, 180, 0);
            else if (!south) rot = Quaternion.Euler(0, 0, 0);
            else if (!east) rot = Quaternion.Euler(0, -90, 0);
            else if (!west) rot = Quaternion.Euler(0, 90, 0);
        }
        // STRAIGHT / CORNER
        else if (connections == 2)
        {
            if ((north && south) || (east && west))
            {
                prefab = gridManager.roadStraightPrefab;

                if (east && west)
                    rot = Quaternion.Euler(0, 90, 0);
            }
            else
            {
                prefab = gridManager.roadCornerPrefab;

                if (north && east) rot = Quaternion.Euler(0, 90, 0);
                else if (east && south) rot = Quaternion.Euler(0, 180, 0);
                else if (south && west) rot = Quaternion.Euler(0, 270, 0);
                else if (west && north) rot = Quaternion.Euler(0, 0, 0);
            }
        }
        // END
        else if (connections == 1)
        {
            prefab = gridManager.roadEndPrefab;

            if (north) rot = Quaternion.Euler(0, 0, 0);
            else if (east) rot = Quaternion.Euler(0, 90, 0);
            else if (south) rot = Quaternion.Euler(0, 180, 0);
            else if (west) rot = Quaternion.Euler(0, 270, 0);
        }
        // SOLO
        else
        {
            prefab = gridManager.roadEndPrefab;
        }

        Vector3 pos = transform.position;
        pos.y = 0.05f;

        Quaternion finalRot = rot * modelFix;

        // 🔥 Fix only T road flipping
        if (prefab == gridManager.roadTPrefab)
        {
            finalRot *= Quaternion.Euler(0, -90, 0);
        }

        roadObject = Instantiate(prefab, pos, finalRot);
    }

    bool HasRoadAt(int dx, int dz)
    {
        TileObject n = gridManager.GetTileAt(
            gridPosition.x + dx,
            gridPosition.y + dz
        );

        if (n == null) return false;

        return n.tileData.roadType != RoadType.None;
    }

    void UpdateNeighbors()
    {
        TryUpdateNeighbor(0, 1);
        TryUpdateNeighbor(0, -1);
        TryUpdateNeighbor(1, 0);
        TryUpdateNeighbor(-1, 0);
    }

    void TryUpdateNeighbor(int dx, int dz)
    {
        TileObject n = gridManager.GetTileAt(
            gridPosition.x + dx,
            gridPosition.y + dz
        );

        if (n != null && n.tileData.roadType != RoadType.None)
        {
            n.UpdateRoadVisual();
        }
    }
    System.Collections.IEnumerator BuildAnimation(GameObject obj)
    {
        float duration = 1.5f;
        float t = 0f;

        Vector3 finalPos = obj.transform.position;
        Vector3 startPos = finalPos + Vector3.down * 1f;

        obj.transform.position = startPos;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            float y = Mathf.Lerp(startPos.y, finalPos.y, t);

            float shake = 0f;
            if (t > 0.7f)
            {
                shake = Mathf.Sin(Time.time * 40f) * 0.03f * (1f - t);
            }

            if (t > 0.8f)
            {
                obj.transform.localPosition += Vector3.up * 0.01f * Mathf.Sin(Time.time * 60f);
            }

            obj.transform.position = new Vector3(
                finalPos.x + shake,
                y,
                finalPos.z + shake
            );

            yield return null;
        }

        obj.transform.position = finalPos;

        yield return new WaitForSeconds(0.3f);
        gridManager.RegisterDistrict(this);
    }
    public void SpawnResourceVisual()
    {
        if (tileData == null) return;

        GameObject prefab = null;

        switch (tileData.resourceType)
        {
            case ResourceType.Wood:
                prefab = gridManager.woodResourcePrefab;
                break;

            case ResourceType.Oil:
                prefab = gridManager.oilResourcePrefab;
                break;

            case ResourceType.Farming:
                prefab = gridManager.farmingResourcePrefab;
                break;

            case ResourceType.Minerals:
                prefab = gridManager.mineralsResourcePrefab;
                break;

            case ResourceType.Iron:
                prefab = gridManager.ironResourcePrefab;
                break;
        }

        if (prefab == null) return;

        resourceObject = Instantiate(prefab, transform);

        resourceObject.transform.localPosition =
            new Vector3(0, 1f, 0);

        resourceObject.transform.localRotation =
            Quaternion.identity;
    }
    public void SetCurrentBuilding(GameObject building)
    {
        districtObject = building;
    }
    public GameObject CurrentBuilding
    {
        get
        {
            return districtObject != null ? districtObject : null;
        }
    }
    public void DemolishBuilding()
    {
        if (districtObject == null)
            return;

        // REMOVE ALL ROUTES FROM THIS BUILDING
        BuildingLogistics logistics =
            districtObject.GetComponent<BuildingLogistics>();

        if (logistics != null)
        {
            List<LogisticsRoute> routesCopy =
                new List<LogisticsRoute>(logistics.routes);

            foreach (var route in routesCopy)
            {
                logistics.RemoveRoute(route);
            }
        }

        // DESTROY BUILDING
        Destroy(districtObject);

        districtObject = null;
        buildingObject = null;
        buildingLogistics = null;
        buildingInventory = null;

        tileData.buildingType = BuildingType.None;
        productionType = ProductionType.None;
        isFunded = true;
        gracePeriodMonths = 0;
        buildingNumber = 0;

        // RESPAWN TERRAIN DECORATION
        SpawnDecoration();

        // Recalculate resource totals so freeWorkers and
        // freeTrucks are accurate before ValidateAllRoutes
        if (GameManager.Instance != null)
            GameManager.Instance.UpdateGlobalResourceTotals();

        // REFRESH LOGISTICS
        if (LogisticsManager.Instance != null)
        {
            LogisticsManager.Instance.RunLogisticsTick();
        }

        // CLOSE UI IF THIS TILE WAS SELECTED
        if (BuildManager.selectedTile == this)
        {
            UIManager.Instance.CloseAllPanels();

            if (LogisticsUIManager.Instance != null)
                LogisticsUIManager.Instance.ClosePanel();

            BuildManager.selectedTile = null;
        }
        if (LogisticsManager.Instance != null)
        {
            LogisticsManager.Instance.ValidateAllRoutes();
        }
    }
    public void DemolishRoad()
    {
        if (roadObject == null)
            return;

        Destroy(roadObject);

        roadObject = null;

        tileData.roadType = RoadType.None;

        UpdateNeighbors();

        // respawn decoration if empty
        if (!HasDistrict())
        {
            SpawnDecoration();
        }
        if (LogisticsManager.Instance != null)
        {
            LogisticsManager.Instance.ValidateAllRoutes();
        }
    }
    public GameObject RoadObject
    {
        get { return roadObject; }
    }
    public void ShowPoliceCoverage(float radius)
    {
        HidePoliceCoverage();

        coverageVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        coverageVisual.transform.position =
            transform.position + Vector3.up * 0.05f;

        coverageVisual.transform.localScale =
            new Vector3(radius * 2f, 0.02f, radius * 2f);

        Renderer rend =
            coverageVisual.GetComponent<Renderer>();

        rend.material = new Material(
            Shader.Find("Standard"));

        Color c = Color.green;
        c.a = 0.25f;

        rend.material.color = c;

        Destroy(coverageVisual.GetComponent<Collider>());
    }
    public void HidePoliceCoverage()
    {
        if (coverageVisual != null)
        {
            Destroy(coverageVisual);
        }
    }
}