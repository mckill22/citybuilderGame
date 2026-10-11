using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    // ─────────────────────────────────────────
    // CLOCK — date shown in main HUD
    // Full speed controls live in ClockUI.
    // ─────────────────────────────────────────
    [Header("Clock")]
    public TextMeshProUGUI dateText;    // "15 March 1952"
    public TextMeshProUGUI seasonText;  // "Spring" (optional)

    // ─────────────────────────────────────────
    // GLOBAL RESOURCES
    // ─────────────────────────────────────────
    [Header("Global Resources")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI populationText;
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI oilText;
    public TextMeshProUGUI farmingText;
    public TextMeshProUGUI mineralsText;
    public TextMeshProUGUI ironText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI civilianGoodsText;
    public TextMeshProUGUI heavySuppliesText;
    public TextMeshProUGUI gasolineText;
    public TextMeshProUGUI militarySuppliesText;
    public TextMeshProUGUI workforceText;

    // ─────────────────────────────────────────
    // PER-CITY SERVICES
    // These always show the *viewed* city.
    // ─────────────────────────────────────────
    [Header("City Services")]
    public TextMeshProUGUI happinessText;
    public TextMeshProUGUI healthcareText;
    public TextMeshProUGUI educationText;
    public TextMeshProUGUI crimeText;

    // ─────────────────────────────────────────
    // CITY IDENTITY PANEL
    // Shows name, leader, government, tax.
    // Switches when player clicks a Town Hall.
    // ─────────────────────────────────────────
    [Header("City Identity")]
    public TextMeshProUGUI cityViewNameText;    // "★ Baghdad" or "City 2"
    public TextMeshProUGUI countryNameText;     // country name
    public TextMeshProUGUI capitalNameText;     // capital city name
    public TextMeshProUGUI leaderNameText;      // capital's leader name
    public TextMeshProUGUI governmentTypeText;  // capital's government
    public TextMeshProUGUI taxRateText;         // global tax rate
    public TextMeshProUGUI cityPopulationText;  // viewed city's population

    // Tax controls live in GovernmentPanelUI.
    // GvmPanel open/close is handled by BuildManager.

    [Header("Town Hall Button")]
    public UnityEngine.UI.Button buildTownHallButton; // locked until pop threshold
    public TextMeshProUGUI townHallUnlockText;  // "New city at 1250 pop" 

    // ─────────────────────────────────────────
    // FEEDBACK
    // ─────────────────────────────────────────
    [Header("Feedback UI")]
    public GameObject noMoneyText;
    public GameObject floatingTextPrefab;
    public Canvas mainCanvas;
    public GameObject logisticsButton;

    // ─────────────────────────────────────────
    // BUILDING PANELS
    // ─────────────────────────────────────────
    [Header("Building Panels")]
    public GameObject industrialPanel;
    public TextMeshProUGUI industrialProductionText;

    [Header("Building Inventory Overlay")]
    public GameObject buildingInfoPanel;
    public TextMeshProUGUI buildingTitleText;
    public TextMeshProUGUI buildingInventoryText;
    public TextMeshProUGUI productionCostText;
    [Header("Build Mode UI")]
    public TextMeshProUGUI buildModeText;
    // ─────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────
    private TileObject currentTile;
    private BuildingInventoryUI currentInventoryUI;

    // Which city the HUD is showing.
    // Null = before game starts (shows GameManager globals).
    private CityData viewedCity = null;

    // =========================================
    void Awake()
    {
        Instance = this;
    }

    // =========================================
    // CITY VIEW SWITCHING
    // Called by InputManager when a TownHall
    // tile is clicked.
    // =========================================
    public void SwitchCityView(CityData city)
    {
        viewedCity = city;


    }

    CityData GetViewedCity()
    {
        if (CityManager.Instance == null ||
            !CityManager.Instance.gameStarted)
            return null;

        // Default to capital on first access
        if (viewedCity == null)
            viewedCity = CityManager.Instance.Capital;

        return viewedCity;
    }

    // Tax controls moved to GovernmentPanelUI.

    // =========================================
    // MAIN UPDATE LOOP
    // =========================================
    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (GameManager.Instance == null) return;
        var gm = GameManager.Instance;

        // ── CLOCK ─────────────────────────────
        if (GameClock.Instance != null)
        {
            if (dateText != null)
                dateText.text =
                    GameClock.Instance.GetDateString();

            if (seasonText != null)
                seasonText.text =
                    GameClock.Instance
                        .GetCurrentSeason().ToString();
        }

        // ── GLOBAL RESOURCES ─────────────────
        moneyText.text = gm.money.ToString();
        // Global: sum of all city populations
        int totalPop = (CityManager.Instance != null &&
                        CityManager.Instance.gameStarted)
            ? CityManager.Instance.GetTotalPopulation()
            : gm.population;
        populationText.text = totalPop + " / " + gm.maxPopulation;
        woodText.text = "" + gm.wood;
        oilText.text = " " + gm.oil;
        farmingText.text = " " + gm.farming;
        mineralsText.text = " " + gm.minerals;
        ironText.text = " " + gm.iron;
        civilianGoodsText.text = " " + gm.civilianGoods;
        if (heavySuppliesText != null) heavySuppliesText.text = " " + gm.heavySupplies;
        if (gasolineText != null) gasolineText.text = " " + gm.gasoline;
        militarySuppliesText.text = " " + gm.militarySupplies;
        powerText.text = gm.powerProduced + " / " + gm.powerConsumed;

        float workerRatio = gm.totalWorkers > 0
     ? (float)gm.employedWorkers / gm.totalWorkers : 0f;

        int freeWorkers = Mathf.Max(0, gm.totalWorkers - gm.employedWorkers);

        string freeColor =
            freeWorkers <= 0 ? "#ff4444" :
            freeWorkers < 10 ? "#ffaa00" :
            "#44ff44";

        workforceText.text =
            $"{gm.employedWorkers} / {gm.totalWorkers} " +
            $"(<color={freeColor}>{freeWorkers} free</color>)";

        workforceText.color = workerRatio > 0.9f ? Color.red
            : workerRatio > 0.7f ? new Color(1f, 0.6f, 0f)
            : Color.white;

        // ── PER-CITY STATS ───────────────────
        CityData city = GetViewedCity();

        if (city != null)
        {
            happinessText.text = city.happiness + "%";
            healthcareText.text = city.healthcare + "%";
            educationText.text = city.education + "%";
            crimeText.text = city.crime + "%";

            happinessText.color = GetServiceColor(city.happiness);
            healthcareText.color = GetServiceColor(city.healthcare);
            educationText.color = GetServiceColor(city.education);
            crimeText.color = GetCrimeColor(city.crime);

            // City identity
            if (cityViewNameText != null)
                cityViewNameText.text =
                    (city.isCapital ? "★ " : "") + city.cityName;

            if (cityPopulationText != null)
                cityPopulationText.text = "Pop: " + city.population;

            RefreshTownHallButton();



            // Government identity — always from capital
            var cap = CityManager.Instance.Capital;
            if (cap != null)
            {
                if (countryNameText != null)
                    countryNameText.text =
                        CityManager.Instance.countryName;

                if (capitalNameText != null)
                    capitalNameText.text =
                        "Capital: " + cap.cityName;

                if (leaderNameText != null)
                    leaderNameText.text =
                        "Leader: " + cap.leaderName;

                if (governmentTypeText != null)
                    governmentTypeText.text =
                        cap.governmentType.ToString();
            }
        }
        else
        {
            // Before game starts — fall back to globals
            happinessText.text = gm.happiness + "%";
            healthcareText.text = gm.healthcare + "%";
            educationText.text = gm.education + "%";
            crimeText.text = gm.crime + "%";

            happinessText.color = GetServiceColor(gm.happiness);
            healthcareText.color = GetServiceColor(gm.healthcare);
            educationText.color = GetServiceColor(gm.education);
            crimeText.color = GetCrimeColor(gm.crime);
        }

        // ── BUILDING OVERLAY ─────────────────
        if (buildingInfoPanel != null &&
            buildingInfoPanel.activeSelf &&
            currentTile != null)
            RefreshBuildingInventoryOverlay();

        if (industrialPanel != null &&
            industrialPanel.activeSelf &&
            currentTile != null)
            UpdateIndustrialPanel(currentTile);
    }

    // =========================================
    // NO MONEY FEEDBACK
    // =========================================
    public void ShowNoMoney()
    {
        if (noMoneyText == null) return;
        StopAllCoroutines();
        StartCoroutine(NoMoneyRoutine());
    }

    System.Collections.IEnumerator NoMoneyRoutine()
    {
        noMoneyText.SetActive(true);
        yield return new WaitForSeconds(1f);
        noMoneyText.SetActive(false);
    }

    // =========================================
    // FLOATING COST TEXT
    // =========================================
    public void ShowCost(Vector3 worldPos, int amount)
    {
        if (floatingTextPrefab == null || mainCanvas == null) return;

        GameObject obj = Instantiate(
            floatingTextPrefab, mainCanvas.transform);
        RectTransform rt = obj.GetComponent<RectTransform>();
        TextMeshProUGUI t = obj.GetComponent<TextMeshProUGUI>();

        if (t != null) t.text = "-" + amount + "$";

        Vector3 screenPos = Camera.main.WorldToScreenPoint(
            worldPos + Vector3.up * 2f);
        rt.position = screenPos;

        StartCoroutine(FloatText(obj));
    }

    System.Collections.IEnumerator FloatText(GameObject obj)
    {
        float t = 0f;
        RectTransform rt = obj.GetComponent<RectTransform>();
        TextMeshProUGUI tx = obj.GetComponent<TextMeshProUGUI>();
        Vector3 startPos = rt.position;
        Color c = tx.color;

        while (t < 1f)
        {
            t += Time.deltaTime;
            rt.position = startPos + new Vector3(0, t * 50f, 0);
            c.a = 1f - t;
            tx.color = c;
            yield return null;
        }

        Destroy(obj);
    }

    // =========================================
    // BUILDING PANEL
    // =========================================
    public void ShowBuildingPanel(TileObject tile)
    {
        currentTile = tile;
        HideAllCoverageVisuals();

        if (tile.tileData.buildingType == BuildingType.PoliceStation)
            tile.ShowPoliceCoverage(6f);

        // Town Hall — switch city view and open
        // the government panel for that city
        if (tile.tileData.buildingType == BuildingType.TownHall)
        {
            CityData city = CityManager.Instance != null
                ? CityManager.Instance.GetCityAt(tile.gridPosition)
                : null;

            if (city != null)
                SwitchCityView(city);

            // Open government panel via BuildManager
            // so its open/close logic stays in one place
            BuildManager bm = FindObjectOfType<BuildManager>();
            if (bm != null)
                bm.OpenGovernmentPanel();

            CloseAllPanels();
            return;
        }

        industrialPanel.SetActive(
            tile.tileData.buildingType == BuildingType.Industrial);

        if (tile.tileData.buildingType == BuildingType.Industrial)
            UpdateIndustrialPanel(tile);

        BuildingLogistics logistics = null;
        if (tile.CurrentBuilding != null)
            logistics = tile.CurrentBuilding
                .GetComponent<BuildingLogistics>();

        logisticsButton.SetActive(logistics != null);

        ShowBuildingInventoryPanel(tile);

        if (currentInventoryUI != null) currentInventoryUI.Hide();
        if (tile.CurrentBuilding != null)
        {
            currentInventoryUI = tile.CurrentBuilding
                .GetComponent<BuildingInventoryUI>();
            if (currentInventoryUI != null)
                currentInventoryUI.Show();
        }
        else
        {
            currentInventoryUI = null;
        }
    }

    void ShowBuildingInventoryPanel(TileObject tile)
    {
        if (buildingInfoPanel == null) return;

        if (tile.CurrentBuilding == null)
        {
            buildingInfoPanel.SetActive(false);
            return;
        }

        buildingInfoPanel.SetActive(true);

        if (buildingTitleText != null)
            buildingTitleText.text = tile.GetBuildingLabel();

        RefreshBuildingInventoryOverlay();
    }

    void RefreshBuildingInventoryOverlay()
    {
        if (buildingInventoryText == null ||
            currentTile == null) return;

        if (currentTile.CurrentBuilding == null)
        {
            buildingInventoryText.text = "";
            return;
        }

        BuildingInventory inv = currentTile.buildingInventory;
        if (inv == null)
        {
            buildingInventoryText.text = "No inventory";
            return;
        }

        int demand = GameManager.Instance
            .GetWorkerCost(currentTile);
        float productivity = GameManager.Instance
            .GetBuildingProductivity(currentTile);
        int productivityPct =
            Mathf.RoundToInt(productivity * 100f);
        int assignedWorkers =
            Mathf.RoundToInt(productivity * demand);

        string colorHex = productivityPct >= 100 ? "00ff00"
            : productivityPct >= 50 ? "ff9900"
            : "ff0000";

        string workerLine = demand > 0
            ? "\n<b>Workforce</b>\n" +
              "  Workers: <color=#" + colorHex + ">" +
              assignedWorkers + " / " + demand + "</color>\n" +
              "  Productivity: <color=#" + colorHex + ">" +
              productivityPct + "%</color>\n"
            : "\n<b>Workforce</b>\n  No workers required\n";

        buildingInventoryText.text =
            BuildingInventoryUI.GetInventorySummary(inv) +
            workerLine;

        RefreshProductionCostUI();
    }

    // =========================================
    // PRODUCTION MODE BUTTONS
    // =========================================
    public void SetProductionCivilian()
    {
        if (currentTile != null)
            currentTile.productionType = ProductionType.Civilian;
        CloseAllPanels();
    }

    public void SetProductionMilitary()
    {
        if (currentTile != null)
            currentTile.productionType = ProductionType.HeavySupplies;
        CloseAllPanels();
    }

    public void SetProductionSupplies()
    {
        if (currentTile != null)
            currentTile.productionType = ProductionType.Supplies;
        CloseAllPanels();
    }

    // =========================================
    // CLOSE ALL PANELS
    // =========================================
    public void CloseAllPanels()
    {
        industrialPanel.SetActive(false);

        if (buildingInfoPanel != null)
            buildingInfoPanel.SetActive(false);

        if (currentInventoryUI != null)
        {
            currentInventoryUI.Hide();
            currentInventoryUI = null;
        }

        currentTile = null;

        if (LogisticsUIManager.Instance != null)
            LogisticsUIManager.Instance.logisticsPanel
                .SetActive(false);

        if (productionCostText != null)
            productionCostText.text = "";

        HideAllCoverageVisuals();

        if (NewsPanelUI.Instance != null) NewsPanelUI.Instance.Close();
        if (HappinessInfoPanel.Instance != null) HappinessInfoPanel.Instance.Close();
        if (FoodInfoPanel.Instance != null) FoodInfoPanel.Instance.Close();
        if (PolicyPanelUI.Instance != null) PolicyPanelUI.Instance.Close();
        if (WorkforceInfoPanel.Instance != null) WorkforceInfoPanel.Instance.Close();
    }

    public void OpenLogisticsPanel()
    {
        if (currentTile == null) return;
        LogisticsUIManager.Instance.OpenPanel(currentTile);
    }

    // =========================================
    // BUILD TOWN HALL BUTTON
    // Called by the locked/unlocked button in
    // the build menu. Activates TownHall build
    // mode in BuildManager.
    // =========================================
    public void OnBuildTownHallClicked()
    {
        if (CityManager.Instance == null ||
            !CityManager.Instance.CanFoundNewCity())
            return;

        BuildManager bm = FindObjectOfType<BuildManager>();
        if (bm != null)
            bm.BeginTownHallPlacement();
    }

    // =========================================
    // INDUSTRIAL PANEL
    // =========================================
    void UpdateIndustrialPanel(TileObject tile)
    {
        if (industrialProductionText == null) return;

        string mode;
        switch (tile.productionType)
        {
            case ProductionType.Civilian:
                mode = "Civilian Goods";
                break;
            case ProductionType.HeavySupplies:
                mode = "Heavy Supplies";
                break;
            case ProductionType.Supplies:
                mode = "Military Supplies";
                break;
            default:
                mode = "Not Set";
                break;
        }

        industrialProductionText.text = "Production Mode: " + mode;
    }

    // =========================================
    // PRODUCTION COST PANEL
    // =========================================
    void RefreshProductionCostUI()
    {
        if (productionCostText == null || currentTile == null)
            return;

        BuildingType type = currentTile.tileData.buildingType;
        productionCostText.text = "";

        switch (type)
        {
            case BuildingType.Industrial:
                switch (currentTile.productionType)
                {
                    case ProductionType.Civilian:
                        productionCostText.text =
                            "<b>Civilian Goods</b>\n" +
                            "Consumes:\n• 2 Wood\n• 1 Iron\n\n" +
                            "Produces:\n• 3 Civilian Goods";
                        break;
                    case ProductionType.HeavySupplies:
                        productionCostText.text =
                            "<b>Heavy Supplies</b>\n" +
                            "Consumes:\n• 3 Iron\n• 2 Wood\n\n" +
                            "Produces:\n• 2 Heavy Supplies";
                        break;
                    case ProductionType.Supplies:
                        productionCostText.text =
                            "<b>Military Supplies</b>\n" +
                            "Consumes:\n• 3 Farming\n• 1 Oil\n\n" +
                            "Produces:\n• 3 Military Supplies";
                        break;
                }
                break;

            case BuildingType.HeavyComplex:
                productionCostText.text =
                    "<b>Truck Production</b>\n" +
                    "Consumes:\n• 8 Heavy Supplies\n\n" +
                    "Produces:\n• 1 Truck";
                break;

            case BuildingType.Commercial:
                productionCostText.text =
                    "<b>Commercial Consumption</b>\n" +
                    "Consumes:\n• Civilian Goods\n\n" +
                    "Produces:\n• Money\n• Happiness";
                break;

            case BuildingType.PowerPlant:
                productionCostText.text =
                    "<b>Power Generation</b>\n" +
                    "Consumes:\n• 2 Oil\n\n" +
                    "Produces:\n• 25 Power";
                break;
        }
    }

    // =========================================
    // TOWN HALL BUTTON
    // Locked until population threshold is met.
    // Shows the next threshold to the player.
    // =========================================
    void RefreshTownHallButton()
    {
        if (buildTownHallButton == null) return;
        if (CityManager.Instance == null ||
            !CityManager.Instance.gameStarted)
        {
            buildTownHallButton.interactable = false;
            return;
        }

        bool canBuild = CityManager.Instance.CanFoundNewCity();
        buildTownHallButton.interactable = canBuild;

        if (townHallUnlockText != null)
        {
            if (canBuild)
            {
                townHallUnlockText.text = "Found new city";
            }
            else
            {
                int needed =
                    CityManager.Instance.GetNextCityThreshold();
                int current =
                    CityManager.Instance.GetTotalPopulation();
                townHallUnlockText.text =
                    "Need " + needed + " pop (" +
                    current + "/" + needed + ")";
            }
        }
    }

    // =========================================
    // COLOR HELPERS
    // =========================================
    Color GetServiceColor(int value)
    {
        if (value >= 70) return Color.green;
        if (value >= 40) return Color.yellow;
        return Color.red;
    }

    Color GetCrimeColor(int value)
    {
        if (value <= 30) return Color.green;
        if (value <= 60) return Color.yellow;
        return Color.red;
    }

    void HideAllCoverageVisuals()
    {
        TileObject[] allTiles =
            FindObjectsOfType<TileObject>();

        foreach (var t in allTiles)
            t.HidePoliceCoverage();
    }
    public void SetBuildModeText(string text, Color color)
    {
        if (buildModeText == null)
            return;

        buildModeText.text = text;
        buildModeText.color = color;
        buildModeText.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }
}