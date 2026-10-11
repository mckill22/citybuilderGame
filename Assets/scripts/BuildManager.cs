using UnityEngine;
using UnityEngine.EventSystems;

public class BuildManager : MonoBehaviour
{
    public Camera cam;

    public GameObject residentialPrefab;
    public GameObject industrialPrefab;
    public GameObject roadPrefab;
    public GameObject roadPreviewPrefab;
    public GameObject farmPrefab;
    public GameObject oilExtractorPrefab;
    public GameObject minePrefab;
    public GameObject lumberYardPrefab;
    public GameObject ironMinePrefab;
    public GameObject powerPlantPrefab;
    public GameObject commercialPrefab;
    public GameObject heavyComplexPrefab;
    public GameObject DemolishBtn;
    public GameObject generalStoragePrefab;
    public GameObject oilStoragePrefab;
    public GameObject industrialStoragePrefab;
    public GameObject hardwareStoragePrefab;
    public GameObject policeStationPrefab;
    public GameObject hospitalPrefab;
    public GameObject collegeCampusPrefab;
    public GameObject townHallPrefab;
    public GameObject oilRefineryPrefab;
    public GameObject gasStationPrefab;
    LayerMask tileLayer;
    public GameObject demolishPreviewPrefab;
    private BuildMode currentMode = BuildMode.None;
    private float currentRotation = 0f;
    private GameObject previewObject;
    private bool isDragging = false;
    private TileObject lastTile;
    public static TileObject selectedTile;
    private GameObject lastHoveredBuilding;
    private Color lastHoveredOriginalColor;
    private TileObject lastHoveredTile;
    [Header("Costs")]
    public int roadCost = 2;
    public int residentialCost = 10;
    public int industrialCost = 15;
    public int farmCost = 25;
    public int oilExtractorCost = 60;
    public int mineCost = 50;
    public int lumberYardCost = 30;
    public int ironMineCost = 45;
    public int powerPlantCost = 100;
    public int commercialCost = 50;
    public int heavyComplexCost = 150;
    public int policeStationCost = 120;
    public int hospitalCost = 200;
    public int collegeCampusCost = 350;
    public int generalStorageCost = 80;
    public int oilStorageCost = 120;
    public int industrialStorageCost = 140;
    public int hardwareStorageCost = 180;
    public int townHallCost = 500;
    public int oilRefineryCost = 180;
    public int gasStationCost = 80;
    [Header("UI")]
    public GameObject buildMenuPanel;   // buttons (residential, road, etc)
    public GameObject openBuildButton;  // single "Build" button
    public GameObject GvmPanel;          // government panel (country overview + city list)
    void Start()
    {
        tileLayer = LayerMask.GetMask("Tile");
    }

    void Update()
    {
        // Block all build interactions until
        // the capital TownHall has been placed
        bool setupDone =
            GameStartUI.Instance == null ||
            GameStartUI.Instance.IsSetupDone();

        if (!setupDone &&
            currentMode != BuildMode.TownHall)
        {
            HandlePreview();
            HandlePlacement();
            return;
        }

        HandlePreview();
        HandlePlacement();
        HandleRotation();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitBuildMode();
            UIManager.Instance.CloseAllPanels();
            selectedTile = null;
            buildMenuPanel.SetActive(false);
            openBuildButton.SetActive(true);
            ExitBuildMode();
            UIManager.Instance.logisticsButton.SetActive(false);
            LogisticsUIManager.Instance.ClosePanel();
            DemolishBtn.SetActive(true);
            GvmPanel.SetActive(false);
        }
        if (Input.GetMouseButtonDown(1))
        {
            // Don't close panels if clicking on UI elements
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                UIManager.Instance.CloseAllPanels();
                selectedTile = null;
                buildMenuPanel.SetActive(false);
                openBuildButton.SetActive(true);
                ExitBuildMode();
                UIManager.Instance.logisticsButton.SetActive(false);
                LogisticsUIManager.Instance.ClosePanel();
                DemolishBtn.SetActive(true);
                GvmPanel.SetActive(false);
            }
        }
    }

    // =========================
    // UI HOOKS
    // =========================

    public void SetBuildModeFromUI(int modeIndex)
    {
        SetBuildMode((BuildMode)modeIndex);
    }

    public void ExitBuildMode()
    {
        SetBuildMode(BuildMode.None);

    }

    void SetBuildMode(BuildMode mode)
    {
        if (UIManager.Instance != null)
        {
            if (mode == BuildMode.None)
            {
                UIManager.Instance.SetBuildModeText("", Color.white);
            }
            else if (mode == BuildMode.Demolish)
            {
                UIManager.Instance.SetBuildModeText(
                    "DEMOLISH MODE",
                    Color.red
                );
            }
            else
            {
                UIManager.Instance.SetBuildModeText(
     "BUILD MODE: " + mode.ToString(),
     Color.green
 );
            }
        }
        currentMode = mode;

        if (previewObject != null)
            Destroy(previewObject);

        if (mode == BuildMode.None)
        {
            // Restore any demolish-hovered building color
            if (lastHoveredBuilding != null)
            {
                Renderer rend = lastHoveredBuilding.GetComponentInChildren<Renderer>();
                if (rend != null)
                    rend.material.color = lastHoveredOriginalColor;
                lastHoveredBuilding = null;
            }

            // Restore any demolish-hovered road color
            if (lastHoveredTile != null && lastHoveredTile.RoadObject != null)
            {
                Renderer rend = lastHoveredTile.RoadObject.GetComponentInChildren<Renderer>();
                if (rend != null)
                    rend.material.color = lastHoveredOriginalColor;
            }
            lastHoveredTile = null;

            buildMenuPanel.SetActive(false);
            openBuildButton.SetActive(true);
            return;
        }

        // Demolish should NOT open the build menu
        if (mode == BuildMode.Demolish)
        {
            buildMenuPanel.SetActive(false);
            openBuildButton.SetActive(false);
            GameObject prefab = GetPrefabForMode(mode);
            previewObject = Instantiate(prefab);
            SetPreviewMaterial(previewObject);
            return;
        }
        // Restore any demolish-hovered building color
        if (lastHoveredBuilding != null)
        {
            Renderer rend = lastHoveredBuilding.GetComponentInChildren<Renderer>();
            if (rend != null)
                rend.material.color = lastHoveredOriginalColor;
            lastHoveredBuilding = null;
        }
        buildMenuPanel.SetActive(true);
        openBuildButton.SetActive(false);

        GameObject modePrefab = GetPrefabForMode(mode);
        currentRotation = 0f;
        if (modePrefab == null)
        {
            Debug.LogWarning($"[BuildManager] No prefab assigned for mode: {mode}");
            currentMode = BuildMode.None;
            return;
        }
        previewObject = Instantiate(modePrefab);
        SetPreviewMaterial(previewObject);
    }
    // =========================
    // PREVIEW (NO BUILD HERE)
    // =========================

    void HandlePreview()
    {
        if (currentMode == BuildMode.None || previewObject == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, tileLayer))
        {
            TileObject tile = hit.collider.GetComponentInParent<TileObject>();

            if (tile == null) return;

            Vector3 pos = tile.transform.position;
            pos.y = 0.1f;

            previewObject.transform.position = pos;

            previewObject.transform.rotation = Quaternion.Lerp(
                previewObject.transform.rotation,
                Quaternion.Euler(0, currentRotation, 0),
                Time.deltaTime * 15f
            );

            if (currentMode == BuildMode.Demolish)
            {
                if (tile != lastHoveredTile)
                {
                    // Restore previous building
                    if (lastHoveredBuilding != null)
                    {
                        Renderer prevRend = lastHoveredBuilding.GetComponentInChildren<Renderer>();
                        if (prevRend != null)
                            prevRend.material.color = lastHoveredOriginalColor;
                        lastHoveredBuilding = null;
                    }

                    // Restore previous road
                    if (lastHoveredTile != null && lastHoveredTile.HasRoad())
                    {
                        Renderer prevRend = lastHoveredTile.RoadObject.GetComponentInChildren<Renderer>();
                        if (prevRend != null)
                            prevRend.material.color = lastHoveredOriginalColor;
                    }

                    // Tint new building
                    if (tile.CurrentBuilding != null)
                    {
                        Renderer rend = tile.CurrentBuilding.GetComponentInChildren<Renderer>();
                        if (rend != null)
                        {
                            lastHoveredOriginalColor = rend.material.color;
                            Color red = Color.red;
                            red.a = rend.material.color.a;
                            rend.material.color = red;
                            lastHoveredBuilding = tile.CurrentBuilding;
                        }
                    }
                    // Tint road if no building
                    else if (tile.HasRoad())
                    {
                        Renderer rend = tile.RoadObject.GetComponentInChildren<Renderer>();
                        if (rend != null)
                        {
                            lastHoveredOriginalColor = rend.material.color;
                            Color red = Color.red;
                            red.a = rend.material.color.a;
                            rend.material.color = red;
                        }
                    }

                    lastHoveredTile = tile;
                }

                return;
            }
            // Restore if we switched out of demolish while hovering
            if (lastHoveredBuilding != null)
            {
                Renderer prevRend = lastHoveredBuilding.GetComponentInChildren<Renderer>();
                if (prevRend != null)
                    prevRend.material.color = lastHoveredOriginalColor;
                lastHoveredBuilding = null;
            }

            bool canBuild;

            if (currentMode == BuildMode.Road)
            {
                canBuild = !tile.HasRoad() &&
                           tile.tileData.terrainType != TerrainType.Water &&
                           tile.tileData.terrainType != TerrainType.Mountain;
            }
            else
            {
                canBuild = CanPlaceDistrict(tile);
            }

            int cost = GetCost(currentMode);
            bool canAfford = GameManager.Instance.money >= cost;

            SetPreviewColor((canBuild && canAfford) ? Color.green : Color.red);
        }
    }


    // =========================
    // ACTUAL BUILDING
    // =========================

    void HandlePlacement()
    {
        // 👉 SELECT TILE (when NOT building)
        if (Input.GetMouseButtonDown(0) && currentMode == BuildMode.None)
        {
            // Don't allow selecting buildings while logistics panel is open
            if (LogisticsUIManager.Instance != null &&
                LogisticsUIManager.Instance.IsOpen())
            {
                return;
            }

            if (EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, tileLayer))
            {
                TileObject tile = hit.collider.GetComponentInParent<TileObject>();

                if (tile != null)
                {
                    selectedTile = tile;
                    UIManager.Instance.ShowBuildingPanel(tile);
                }
            }

            return;
        }
        if (currentMode == BuildMode.None) return;
        // DEMOLISH MODE
        if (currentMode == BuildMode.Demolish)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100f, tileLayer))
                {
                    TileObject tile =
                        hit.collider.GetComponentInParent<TileObject>();

                    if (tile == null)
                        return;

                    if (tile.HasDistrict())
                    {
                        tile.DemolishBuilding();

                        GameManager.Instance.RunEconomyTick();
                    }
                    else if (tile.HasRoad())
                    {
                        tile.DemolishRoad();
                    }
                }
            }

            return;
        }
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        if (LogisticsUIManager.Instance != null &&
    LogisticsUIManager.Instance.destinationClickBlockTimer > 0f)
        {
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, tileLayer))
            {
                TileObject tile = hit.collider.GetComponentInParent<TileObject>();

                if (tile == null) return;

                bool canBuild;

                if (currentMode == BuildMode.Road)
                {
                    canBuild =
                        !tile.HasRoad() &&
                        tile.tileData.terrainType != TerrainType.Water && tile.tileData.terrainType != TerrainType.Mountain;
                }
                else
                {
                    canBuild = CanPlaceDistrict(tile);
                }

                if (!canBuild) return;

                int cost = GetCost(currentMode);

                if (!GameManager.Instance.SpendMoney(cost))
                {
                    UIManager.Instance.ShowNoMoney();
                    return;
                }

                UIManager.Instance.ShowCost(tile.transform.position, cost);
                // ✅ build AFTER payment
                if (currentMode == BuildMode.Road)
                    tile.BuildRoad();
                else
                    tile.PlaceBuilding(GetPrefabForMode(currentMode), currentRotation);
            }
        }
    }


    // =========================
    // HELPERS
    // =========================

    GameObject GetPrefabForMode(BuildMode mode)
    {
        switch (mode)
        {
            case BuildMode.Residential: return residentialPrefab;
            case BuildMode.Industrial: return industrialPrefab;
            case BuildMode.Road: return roadPreviewPrefab;
            case BuildMode.Farm:
                return farmPrefab;

            case BuildMode.OilExtractor:
                return oilExtractorPrefab;

            case BuildMode.Mine:
                return minePrefab;

            case BuildMode.LumberYard:
                return lumberYardPrefab;

            case BuildMode.IronMine:
                return ironMinePrefab;
            case BuildMode.PowerPlant:
                return powerPlantPrefab;

            case BuildMode.Commercial:
                return commercialPrefab;
            case BuildMode.HeavyComplex:
                return heavyComplexPrefab;

            case BuildMode.GeneralStorage:
                return generalStoragePrefab;

            case BuildMode.OilStorage:
                return oilStoragePrefab;

            case BuildMode.IndustrialStorage:
                return industrialStoragePrefab;

            case BuildMode.HardwareStorage:
                return hardwareStoragePrefab;
            case BuildMode.Demolish:
                return demolishPreviewPrefab;
            case BuildMode.PoliceStation:
                return policeStationPrefab;

            case BuildMode.Hospital:
                return hospitalPrefab;

            case BuildMode.CollegeCampus:
                return collegeCampusPrefab;

            case BuildMode.TownHall:
                return townHallPrefab;
            case BuildMode.OilRefinery:
                return oilRefineryPrefab;
            case BuildMode.GasStation:
                return gasStationPrefab;
        }

        return null;
    }

    int GetCost(BuildMode mode)
    {
        switch (mode)
        {
            case BuildMode.Road:
                return roadCost;

            case BuildMode.Residential:
                return residentialCost;

            case BuildMode.Industrial:
                return industrialCost;

            case BuildMode.Farm:
                return farmCost;

            case BuildMode.OilExtractor:
                return oilExtractorCost;

            case BuildMode.Mine:
                return mineCost;

            case BuildMode.LumberYard:
                return lumberYardCost;

            case BuildMode.IronMine:
                return ironMineCost;
            case BuildMode.PowerPlant:
                return powerPlantCost;

            case BuildMode.Commercial:
                return commercialCost;
            case BuildMode.HeavyComplex:
                return heavyComplexCost;

            case BuildMode.GeneralStorage:
                return generalStorageCost;

            case BuildMode.OilStorage:
                return oilStorageCost;

            case BuildMode.IndustrialStorage:
                return industrialStorageCost;

            case BuildMode.HardwareStorage:
                return hardwareStorageCost;
            case BuildMode.PoliceStation:
                return policeStationCost;

            case BuildMode.Hospital:
                return hospitalCost;

            case BuildMode.CollegeCampus:
                return collegeCampusCost;

            case BuildMode.TownHall:
                return townHallCost;
            case BuildMode.OilRefinery:
                return oilRefineryCost;
            case BuildMode.GasStation:
                return gasStationCost;
        }


        return 0;
    }

    void SetPreviewMaterial(GameObject obj)
    {
        Renderer rend = obj.GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            Color c = rend.material.color;
            c.a = 0.5f;
            rend.material.color = c;
        }
    }

    void SetPreviewColor(Color color)
    {
        if (previewObject == null) return;

        Renderer rend = previewObject.GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            color.a = 0.5f;
            rend.material.color = color;
        }
    }

    void HandleRotation()
    {
        if (currentMode == BuildMode.None) return;

        if (Input.GetKeyDown(KeyCode.Q))
            currentRotation -= 90f;

        if (Input.GetKeyDown(KeyCode.E))
            currentRotation += 90f;
    }

    public bool IsInBuildMode()
    {
        return currentMode != BuildMode.None;
    }

    // =========================================
    // GOVERNMENT PANEL
    // Called by the "Manage Government" button.
    // Opens GvmPanel and notifies GovernmentPanelUI
    // to refresh its city list and stats.
    // =========================================
    public void OpenGovernmentPanel()
    {
        if (GvmPanel != null)
            GvmPanel.SetActive(true);

        if (GovernmentPanelUI.Instance != null)
            GovernmentPanelUI.Instance.OnPanelOpened();
    }

    // =========================================
    // TOWN HALL PLACEMENT
    // Called by GameStartUI (first city) and by
    // the "Found New City" button (subsequent).
    // =========================================
    public void BeginTownHallPlacement()
    {
        if (CityManager.Instance == null ||
            !CityManager.Instance.gameStarted)
        {
            SetBuildMode(BuildMode.TownHall);
            return;
        }

        if (!CityManager.Instance.CanFoundNewCity())
        {
            Debug.Log("[BuildManager] Need " +
                CityManager.Instance.GetNextCityThreshold() +
                " pop to found next city.");
            return;
        }

        SetBuildMode(BuildMode.TownHall);
    }

    public bool CanFoundNewCity()
    {
        if (CityManager.Instance == null) return false;
        if (!CityManager.Instance.gameStarted) return false;
        return CityManager.Instance.CanFoundNewCity();
    }

    bool CanPlaceDistrict(TileObject tile)
    {
        if (tile.HasDistrict()) return false;

        // ── TOWN HALL ────────────────────────
        if (currentMode == BuildMode.TownHall)
        {
            if (!tile.tileData.IsBuildable()) return false;

            // Capital: allowed before game starts
            if (CityManager.Instance == null ||
                !CityManager.Instance.gameStarted)
                return true;

            // Extra cities: outside borders + pop met
            if (!CityManager.Instance.CanFoundNewCity())
                return false;

            if (tile.tileData.cityId >= 0) return false;

            return true;
        }

        // ── STANDARD BUILDINGS ───────────────
        // Must be inside a city border
        if (CityManager.Instance != null &&
            CityManager.Instance.gameStarted &&
            tile.tileData.cityId < 0)
            return false;

        switch (currentMode)
        {
            case BuildMode.Farm:
                return tile.tileData.resourceType ==
                       ResourceType.Farming;
            case BuildMode.OilExtractor:
                return tile.tileData.resourceType ==
                       ResourceType.Oil;
            case BuildMode.Mine:
                return tile.tileData.resourceType ==
                       ResourceType.Minerals;
            case BuildMode.LumberYard:
                return tile.tileData.resourceType ==
                       ResourceType.Wood;
            case BuildMode.IronMine:
                return tile.tileData.resourceType ==
                       ResourceType.Iron;
            default:
                return tile.tileData.IsBuildable();
        }
    }

}