using UnityEngine;
using System.Collections.Generic;

// =============================================
// CITY MANAGER
// Singleton. Owns all CityData instances.
// Drives per-city service ticks, border
// expansion, and city founding flow.
// =============================================
public class CityManager : MonoBehaviour
{
    public static CityManager Instance;

    // ──────────────────────────────────────
    // CITY LIST
    // ──────────────────────────────────────
    public List<CityData> cities = new List<CityData>();

    // Convenience accessor for the capital
    public CityData Capital =>
        cities.Count > 0 ? cities[0] : null;

    // ──────────────────────────────────────
    // GLOBAL GOVERNMENT STATE
    // Read from capital city.
    // ──────────────────────────────────────
    public GovernmentType GovernmentType =>
        Capital != null
            ? Capital.governmentType
            : GovernmentType.Democracy;

    public string LeaderName =>
        Capital != null ? Capital.leaderName : "";

    public GovernmentConfig Config =>
        GovernmentConfig.Get(GovernmentType);

    // ──────────────────────────────────────
    // GAME START STATE
    // GameStartUI drives this via SetupData().
    // ──────────────────────────────────────
    public bool gameStarted { get; private set; } = false;

    // Stored during setup, applied when first
    // TownHall is placed.
    // Country name — set once at game start,
    // displayed in GovernmentPanel and HUD
    public string countryName { get; private set; } = "";

    private GovernmentType pendingGovernment;
    private string pendingLeaderName;
    private string pendingCapitalName;
    private string pendingCountryName;

    // ──────────────────────────────────────
    // NEW-CITY THRESHOLD
    // pop required to found 2nd city = 500,
    // 3rd = 1000, 4th = 2000, etc.
    // ──────────────────────────────────────
    // ──────────────────────────────────────
    // CITY FOUNDING THRESHOLDS
    // Index N = total country pop required to
    // found city number N+2 (so index 0 = city 2).
    //
    //  City 2:  500    (+500)
    //  City 3:  1250   (+750)
    //  City 4:  2200   (+950)
    //  City 5:  3500   (+1300)
    //  City 6:  5200   (+1700)
    //  City 7:  7500   (+2300)
    //  City 8:  10500  (+3000) — soft cap
    //
    // Beyond index 7 we add 4000 per extra city.
    // ──────────────────────────────────────
    private static readonly int[] cityPopThresholds =
        new int[] { 500, 1250, 2200, 3500, 5200, 7500, 10500 };

    private int nextCityPopThreshold = 500;

    // Tracks previous happiness per city to detect threshold crossings
    private Dictionary<int, int> prevCityHappiness = new Dictionary<int, int>();

    // ──────────────────────────────────────
    // GLOBAL TAX RATE
    // One rate for whole country, capped by
    // GovernmentConfig.maxTaxRate.
    // UIManager +/- buttons call GlobalTaxRate.
    // ──────────────────────────────────────
    [Range(0, 60)]
    public int globalTaxRate = 10;

    void Awake()
    {
        Instance = this;
    }

    // =============================================
    // SETUP — called by GameStartUI before the
    // player places the first TownHall
    // =============================================
    public void SetupData(
        GovernmentType gov,
        string leader,
        string capitalName,
        string country)
    {
        pendingGovernment = gov;
        pendingLeaderName = leader;
        pendingCapitalName = capitalName;
        pendingCountryName = country;
    }

    // =============================================
    // FOUND CITY — called by TileObject when a
    // TownHall is placed
    // =============================================
    public CityData FoundCity(Vector2Int townHallPos)
    {
        int id = cities.Count;
        bool isCapital = (id == 0);

        string name = isCapital
            ? pendingCapitalName
            : $"City {id + 1}";   // non-capital name
                                  // (future: prompt player)

        CityData city = new CityData(id, name, townHallPos, isCapital);

        if (isCapital)
        {
            city.governmentType = pendingGovernment;
            city.leaderName = pendingLeaderName;
            countryName = pendingCountryName;
            gameStarted = true;
        }

        cities.Add(city);

        // Stamp ownership on all tiles in range
        RefreshCityBorderTiles(city);

        // Advance to the next threshold after founding
        // a non-capital city.
        // Uses the hardcoded list first, then adds 1500
        // for each city beyond the list.
        if (!isCapital)
        {
            int nextIndex = cities.Count - 1;
            if (nextIndex < cityPopThresholds.Length)
                nextCityPopThreshold =
                    cityPopThresholds[nextIndex];
            else
                nextCityPopThreshold += 4000;
        }

        Debug.Log($"[CityManager] Founded city '{city.cityName}'" +
                  $" at {townHallPos}" +
                  $" (capital: {isCapital})");

        return city;
    }

    // =============================================
    // CAN FOUND NEW CITY?
    // Returns true when total country pop meets
    // the next city threshold.
    // =============================================
    public bool CanFoundNewCity()
    {
        if (cities.Count == 0) return true; // always allow first

        return GetTotalPopulation() >= nextCityPopThreshold;
    }

    // =============================================
    // NEXT CITY THRESHOLD (for UI display)
    // =============================================
    public int GetNextCityThreshold()
    {
        return nextCityPopThreshold;
    }

    // =============================================
    // TOTAL POPULATION ACROSS ALL CITIES
    // =============================================
    public int GetTotalPopulation()
    {
        // Population is still a single global value
        // managed by GameManager's gradual growth.
        // City populations are proportional shares
        // of this global number, so we return the
        // source of truth directly to avoid summing
        // and double-counting.
        return GameManager.Instance != null
            ? GameManager.Instance.population
            : 0;
    }

    // =============================================
    // GET CITY AT TILE
    // Returns null if tile is unclaimed.
    // =============================================
    public CityData GetCityAt(Vector2Int gridPos)
    {
        WorldTile tile =
            GridManager.Instance.grid[gridPos.x, gridPos.y];

        if (tile == null || tile.cityId < 0) return null;
        if (tile.cityId >= cities.Count) return null;

        return cities[tile.cityId];
    }

    // =============================================
    // CITY TICK
    // Called from GameManager.RunTick() after
    // population has been updated.
    // =============================================
    public void RunCityTick()
    {
        foreach (var city in cities)
        {
            UpdateCityPopulation(city);
            CheckBorderExpansion(city);
            RunCityServices(city);
            RunTaxIncome(city);
        }

        // Sync capital city values back to
        // GameManager for legacy UI compatibility
        SyncCapitalToGameManager();
    }

    // =============================================
    // PER-CITY: POPULATION
    // Count residential buildings inside border.
    // =============================================
    void UpdateCityPopulation(CityData city)
    {
        // Count residential capacity inside this city's border
        int residentialCap = 0;

        TileObject[] allTiles =
            FindObjectsOfType<TileObject>();

        foreach (var tile in allTiles)
        {
            if (tile.tileData.cityId != city.cityId)
                continue;

            if (tile.tileData.buildingType ==
                BuildingType.Residential)
            {
                residentialCap += 25;
            }
        }

        // For the capital (or single-city games),
        // mirror GameManager.population — it grows
        // gradually each tick driven by happiness.
        // For secondary cities, use a proportional
        // share of the global population based on
        // how many residential buildings they have
        // vs the total.
        if (city.isCapital || cities.Count == 1)
        {
            // Gradual value — never jumps to cap
            city.population = Mathf.Min(
                GameManager.Instance.population,
                residentialCap);
        }
        else
        {
            // Secondary city: proportion of global pop
            int totalCap = GameManager.Instance.maxPopulation;
            if (totalCap > 0 && residentialCap > 0)
            {
                float share = (float)residentialCap / totalCap;
                city.population = Mathf.RoundToInt(
                    GameManager.Instance.population * share);
            }
            else
            {
                city.population = 0;
            }
        }
    }

    // =============================================
    // PER-CITY: BORDER EXPANSION
    // =============================================
    void CheckBorderExpansion(CityData city)
    {
        bool expanded = city.TryExpandBorder();

        if (expanded)
        {
            RefreshCityBorderTiles(city);
            Debug.Log($"[CityManager] '{city.cityName}' " +
                      $"border expanded to radius {city.borderRadius}");
        }
    }

    // =============================================
    // PER-CITY: SERVICES
    // Healthcare, education, crime, happiness —
    // all calculated from buildings inside the
    // city's border only.
    // =============================================
    void RunCityServices(CityData city)
    {
        int hospitals = 0;
        int colleges = 0;
        int policeStations = 0;
        int residentials = 0;

        TileObject[] allTiles =
            FindObjectsOfType<TileObject>();

        foreach (var tile in allTiles)
        {
            if (tile == null || tile.tileData == null)
                continue;

            if (tile.tileData.cityId != city.cityId)
                continue;

            switch (tile.tileData.buildingType)
            {
                case BuildingType.Hospital:
                    if (tile.isFunded) hospitals++;
                    break;
                case BuildingType.CollegeCampus:
                    if (tile.isFunded) colleges++;
                    break;
                case BuildingType.PoliceStation:
                    if (tile.isFunded) policeStations++;
                    break;
                case BuildingType.Residential:
                    residentials++;
                    break;
            }
        }

        int pop = city.population;

        // ── HEALTHCARE ──────────────────────────
        if (pop <= 0 || hospitals == 0)
        {
            city.healthcare = 0;
        }
        else
        {
            int score = hospitals * 20
                - Mathf.Max(0, pop / 200 - hospitals) * 10;
            city.healthcare = Mathf.Clamp(score, 0, 100);
        }

        // ── EDUCATION ───────────────────────────
        if (pop <= 0 || colleges == 0)
        {
            city.education = 0;
        }
        else
        {
            int score = colleges * 20
                - Mathf.Max(0, pop / 250 - colleges) * 10;
            city.education = Mathf.Clamp(score, 0, 100);
        }

        // ── CRIME ───────────────────────────────
        GovernmentConfig cfg = Config;

        if (pop <= 0)
        {
            city.crime = 0;
        }
        else
        {
            int policyCrimeModifier = PolicyManager.Instance != null
                ? PolicyManager.Instance.CrimeModifier : 0;

            int crimeScore = pop / 100
                - policeStations * 20
                + Mathf.Max(0, 50 - city.happiness) / 5
                + cfg.crimeBaseline
                + policyCrimeModifier;

            city.crime = Mathf.Clamp(crimeScore, 0, 100);
        }

        // ── HAPPINESS ───────────────────────────
        if (pop <= 100)
        {
            // Early game lock
            city.happiness = Mathf.Max(
                50,
                50 + cfg.happinessBaseline);
            return;
        }

        int h = 50 + cfg.happinessBaseline;

        // Civilian goods — flow-based, matches HappinessManager.
        // No penalty until pop > 400.
        int sold = GameManager.Instance.monthlyGoodsSold;
        int demand = GameManager.Instance.monthlyGoodsDemand;

        if (demand <= 0)
        { if (pop > 400) h -= 5; }
        else
        {
            float salesRatio = Mathf.Clamp((float)sold / demand, 0f, 1.5f);
            if (salesRatio < 0.2f) h -= 12;
            else if (salesRatio < 0.5f) h -= 6;
            else if (salesRatio < 0.8f) h -= 2;
            else if (salesRatio < 1.0f) h += 6;
            else if (salesRatio < 1.3f) h += 12;
            else h += 18;
        }

        // Power
        if (GameManager.Instance.powerProduced >=
            GameManager.Instance.powerConsumed)
            h += 10;
        else
            h -= 8;

        // Healthcare — uses PolicyManager coverage
        int hCov = PolicyManager.Instance != null
            ? PolicyManager.Instance.HospitalCoverage : 500;
        float sEff = PolicyManager.Instance != null
            ? PolicyManager.Instance.ServiceEffectivenessModifier : 1f;
        if (hospitals == 0)
        { if (pop > 400) h -= 15; }
        else
        {
            float hcov = Mathf.Clamp01((float)(hospitals * hCov) / pop);
            h += Mathf.RoundToInt(Mathf.Lerp(0f, 22f * sEff, hcov));
        }

        // Education — uses PolicyManager coverage
        int eCov = PolicyManager.Instance != null
            ? PolicyManager.Instance.CollegeCoverage : 600;
        float eEff = PolicyManager.Instance != null
            ? PolicyManager.Instance.EducationEffectivenessModifier : 1f;
        if (colleges == 0)
        { if (pop > 500) h -= 10; }
        else
        {
            float ecov = Mathf.Clamp01((float)(colleges * eCov) / pop);
            h += Mathf.RoundToInt(Mathf.Lerp(0f, 18f * sEff * eEff, ecov));
        }

        // Crime
        h -= Mathf.RoundToInt(city.crime * 0.25f);

        // Random events and fuel availability affect every city's residents.
        if (RandomEventManager.Instance != null)
            h += RandomEventManager.Instance.HappinessModifier;

        if (GameManager.Instance.isGasShortage)
            h -= 8;
        else if (GameManager.Instance.monthlyGasSupplied > 0)
            h += 3;

        // Policy happiness modifier
        if (PolicyManager.Instance != null)
            h += PolicyManager.Instance.HappinessModifier;

        // Government happiness floor
        city.happiness = Mathf.Clamp(
            h,
            cfg.happinessFloor,
            100);

        // ── THRESHOLD CROSSING NEWS ──────────
        // Post to news when city happiness crosses
        // meaningful thresholds in either direction.
        if (prevCityHappiness.TryGetValue(city.cityId, out int prev))
        {
            string name = city.cityName;

            // Crossed into danger zones (going down)
            if (prev >= 20 && city.happiness < 20)
                NewsManager.Post(NewsCategory.Social,
                    $"{name}: Happiness critically low! Population may start fleeing.");
            else if (prev >= 30 && city.happiness < 30)
                NewsManager.Post(NewsCategory.Social,
                    $"{name}: Happiness very low. Strike risk increasing.");
            else if (prev >= 10 && city.happiness < 10)
                NewsManager.Post(NewsCategory.Social,
                    $"{name}: Happiness near zero! Severe penalties active.");

            // Crossed into recovery zones (going up)
            if (prev < 20 && city.happiness >= 20)
                NewsManager.Post(NewsCategory.Social,
                    $"{name}: Happiness recovering. Population flight risk reduced.");
            else if (prev < 30 && city.happiness >= 30)
                NewsManager.Post(NewsCategory.Social,
                    $"{name}: Happiness stabilising. Strike risk reduced.");
            else if (prev < 70 && city.happiness >= 70)
                NewsManager.Post(NewsCategory.Social,
                    $"{name}: Citizens thriving! Production and growth boosted.");
            else if (prev >= 70 && city.happiness < 70)
                NewsManager.Post(NewsCategory.Social,
                    $"{name}: Happiness dropping below 70%. Production bonus lost.");
        }

        prevCityHappiness[city.cityId] = city.happiness;
    }

    // =============================================
    // PER-CITY: TAX INCOME
    // Commercial buildings + population base tax.
    // Communism uses flat per-pop instead.
    // =============================================
    void RunTaxIncome(CityData city)
    {
        GovernmentConfig cfg = Config;

        // Clamp global tax rate to government max
        globalTaxRate = Mathf.Clamp(
            globalTaxRate, 0, cfg.maxTaxRate);

        // Mirror onto city data so UI reads right
        city.taxRate = globalTaxRate;

        int income = 0;

        if (cfg.commercialIncomeEnabled)
        {
            // Each citizen contributes based on
            // global tax rate and government multiplier.
            // At 0% tax the state collects nothing.
            if (globalTaxRate > 0)
            {
                income += Mathf.RoundToInt(
                    city.population *
                    (globalTaxRate / 100f) *
                    cfg.taxMultiplier);
            }
        }
        else
        {
            // Communism: flat state wage per citizen.
            // Still respects tax rate — setting it to
            // 0% means the state collects nothing.
            if (globalTaxRate > 0)
            {
                income += Mathf.RoundToInt(
                    city.population * cfg.flatIncomePerPop);
            }
        }

        // Only apply happiness modifier when there
        // is actual income — prevents 0 * modifier
        // rounding artifacts.
        if (income > 0)
        {
            float happinessMod = 0.5f + (city.happiness / 100f);
            income = Mathf.RoundToInt(income * happinessMod);

            // Use this city's crime level, not the capital's/national average.
            float crimeMod = city.crime >= 70 ? 0.50f
                : city.crime >= 50 ? 0.75f
                : city.crime >= 30 ? 0.90f
                : 1f;
            income = Mathf.RoundToInt(income * crimeMod);

            // Random events (market crash, sanctions)
            float eventMod = RandomEventManager.Instance != null
                ? RandomEventManager.Instance.TaxModifier : 1f;
            income = Mathf.RoundToInt(income * eventMod);

            // Policy tax modifier
            float policyTaxMod = PolicyManager.Instance != null
                ? PolicyManager.Instance.TaxModifier : 1f;
            income = Mathf.RoundToInt(income * policyTaxMod);
        }

        GameManager.Instance.money += income;
    }

    // =============================================
    // SYNC CAPITAL → GAMEMANGER
    // Keeps legacy UI (UIManager reads from
    // GameManager) accurate for the capital.
    // =============================================
    void SyncCapitalToGameManager()
    {
        if (Capital == null) return;

        var gm = GameManager.Instance;

        gm.happiness = Capital.happiness;
        gm.healthcare = Capital.healthcare;
        gm.education = Capital.education;
        gm.crime = Capital.crime;

        // Also sync to HappinessManager so its
        // modifiers stay accurate
        if (HappinessManager.Instance != null)
            HappinessManager.Instance.happiness =
                Capital.happiness;
    }

    // =============================================
    // BORDER TILE STAMPING
    // Marks every tile within the city's border
    // radius with that city's id.
    // Tiles claimed by an older city are not
    // overwritten (first-come first-served).
    // =============================================
    public void RefreshCityBorderTiles(CityData city)
    {
        GridManager grid = GridManager.Instance;

        Vector2Int center = city.townHallPosition;
        int r = city.borderRadius;

        for (int x = center.x - r; x <= center.x + r; x++)
        {
            for (int z = center.y - r; z <= center.y + r; z++)
            {
                if (x < 0 || x >= grid.width) continue;
                if (z < 0 || z >= grid.height) continue;

                // Square border — every tile in the
                // NxN area belongs to this city
                WorldTile wt = grid.grid[x, z];
                if (wt == null) continue;

                // Don't steal tiles from earlier cities
                if (wt.cityId >= 0 &&
                    wt.cityId != city.cityId)
                    continue;

                wt.cityId = city.cityId;

                // Apply border tint on the TileObject
                TileObject tileObj =
                    wt.visual?.GetComponent<TileObject>();

                if (tileObj != null)
                    tileObj.ApplyBorderColor(city.cityId);
            }
        }
    }

    // =============================================
    // MULTI-CITY UI HELPER
    // Returns a display string for the city panel.
    // =============================================
    public string GetCitySummary(CityData city)
    {
        return $"<b>{city.cityName}</b>\n" +
               $"Pop: {city.population}\n" +
               $"Happiness: {city.happiness}%\n" +
               $"Crime: {city.crime}%\n" +
               $"Healthcare: {city.healthcare}%\n" +
               $"Education: {city.education}%\n" +
               $"Tax Rate: {city.taxRate}%";
    }
}