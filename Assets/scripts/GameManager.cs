using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Economy")]
    public int money = 100;

    [Header("Population")]
    public int population = 0;
    public int maxPopulation = 0;

    [Header("Tick System")]
    public float tickInterval = 2f;

    [Header("City")]
    [Range(0, 100)]
    public int happiness = 50;

    [Header("Power")]
    public int powerProduced;
    public int powerConsumed;

    [Header("Power and Resources Feedback")]
    public GameObject noPowerIconPrefab;
    public GameObject noResourcesIconPrefab;
    public GameObject noWorkersIconPrefab;
    private List<GameObject> activeNoResourceIcons = new List<GameObject>();
    private List<GameObject> activePowerIcons = new List<GameObject>();
    private List<GameObject> activeNoWorkersIcons = new List<GameObject>();

    [Header("Global Resource Totals")]
    public int wood;
    public int oil;
    public int farming;
    public int minerals;
    public int iron;
    public int civilianGoods;
    public int heavySupplies;       // renamed from militaryGoods
    public int militarySupplies;
    public int gasoline;
    public int usedTrucks;
    public int trucks;
    public int heavyTrucks;
    public int tanks;

    [Header("Starting Resources")]
    public int startingFreeTrucks = 25;
    public int startingFood = 500;          // set in Inspector — consumed over time
    [HideInInspector] public int foodStock; // runtime depleting stock
    public int startingGas = 1000;
    [HideInInspector] public int gasStock;

    [Header("Workforce")]
    public int totalWorkers;
    public int employedWorkers;
    public int freeWorkers;
    public int logisticsWorkers;

    [Header("Decay Settings")]
    public float populationDecayRate = 5f;
    public float workforceDecayRate = 3f;

    // ─────────────────────────────────────────
    // GOODS SALES TRACKING (for happiness)
    // Accumulates each tick, read by
    // HappinessManager, resets each month.
    // ─────────────────────────────────────────
    [HideInInspector] public int monthlyGoodsSold;
    [HideInInspector] public int monthlyGoodsDemand;

    [Header("City Services")]
    [Range(0, 100)] public int healthcare = 0;
    [Range(0, 100)] public int education = 0;
    [Range(0, 100)] public int crime = 0;

    // ─────────────────────────────────────────
    // UPKEEP
    // ─────────────────────────────────────────
    [Header("Upkeep Costs (per month)")]
    public int policeUpkeep = 50;
    public int hospitalUpkeep = 80;
    public int collegeUpkeep = 100;
    public GameObject noFundsIconPrefab;
    private List<GameObject> activeNoFundsIcons = new List<GameObject>();

    // ─────────────────────────────────────────
    // FOOD & FAMINE
    // ─────────────────────────────────────────
    [Header("Food & Famine")]
    public int foodShortageThreshold = 3;
    public int faminePopDecayPerTick = 2;
    public int famineHappinessPenalty = 15;
    public GameObject famineIconPrefab;  // separate icon for famine on residential tiles
    private List<GameObject> activeFamineIcons = new List<GameObject>();

    [HideInInspector] public int foodShortageCounter = 0;
    [HideInInspector] public bool isFamine = false;

    // ─── GAS SHORTAGE ────────────────────────
    public int gasShortageCounter = 0;
    public int gasShortageThreshold = 3;
    public bool isGasShortage = false;
    [HideInInspector] public int monthlyGasSupplied = 0;
    [HideInInspector] public int monthlyGasDemand = 0;

    private Dictionary<TileObject, int> iconCountPerTile =
        new Dictionary<TileObject, int>();
    private Dictionary<TileObject, float> buildingProductivity =
        new Dictionary<TileObject, float>();

    void Awake() { Instance = this; }

    void Start()
    {
        foodStock = startingFood;
        gasStock = startingGas;

        if (GameClock.Instance != null)
        {
            GameClock.Instance.OnTick += RunEconomyTick;
            GameClock.Instance.OnMonthEnd += RunMonthlyUpkeep;
        }
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
        {
            GameClock.Instance.OnTick -= RunEconomyTick;
            GameClock.Instance.OnMonthEnd -= RunMonthlyUpkeep;
        }
    }

    public void RunEconomyTick()
    {
        // ─────────────────────────────────────
        // CLEAR FEEDBACK ICONS
        // ─────────────────────────────────────
        foreach (var icon in activeNoResourceIcons)
            if (icon != null) Destroy(icon);
        activeNoResourceIcons.Clear();

        foreach (var icon in activePowerIcons)
            if (icon != null) Destroy(icon);
        activePowerIcons.Clear();

        foreach (var icon in activeNoWorkersIcons)
            if (icon != null) Destroy(icon);
        activeNoWorkersIcons.Clear();

        foreach (var icon in activeNoFundsIcons)
            if (icon != null) Destroy(icon);
        activeNoFundsIcons.Clear();

        foreach (var icon in activeFamineIcons)
            if (icon != null) Destroy(icon);
        activeFamineIcons.Clear();

        iconCountPerTile.Clear();
        buildingProductivity.Clear();

        // ─────────────────────────────────────
        // LOGISTICS
        // ─────────────────────────────────────
        if (LogisticsManager.Instance != null)
            LogisticsManager.Instance.RunLogisticsTick();

        if (GridManager.Instance == null) return;
        List<TileObject> allTiles = GridManager.Instance.allTiles;

        int newMaxPop = 0;
        int growth = 0;
        powerProduced = 0;
        powerConsumed = 0;

        // ─────────────────────────────────────
        // WORKFORCE RESET
        // ─────────────────────────────────────
        totalWorkers = Mathf.RoundToInt(population * 0.7f);
        employedWorkers = 0;
        freeWorkers = totalWorkers;
        freeWorkers -= logisticsWorkers;
        if (freeWorkers < 0) freeWorkers = 0;
        employedWorkers += logisticsWorkers;

        // ─────────────────────────────────────
        // PASS 1 — POWER GENERATION
        // ─────────────────────────────────────
        foreach (TileObject tile in allTiles)
        {
            if (tile == null || tile.tileData == null || tile.CurrentBuilding == null)
                continue;
            if (tile.tileData.buildingType != BuildingType.PowerPlant)
                continue;

            BuildingInventory inv = tile.buildingInventory;
            if (inv == null) continue;

            int workerDemand = GetWorkerCost(tile);
            int assignedWorkers = Mathf.Min(freeWorkers, workerDemand);
            freeWorkers -= assignedWorkers;
            employedWorkers += assignedWorkers;

            float productivity = workerDemand > 0
                ? (float)assignedWorkers / workerDemand
                : 1f;

            if (productivity <= 0f) continue;
            buildingProductivity[tile] = productivity;

            int oilNeeded = Mathf.RoundToInt(2 * productivity);
            if (oilNeeded <= 0) continue;
            if (inv.GetResource(ResourceType.Oil) < oilNeeded) continue;

            inv.RemoveResource(ResourceType.Oil, oilNeeded);
            powerProduced += Mathf.RoundToInt(25 * productivity);
        }

        // ─────────────────────────────────────
        // PASS 2 — POWER DEMAND
        // ─────────────────────────────────────
        int totalDemand = 0;
        foreach (TileObject tile in allTiles)
        {
            if (tile == null || tile.tileData == null) continue;
            switch (tile.tileData.buildingType)
            {
                case BuildingType.Residential: totalDemand += 1; break;
                case BuildingType.Industrial: totalDemand += 5; break;
                case BuildingType.Farm: totalDemand += 3; break;
                case BuildingType.Mine: totalDemand += 3; break;
                case BuildingType.LumberYard: totalDemand += 3; break;
                case BuildingType.IronMine: totalDemand += 3; break;
                case BuildingType.Commercial: totalDemand += 2; break;
                case BuildingType.HeavyComplex: totalDemand += 10; break;
                case BuildingType.GeneralStorage: totalDemand += 1; break;
                case BuildingType.IndustrialStorage: totalDemand += 2; break;
                case BuildingType.HardwareStorage: totalDemand += 3; break;
                case BuildingType.PoliceStation: totalDemand += 4; break;
                case BuildingType.Hospital: totalDemand += 8; break;
                case BuildingType.CollegeCampus: totalDemand += 10; break;
                case BuildingType.OilRefinery:
                    totalDemand += 4;
                    break;
            }
        }
        powerConsumed = totalDemand;

        // ─────────────────────────────────────
        // SORT BY PRIORITY
        // ─────────────────────────────────────
        List<TileObject> sortedTiles = new List<TileObject>(allTiles);
        sortedTiles.Sort((a, b) => GetPriority(a).CompareTo(GetPriority(b)));

        // ─────────────────────────────────────
        // PASS 3 — BUILDING LOGIC
        // ─────────────────────────────────────
        int remainingPower = powerProduced;

        foreach (TileObject tile in sortedTiles)
        {
            if (tile == null || tile.tileData == null || tile.CurrentBuilding == null)
                continue;

            // PowerPlants handled in Pass 1 — skip here
            if (tile.tileData.buildingType == BuildingType.PowerPlant)
                continue;

            BuildingInventory inv = tile.buildingInventory;

            int powerCost = GetPowerCost(tile);
            bool hasPower = false;
            if (remainingPower >= powerCost)
            {
                hasPower = true;
                remainingPower -= powerCost;
            }

            int workerDemand = GetWorkerCost(tile);
            int assignedWorkers = Mathf.Min(freeWorkers, workerDemand);
            freeWorkers -= assignedWorkers;
            employedWorkers += assignedWorkers;

            float productivity = workerDemand > 0
                ? (float)assignedWorkers / workerDemand
                : 1f;
            buildingProductivity[tile] = productivity;

            bool hasEnoughWorkers = assignedWorkers >= workerDemand;
            if (tile.HasBuilding() && workerDemand > 0 && !hasEnoughWorkers)
                SpawnNoWorkersIcon(tile);

            // Unfunded service building warning
            if (!tile.isFunded)
                SpawnNoFundsIcon(tile);

            if (!hasPower)
                SpawnNoPowerIcon(tile);

            bool hasInputs = HasProductionInputs(tile);
            if (hasPower && !hasInputs)
                SpawnNoResourcesIcon(tile);

            switch (tile.tileData.buildingType)
            {
                case BuildingType.Residential:
                    newMaxPop += 25;
                    growth += 1;
                    break;

                case BuildingType.Industrial:
                    // STRIKE — block all industrial production
                    if (HappinessManager.Instance != null &&
                        HappinessManager.Instance.strikeActive)
                        continue;
                    if (!hasPower || productivity <= 0f || inv == null) continue;
                    // Apply productivity modifier from happiness/crime/education
                    float indMod = HappinessManager.Instance != null
                        ? HappinessManager.Instance.GetProductivityModifier() : 1f;
                    // Apply random event production modifier (trade boom, labor dispute)
                    float eventProdMod = RandomEventManager.Instance != null
                        ? RandomEventManager.Instance.ProductionModifier : 1f;
                    productivity = Mathf.Clamp(productivity * indMod * eventProdMod, 0f, 1f);
                    switch (tile.productionType)
                    {
                        case ProductionType.Civilian:
                            int cWood = Mathf.RoundToInt(2 * productivity);
                            int cIron = Mathf.RoundToInt(1 * productivity);
                            int cOut = Mathf.RoundToInt(3 * productivity);
                            if (cOut <= 0) break;
                            if (inv.GetResource(ResourceType.Wood) >= cWood &&
                                inv.GetResource(ResourceType.Iron) >= cIron)
                            {
                                inv.RemoveResource(ResourceType.Wood, cWood);
                                inv.RemoveResource(ResourceType.Iron, cIron);
                                inv.AddResource(ResourceType.CivilianGoods, cOut);
                            }
                            break;

                        case ProductionType.HeavySupplies:
                            // Iron + Wood → Heavy Supplies
                            int mIron = Mathf.RoundToInt(3 * productivity);
                            int mWood = Mathf.RoundToInt(2 * productivity);
                            int mOut = Mathf.RoundToInt(2 * productivity);
                            if (mOut <= 0) break;
                            if (inv.GetResource(ResourceType.Iron) >= mIron &&
                                inv.GetResource(ResourceType.Wood) >= mWood)
                            {
                                inv.RemoveResource(ResourceType.Iron, mIron);
                                inv.RemoveResource(ResourceType.Wood, mWood);
                                inv.AddResource(ResourceType.HeavySupplies, mOut);
                            }
                            break;

                        case ProductionType.Supplies:
                            int sFood = Mathf.RoundToInt(3 * productivity);
                            int sOil = Mathf.RoundToInt(1 * productivity);
                            int sOut = Mathf.RoundToInt(3 * productivity);
                            if (sOut <= 0) break;
                            if (inv.GetResource(ResourceType.Farming) >= sFood &&
                                inv.GetResource(ResourceType.Oil) >= sOil)
                            {
                                inv.RemoveResource(ResourceType.Farming, sFood);
                                inv.RemoveResource(ResourceType.Oil, sOil);
                                inv.AddResource(ResourceType.MilitarySupplies, sOut);
                            }
                            break;
                    }
                    break;

                case BuildingType.HeavyComplex:

                    if (!hasPower || productivity <= 0f || inv == null)
                        continue;

                    int heavyNeeded = Mathf.RoundToInt(8 * productivity);
                    int truckOut = Mathf.RoundToInt(1 * productivity);

                    if (truckOut <= 0)
                        break;

                    if (inv.GetResource(ResourceType.HeavySupplies) >= heavyNeeded)
                    {
                        inv.RemoveResource(ResourceType.HeavySupplies, heavyNeeded);

                        inv.AddResource(ResourceType.Trucks, truckOut);
                    }

                    break;

                case BuildingType.Farm:
                    if (!hasPower || productivity <= 0f || inv == null) continue;
                    float farmEventMod = RandomEventManager.Instance != null
                        ? RandomEventManager.Instance.FarmingModifier : 1f;
                    int farmOut = Mathf.RoundToInt(5 * productivity * farmEventMod);
                    if (farmOut <= 0) break;
                    if (tile.tileData.resourceAmount > 0)
                    {
                        int actual = Mathf.Min(farmOut, tile.tileData.resourceAmount);
                        if (inv.AddResource(ResourceType.Farming, actual))
                            tile.tileData.resourceAmount -= actual;
                    }
                    break;

                case BuildingType.OilExtractor:
                    if (productivity <= 0f || inv == null) continue;
                    int oilOut = Mathf.RoundToInt(3 * productivity);
                    if (oilOut <= 0) break;
                    if (tile.tileData.resourceAmount > 0)
                    {
                        int actual = Mathf.Min(oilOut, tile.tileData.resourceAmount);
                        if (inv.AddResource(ResourceType.Oil, actual))
                            tile.tileData.resourceAmount -= actual;
                    }
                    break;

                case BuildingType.Mine:
                    if (!hasPower || productivity <= 0f || inv == null) continue;
                    int minOut = Mathf.RoundToInt(4 * productivity);
                    if (minOut <= 0) break;
                    if (tile.tileData.resourceAmount > 0)
                    {
                        int actual = Mathf.Min(minOut, tile.tileData.resourceAmount);
                        if (inv.AddResource(ResourceType.Minerals, actual))
                            tile.tileData.resourceAmount -= actual;
                    }
                    break;

                case BuildingType.LumberYard:
                    if (!hasPower || productivity <= 0f || inv == null) continue;
                    int woodOut = Mathf.RoundToInt(6 * productivity);
                    if (woodOut <= 0) break;
                    if (tile.tileData.resourceAmount > 0)
                    {
                        int actual = Mathf.Min(woodOut, tile.tileData.resourceAmount);
                        if (inv.AddResource(ResourceType.Wood, actual))
                            tile.tileData.resourceAmount -= actual;
                    }
                    break;

                case BuildingType.IronMine:
                    if (!hasPower || productivity <= 0f || inv == null) continue;
                    int ironOut = Mathf.RoundToInt(4 * productivity);
                    if (ironOut <= 0) break;
                    if (tile.tileData.resourceAmount > 0)
                    {
                        int actual = Mathf.Min(ironOut, tile.tileData.resourceAmount);
                        if (inv.AddResource(ResourceType.Iron, actual))
                            tile.tileData.resourceAmount -= actual;
                    }
                    break;

                case BuildingType.Commercial:
                    if (!hasPower || productivity <= 0f || inv == null) continue;
                    float comMod = HappinessManager.Instance != null
                        ? HappinessManager.Instance.GetProductivityModifier() : 1f;
                    productivity = Mathf.Clamp(productivity * comMod, 0f, 1f);
                    if (inv.GetResource(ResourceType.CivilianGoods) > 0)
                    {
                        int demand = Mathf.Max(1, population / 10);
                        int maxSale = Mathf.RoundToInt(5 * productivity);
                        if (maxSale <= 0) break;
                        int sold = Mathf.Min(
                            inv.GetResource(ResourceType.CivilianGoods),
                            demand, maxSale);
                        inv.RemoveResource(ResourceType.CivilianGoods, sold);

                        // Track for monthly happiness calculation
                        monthlyGoodsSold += sold;
                        monthlyGoodsDemand += demand;

                        float ecoMod = HappinessManager.Instance != null
                            ? HappinessManager.Instance.GetEconomicModifier()
                            : 1f;
                        money += Mathf.RoundToInt(sold * 4 * ecoMod);
                    }
                    break;
                case BuildingType.OilRefinery:
                    if (!hasPower || productivity <= 0f || inv == null)
                        continue;

                    int rOil = Mathf.RoundToInt(3 * productivity);
                    int rOut = Mathf.RoundToInt(5 * productivity);

                    if (rOut <= 0)
                        break;

                    if (inv.GetResource(ResourceType.Oil) >= rOil)
                    {
                        inv.RemoveResource(ResourceType.Oil, rOil);
                        inv.AddResource(ResourceType.Gasoline, rOut);
                    }
                    break;
            }
        }

        // ─────────────────────────────────────
        // POPULATION
        // ─────────────────────────────────────
        maxPopulation = newMaxPop;

        bool strikeBlocking = HappinessManager.Instance != null &&
                              HappinessManager.Instance.strikeActive;
        bool flightActive = HappinessManager.Instance != null &&
                              HappinessManager.Instance.populationFlight;

        // No growth during famine, strike, or population flight
        if (!isFamine && !strikeBlocking && !flightActive &&
            population < maxPopulation)
        {
            int finalGrowth = growth;
            if (HappinessManager.Instance != null)
                finalGrowth = Mathf.RoundToInt(
                    finalGrowth * HappinessManager.Instance.GetPopulationGrowthModifier());
            population += finalGrowth;
            if (population > maxPopulation)
                population = maxPopulation;
        }

        // ─────────────────────────────────────
        // EPIDEMIC RISK
        // Low healthcare + large population =
        // random small pop loss each tick.
        // ─────────────────────────────────────
        if (healthcare < 20 && population > 300)
        {
            if (Random.value < 0.05f) // 5% chance per tick
            {
                int sick = Mathf.Max(1, Mathf.RoundToInt(population * 0.003f));
                population = Mathf.Max(0, population - sick);
                NewsManager.Post(NewsCategory.Health,
                    "Disease Outbreak! Low healthcare is costing lives.");
            }
        }

        // CityManager is the single source of truth for city services and happiness.
        // Run it before legacy consequence systems so they read the latest capital values.
        if (CityManager.Instance != null && CityManager.Instance.gameStarted)
            CityManager.Instance.RunCityTick();

        if (HappinessManager.Instance != null)
            HappinessManager.Instance.RunHappinessTick();

        // Global crime remains responsible for district coverage, national crime events and theft.
        if (CrimeManager.Instance != null)
            CrimeManager.Instance.RunCrimeTick();

        // Must run BEFORE RunFoodTick so farming total is current
        UpdateGlobalResourceTotals();

        // Food & famine — actually removes Farming from inventories
        RunFoodTick();

        // Refresh farming display after consumption so UI is accurate
        farming = 0;
        foreach (var tile in allTiles)
        {
            if (tile == null || tile.CurrentBuilding == null) continue;
            BuildingInventory inv = tile.buildingInventory;
            if (inv != null)
                farming += inv.GetResource(ResourceType.Farming);
        }
        farming += foodStock;
        gasoline += gasStock;

        ApplyPopulationAndWorkforceDecay();
    }

    // ─────────────────────────────────────────
    // GLOBAL RESOURCE TOTALS
    // Iterates allTiles instead of FindObjectsOfType<BuildingInventory>
    // ─────────────────────────────────────────
    public void UpdateGlobalResourceTotals()
    {
        wood = oil = farming = minerals = iron = 0;
        civilianGoods = heavySupplies = militarySupplies = gasoline = 0;
        trucks = heavyTrucks = tanks = usedTrucks = 0;
        logisticsWorkers = 0;

        List<TileObject> allTiles = GridManager.Instance.allTiles;

        foreach (var tile in allTiles)
        {
            if (tile == null || tile.CurrentBuilding == null) continue;

            BuildingInventory inv = tile.buildingInventory;
            if (inv != null)
            {
                wood += inv.GetResource(ResourceType.Wood);
                oil += inv.GetResource(ResourceType.Oil);
                farming += inv.GetResource(ResourceType.Farming);
                minerals += inv.GetResource(ResourceType.Minerals);
                iron += inv.GetResource(ResourceType.Iron);
                civilianGoods += inv.GetResource(ResourceType.CivilianGoods);
                heavySupplies += inv.GetResource(ResourceType.HeavySupplies);
                militarySupplies += inv.GetResource(ResourceType.MilitarySupplies);
                gasoline += inv.GetResource(ResourceType.Gasoline);
                trucks += inv.GetResource(ResourceType.Trucks);
                heavyTrucks += inv.GetResource(ResourceType.HeavyTrucks);
                tanks += inv.GetResource(ResourceType.Tanks);
            }

            BuildingLogistics logistics =
                tile.CurrentBuilding.GetComponent<BuildingLogistics>();
            if (logistics != null)
            {
                foreach (var route in logistics.routes)
                {
                    if (route.active)
                    {
                        usedTrucks += route.assignedTrucks;
                        logisticsWorkers += route.assignedTrucks;
                    }
                }
            }
        }

        trucks += startingFreeTrucks;
    }

    // ─────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────
    public bool SpendMoney(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        return true;
    }

    public int freeTrucks => Mathf.Max(0, trucks - usedTrucks);

    public int computedFreeWorkers
    {
        get
        {
            int total = Mathf.RoundToInt(population * 0.7f);
            // employedWorkers already includes logisticsWorkers —
            // don't subtract it twice
            return Mathf.Max(0, total - employedWorkers);
        }
    }

    public float GetBuildingProductivity(TileObject tile)
    {
        return buildingProductivity.TryGetValue(tile, out float p) ? p : 0f;
    }

    // Returns the worker ratio for a storage/logistics building.
    // LogisticsManager reads this to slow truck dispatch when understaffed.
    // 0 = no workers at all (no trucks dispatch)
    // 0.5 = half staffed (trucks dispatch half as often)
    // 1.0 = fully staffed (normal operation)
    public float GetLogisticsWorkRatio(TileObject tile)
    {
        return GetBuildingProductivity(tile);
    }

    public void ApplyPopulationAndWorkforceDecay()
    {
        if (population > maxPopulation)
        {
            population -= Mathf.CeilToInt(populationDecayRate);
            if (population < maxPopulation)
                population = maxPopulation;
        }

        int idealWorkers = Mathf.RoundToInt(population * 0.7f);
        totalWorkers = idealWorkers;

        if (employedWorkers > totalWorkers)
        {
            int excess = employedWorkers - totalWorkers;
            int reduce = Mathf.Min(excess, Mathf.CeilToInt(workforceDecayRate));
            employedWorkers -= reduce;
        }

        freeWorkers = totalWorkers - employedWorkers - logisticsWorkers;
        if (freeWorkers < 0) freeWorkers = 0;
    }

    public bool HasProductionInputs(TileObject tile)
    {
        if (tile == null || tile.buildingInventory == null) return true;
        BuildingInventory inv = tile.buildingInventory;
        switch (tile.tileData.buildingType)
        {
            case BuildingType.Industrial:
                switch (tile.productionType)
                {
                    case ProductionType.Civilian:
                        return inv.GetResource(ResourceType.Wood) >= 2 &&
                               inv.GetResource(ResourceType.Iron) >= 1;
                    case ProductionType.HeavySupplies:
                        return inv.GetResource(ResourceType.Iron) >= 3 &&
                               inv.GetResource(ResourceType.Wood) >= 2;
                    case ProductionType.Supplies:
                        return inv.GetResource(ResourceType.Farming) >= 3 &&
                               inv.GetResource(ResourceType.Oil) >= 1;
                }
                break;
            case BuildingType.HeavyComplex:
                return inv.GetResource(ResourceType.HeavySupplies) >= 8;
            case BuildingType.Commercial:
                return inv.GetResource(ResourceType.CivilianGoods) > 0;
            case BuildingType.OilRefinery:
                return inv.GetResource(ResourceType.Oil) >= 3;
        }
        return true;
       
        }

    public int GetWorkerCost(TileObject tile)
    {
        switch (tile.tileData.buildingType)
        {
            case BuildingType.Farm: return 5;
            case BuildingType.LumberYard: return 6;
            case BuildingType.Mine: return 8;
            case BuildingType.IronMine: return 10;
            case BuildingType.Industrial: return 20;
            case BuildingType.HeavyComplex: return 40;
            case BuildingType.PowerPlant: return 15;
            case BuildingType.Commercial: return 10;
            case BuildingType.GeneralStorage: return 2;
            case BuildingType.OilStorage: return 2;
            case BuildingType.IndustrialStorage: return 4;
            case BuildingType.HardwareStorage: return 5;
            case BuildingType.PoliceStation: return 15;
            case BuildingType.Hospital: return 25;
            case BuildingType.CollegeCampus: return 30;
            case BuildingType.OilRefinery:
                return 4;
        }
        return 0;
    }

    int GetPowerCost(TileObject tile)
    {
        switch (tile.tileData.buildingType)
        {
            case BuildingType.Residential: return 1;
            case BuildingType.Industrial: return 5;
            case BuildingType.Farm: return 3;
            case BuildingType.Mine: return 3;
            case BuildingType.LumberYard: return 3;
            case BuildingType.IronMine: return 3;
            case BuildingType.Commercial: return 2;
            case BuildingType.HeavyComplex: return 10;
            case BuildingType.GeneralStorage: return 1;
            case BuildingType.IndustrialStorage: return 2;
            case BuildingType.HardwareStorage: return 3;
            case BuildingType.PoliceStation: return 4;
            case BuildingType.Hospital: return 8;
            case BuildingType.CollegeCampus: return 10;
            case BuildingType.OilRefinery:
                return 4;
        }
        return 0;
    }

    int GetPriority(TileObject tile)
    {
        // Lower number = higher priority = gets workers first.
        // Everything depends on power, so power plants first.
        // Essential services next, then food, then production.
        switch (tile.tileData.buildingType)
        {
            case BuildingType.PowerPlant: return 0;
            case BuildingType.Hospital: return 1;
            case BuildingType.PoliceStation: return 1;
            case BuildingType.Farm: return 2;
            case BuildingType.Residential: return 3;
            case BuildingType.OilExtractor: return 4;
            case BuildingType.Mine: return 4;
            case BuildingType.LumberYard: return 4;
            case BuildingType.IronMine: return 4;
            case BuildingType.Industrial: return 5;
            case BuildingType.HeavyComplex: return 5;
            case BuildingType.CollegeCampus: return 5;
            case BuildingType.Commercial: return 6;
            case BuildingType.GeneralStorage: return 7;
            case BuildingType.OilStorage: return 7;
            case BuildingType.IndustrialStorage: return 7;
            case BuildingType.HardwareStorage: return 7;
            case BuildingType.OilRefinery:
                return 5;
            default: return 8;
        }
    }

    void SpawnNoPowerIcon(TileObject tile)
    {
        if (noPowerIconPrefab == null) return;
        GameObject icon = Instantiate(noPowerIconPrefab);
        icon.transform.position = GetNextIconPosition(tile);
        activePowerIcons.Add(icon);
    }

    void SpawnNoFundsIcon(TileObject tile)
    {
        if (noFundsIconPrefab == null) return;
        GameObject icon = Instantiate(noFundsIconPrefab);
        icon.transform.position = GetNextIconPosition(tile);
        activeNoFundsIcons.Add(icon);
    }

    void SpawnFamineIcon(TileObject tile)
    {
        if (famineIconPrefab == null) return;
        GameObject icon = Instantiate(famineIconPrefab);
        icon.transform.position = GetNextIconPosition(tile);
        activeFamineIcons.Add(icon);
    }

    // ─────────────────────────────────────────
    // MONTHLY UPKEEP
    // Fires on GameClock.OnMonthEnd.
    // Priority order: Police → Hospital → College
    // Grace period: 1 month warning before going
    // unfunded so the player has time to react.
    // ─────────────────────────────────────────
    public void RunMonthlyUpkeep()
    {
        // Reset monthly goods tracking for next month
        monthlyGoodsSold = 0;
        monthlyGoodsDemand = 0;

        List<TileObject> allTiles = GridManager.Instance.allTiles;

        // Collect service buildings in priority order
        var policeStations = new System.Collections.Generic.List<TileObject>();
        var hospitals = new System.Collections.Generic.List<TileObject>();
        var colleges = new System.Collections.Generic.List<TileObject>();

        foreach (var tile in allTiles)
        {
            if (tile == null || tile.tileData == null || tile.CurrentBuilding == null)
                continue;
            switch (tile.tileData.buildingType)
            {
                case BuildingType.PoliceStation: policeStations.Add(tile); break;
                case BuildingType.Hospital: hospitals.Add(tile); break;
                case BuildingType.CollegeCampus: colleges.Add(tile); break;
            }
        }

        // Pay in priority order — police first, colleges last
        PayUpkeepGroup(policeStations, policeUpkeep);
        PayUpkeepGroup(hospitals, hospitalUpkeep);
        PayUpkeepGroup(colleges, collegeUpkeep);

        // ── RESET MONTHLY GAS TRACKING ───────────────
        monthlyGasSupplied = 0;
        monthlyGasDemand = 0;

        // ── GAS UPKEEP FOR TRUCKS ────────────────────
        int gasCost = usedTrucks;
        if (gasCost > 0)
        {
            int gasRemaining = gasCost;
            foreach (var tile in allTiles)
            {
                if (gasRemaining <= 0) break;
                if (tile == null || tile.tileData == null) continue;
                if (tile.tileData.buildingType != BuildingType.OilStorage) continue;
                BuildingInventory inv = tile.buildingInventory;
                if (inv == null) continue;
                int have = inv.GetResource(ResourceType.Gasoline);
                int consume = Mathf.Min(gasRemaining, have);
                if (consume > 0) { inv.RemoveResource(ResourceType.Gasoline, consume); gasRemaining -= consume; }
            }
            if (gasRemaining > 0 && gasStock > 0)
            {
                int drawn = Mathf.Min(gasRemaining, gasStock);
                gasStock -= drawn; gasRemaining -= drawn;
            }
            if (gasRemaining > 0)
            {
                gasShortageCounter++;
                if (gasShortageCounter >= gasShortageThreshold && !isGasShortage)
                {
                    isGasShortage = true;
                    NewsManager.Post(NewsCategory.Social, "Gas shortage! Trucks slowing down. Build more Oil Refineries.");
                }
            }
            else if (isGasShortage)
            {
                gasShortageCounter = 0; isGasShortage = false;
                NewsManager.Post(NewsCategory.Social, "Gas supply restored. Trucks running at full speed.");
            }
            else { gasShortageCounter = 0; }
        }

        // ── GAS STATION CONSUMPTION ──────────────────
        foreach (var tile in allTiles)
        {
            if (tile == null || tile.tileData == null) continue;
            if (tile.tileData.buildingType != BuildingType.GasStation) continue;
            if (tile.CurrentBuilding == null) continue;
            BuildingInventory inv = tile.buildingInventory;
            if (inv == null) continue;
            int gasNeeded = 16;
            monthlyGasDemand += gasNeeded;
            int available = inv.GetResource(ResourceType.Gasoline);
            int consumed = Mathf.Min(gasNeeded, available);
            if (gasNeeded - consumed > 0 && gasStock > 0)
            {
                int drawn = Mathf.Min(gasNeeded - consumed, gasStock);
                gasStock -= drawn; consumed += drawn;
            }
            if (consumed > 0 && available > 0)
                inv.RemoveResource(ResourceType.Gasoline, Mathf.Min(consumed, available));
            monthlyGasSupplied += consumed;
        }

        // Happiness bonus for thriving economy
        if (HappinessManager.Instance != null)
        {
            int bonus = HappinessManager.Instance.GetMonthlyHappinessBonus();
            if (bonus > 0)
            {
                money += bonus;
                NewsManager.Post(NewsCategory.Economic,
                    $"Thriving Economy! Monthly happiness bonus: +${bonus}.");
            }
        }
    }

    void PayUpkeepGroup(System.Collections.Generic.List<TileObject> tiles, int cost)
    {
        foreach (var tile in tiles)
        {
            if (money >= cost)
            {
                // Can afford — pay and mark funded
                money -= cost;
                tile.isFunded = true;
                tile.gracePeriodMonths = 0;
            }
            else
            {
                // Can't afford
                if (tile.gracePeriodMonths > 0)
                {
                    // Grace period used up — go unfunded
                    tile.gracePeriodMonths--;
                    tile.isFunded = false;
                }
                else if (tile.isFunded)
                {
                    // First miss — start grace period, still funded this month
                    tile.gracePeriodMonths = 1;
                    tile.isFunded = true;
                }
                else
                {
                    // Already unfunded
                    tile.isFunded = false;
                }
            }
        }
    }

    // ─────────────────────────────────────────
    // FOOD CONSUMPTION & FAMINE
    // Called every tick from RunEconomyTick.
    // Each residential tile needs 1 food/tick.
    // After foodShortageThreshold consecutive
    // shortage ticks, famine begins.
    // ─────────────────────────────────────────
    void RunFoodTick()
    {
        if (population <= 0)
        {
            foodShortageCounter = 0;
            isFamine = false;
            return;
        }

        // ── DEMAND ───────────────────────────
        // 1 food per residential building per tick
        int residentialCount = 0;
        List<TileObject> residentials = new List<TileObject>();
        foreach (var tile in GridManager.Instance.allTiles)
        {
            if (tile == null || tile.tileData == null) continue;
            if (tile.tileData.buildingType == BuildingType.Residential)
            {
                residentialCount++;
                residentials.Add(tile);
            }
        }

        int remaining = residentialCount; // food still needed this tick

        // ── CONSUME FROM INVENTORIES ─────────
        // Pull Farming resources directly from
        // building inventories (farms + storages).
        // This is what actually depletes the stock
        // so the global number goes down visibly.
        if (remaining > 0)
        {
            foreach (var tile in GridManager.Instance.allTiles)
            {
                if (remaining <= 0) break;
                if (tile == null || tile.CurrentBuilding == null) continue;

                BuildingInventory inv = tile.buildingInventory;
                if (inv == null) continue;

                int available = inv.GetResource(ResourceType.Farming);
                if (available <= 0) continue;

                int consume = Mathf.Min(available, remaining);
                inv.RemoveResource(ResourceType.Farming, consume);
                remaining -= consume;
            }
        }

        // ── CONSUME FROM STARTING STOCK ──────
        // Starting food reserve covers any gap
        // that inventories couldn't fill.
        if (remaining > 0 && foodStock > 0)
        {
            int drawn = Mathf.Min(remaining, foodStock);
            foodStock -= drawn;
            remaining -= drawn;
        }

        // ── SHORTAGE / FAMINE ────────────────
        if (remaining <= 0)
        {
            if (isFamine)
            {
                string cn = CityManager.Instance?.Capital?.cityName ?? "your city";
                NewsManager.Post(NewsCategory.Social,
                    $"{cn}: Famine over. Food supply has recovered.");
            }
            foodShortageCounter = 0;
            isFamine = false;
        }
        else
        {
            foodShortageCounter++;

            if (foodShortageCounter >= foodShortageThreshold)
            {
                string cn = CityManager.Instance?.Capital?.cityName ?? "your city";

                if (!isFamine)
                    NewsManager.Post(NewsCategory.Social,
                        $"{cn}: FAMINE! Food supply has collapsed. Population is dying.");

                isFamine = true;

                if (famineIconPrefab != null)
                    foreach (var tile in residentials)
                        SpawnFamineIcon(tile);

                int deaths = Mathf.Clamp(
                    Mathf.RoundToInt(population * 0.005f),
                    1, faminePopDecayPerTick);
                population = Mathf.Max(0, population - deaths);
                maxPopulation = Mathf.Max(0, maxPopulation - deaths);

                happiness = Mathf.Clamp(happiness - famineHappinessPenalty, 0, 100);
                if (HappinessManager.Instance != null)
                    HappinessManager.Instance.happiness = happiness;
            }
            else if (foodShortageCounter == 1)
            {
                string cn = CityManager.Instance?.Capital?.cityName ?? "your city";
                NewsManager.Post(NewsCategory.Social,
                    $"{cn}: Food shortage! Famine in {foodShortageThreshold - 1} more ticks if supply doesn't recover.");
            }
        }
    }

    void SpawnNoResourcesIcon(TileObject tile)
    {
        if (noResourcesIconPrefab == null) return;
        GameObject icon = Instantiate(noResourcesIconPrefab);
        icon.transform.position = GetNextIconPosition(tile);
        activeNoResourceIcons.Add(icon);
    }

    void SpawnNoWorkersIcon(TileObject tile)
    {
        if (noWorkersIconPrefab == null) return;
        GameObject icon = Instantiate(noWorkersIconPrefab);
        icon.transform.position = GetNextIconPosition(tile);
        activeNoWorkersIcons.Add(icon);
    }

    Vector3 GetNextIconPosition(TileObject tile)
    {
        if (!iconCountPerTile.ContainsKey(tile))
            iconCountPerTile[tile] = 0;
        int index = iconCountPerTile[tile]++;
        return tile.transform.position + Vector3.up * 2f + Vector3.right * (index * 0.5f);
    }
}