using UnityEngine;
using System.Collections.Generic;

public class HappinessManager : MonoBehaviour
{
    public static HappinessManager Instance;

    // ─────────────────────────────────────────
    // FINAL VALUES
    // ─────────────────────────────────────────
    [Header("Final Values")]
    [Range(0, 100)] public int happiness = 50;
    [Range(0, 100)] public int crimeRate = 0;

    // ─────────────────────────────────────────
    // CRIME SETTINGS
    // ─────────────────────────────────────────
    [Header("Crime Settings")]
    public float baseCrimeGrowth = 2f;
    public float unhappinessCrimeMultiplier = 0.15f;
    public float policeReductionPerStation = 8f;

    [Header("Police Coverage")]
    public int policeRadius = 6;

    // ─────────────────────────────────────────
    // HAPPINESS MODIFIERS (Inspector readable)
    // ─────────────────────────────────────────
    [Header("Happiness Modifiers")]
    public int civilianGoodsBonus;
    public int electricityBonus;
    public int healthcareBonus;
    public int educationBonus;
    public int crimePenalty;
    public int otherFactorsBonus;

    // ─────────────────────────────────────────
    // STRIKE SYSTEM
    // ─────────────────────────────────────────
    [Header("Strike System")]
    public int strikeHappinessThreshold = 25;  // happiness must be below this
    public int strikeTicksRequired = 5;   // consecutive ticks before strike
    public int strikeDuration = 10;  // ticks the strike lasts
    public int strikeCooldown = 20;  // ticks before another can start

    [HideInInspector] public bool strikeActive = false;
    [HideInInspector] public int unhappyTickCounter = 0;
    [HideInInspector] public int strikeTicksRemaining = 0;
    [HideInInspector] public int strikeCooldownTicks = 0;

    // ─────────────────────────────────────────
    // POPULATION FLIGHT
    // ─────────────────────────────────────────
    [Header("Population Flight")]
    public int flightHappinessThreshold = 20;  // happiness must be below this
    public int flightTicksRequired = 10;  // consecutive ticks before flight
    public float flightRatePerTick = 0.002f; // fraction of pop that leaves

    [HideInInspector] public bool populationFlight = false;
    [HideInInspector] public int flightTickCounter = 0;

    void Awake() { Instance = this; }

    // =========================================
    // MAIN TICK — called from GameManager
    // =========================================
    public void RunHappinessTick()
    {
        if (GameManager.Instance == null || CityManager.Instance == null)
            return;

        CityData capital = CityManager.Instance.Capital;
        if (capital == null)
            return;

        // CityManager owns the actual calculation. This manager only runs
        // country-level consequences that still use the legacy population model.
        happiness = capital.happiness;
        crimeRate = capital.crime;
        GameManager.Instance.happiness = capital.happiness;

        if (capital.population <= 100)
        {
            unhappyTickCounter = 0;
            strikeActive = false;
            strikeTicksRemaining = 0;
            strikeCooldownTicks = 0;
            flightTickCounter = 0;
            populationFlight = false;
            return;
        }

        ApplyHappinessThresholdBonus();
        UpdateStrikeSystem();
        UpdatePopulationFlight();
    }

    // =========================================
    // HAPPINESS THRESHOLD BONUSES / PENALTIES
    // Applied on top of the base calculation
    // =========================================
    void ApplyHappinessThresholdBonus()
    {
        // These are reported to News once when
        // crossing a threshold, not every tick.
        // The economic/growth modifiers handle
        // the actual mechanical effects.

        if (happiness >= 90)
        {
            // Monthly money gift — checked in
            // GameManager.OnMonthEnd via GetMonthlyHappinessBonus()
        }
    }

    // =========================================
    // STRIKE SYSTEM
    // =========================================
    void UpdateStrikeSystem()
    {
        string cityName = CityManager.Instance != null && CityManager.Instance.Capital != null
            ? CityManager.Instance.Capital.cityName : "your city";

        // Count down cooldown
        if (strikeCooldownTicks > 0)
        {
            strikeCooldownTicks--;
            return;
        }

        // Count down active strike
        if (strikeActive)
        {
            strikeTicksRemaining--;
            if (strikeTicksRemaining <= 0)
            {
                strikeActive = false;
                strikeCooldownTicks = strikeCooldown;
                unhappyTickCounter = 0;
                NewsManager.Post(NewsCategory.Social,
                    $"{cityName}: Strike ended. Workers return to factories.");
            }
            return;
        }

        // Track consecutive unhappy ticks
        int effectiveStrikeThreshold = PolicyManager.Instance != null
            ? PolicyManager.Instance.StrikeHappinessThreshold(strikeHappinessThreshold)
            : strikeHappinessThreshold;

        if (happiness < effectiveStrikeThreshold)
        {
            unhappyTickCounter++;
            if (unhappyTickCounter >= strikeTicksRequired)
            {
                strikeActive = true;
                strikeTicksRemaining = strikeDuration;
                unhappyTickCounter = 0;
                NewsManager.Post(NewsCategory.Social,
                    $"{cityName}: Workers Strike! Industrial production halted. Happiness critical.");
            }
        }
        else
        {
            if (unhappyTickCounter > 0)
                unhappyTickCounter = Mathf.Max(0, unhappyTickCounter - 1);
        }
    }

    // =========================================
    // POPULATION FLIGHT
    // =========================================
    void UpdatePopulationFlight()
    {
        string cityName = CityManager.Instance != null && CityManager.Instance.Capital != null
            ? CityManager.Instance.Capital.cityName : "your city";

        if (happiness < flightHappinessThreshold)
        {
            flightTickCounter++;

            if (flightTickCounter >= flightTicksRequired)
            {
                if (!populationFlight)
                {
                    populationFlight = true;
                    NewsManager.Post(NewsCategory.Social,
                        $"{cityName}: Citizens Fleeing! Population leaving due to extreme unhappiness.");
                }

                int leaving = Mathf.Max(1,
                    Mathf.RoundToInt(
                        GameManager.Instance.population * flightRatePerTick));
                GameManager.Instance.population = Mathf.Max(
                    0, GameManager.Instance.population - leaving);
            }
        }
        else
        {
            if (populationFlight)
            {
                populationFlight = false;
                flightTickCounter = 0;
                NewsManager.Post(NewsCategory.Social,
                    $"{cityName}: Population stabilising. Citizens no longer fleeing.");
            }
            else
            {
                flightTickCounter = Mathf.Max(0, flightTickCounter - 1);
            }
        }
    }

    // =========================================
    // CRIME CALCULATION
    // =========================================
    void CalculateCrime()
    {
        if (GameManager.Instance.population <= 0)
        {
            crimeRate = 0;
            return;
        }

        List<TileObject> allTiles = GridManager.Instance.allTiles;

        int residentialTiles = 0;
        int coveredResidentials = 0;
        int policeStations = 0;

        foreach (TileObject tile in allTiles)
        {
            if (tile == null || tile.tileData == null) continue;
            if (tile.tileData.buildingType == BuildingType.Residential)
                residentialTiles++;
            else if (tile.tileData.buildingType == BuildingType.PoliceStation &&
                     tile.isFunded)
                policeStations++;
        }

        foreach (TileObject tile in allTiles)
        {
            if (tile == null || tile.tileData == null) continue;
            if (tile.tileData.buildingType != BuildingType.PoliceStation) continue;
            if (!tile.isFunded) continue;
            coveredResidentials += CountResidentialCoverage(tile, allTiles);
        }

        float coverageRatio = residentialTiles > 0
            ? Mathf.Clamp01((float)coveredResidentials / residentialTiles)
            : 0f;

        float uncovered = 1f - coverageRatio;
        float unhappinessFactor = Mathf.Max(0, 50 - happiness);
        float crimeGrowth = baseCrimeGrowth +
                                   (unhappinessFactor * unhappinessCrimeMultiplier);

        crimeRate += Mathf.RoundToInt(crimeGrowth * uncovered);
        crimeRate -= Mathf.RoundToInt(policeStations * policeReductionPerStation);
        crimeRate = Mathf.Clamp(crimeRate, 0, 100);
    }

    // =========================================
    // HAPPINESS CALCULATION
    // =========================================
    void CalculateHappiness()
    {
        if (GameManager.Instance.population <= 100)
        {
            happiness = 50;
            civilianGoodsBonus = electricityBonus = healthcareBonus =
                educationBonus = crimePenalty = 0;
            GameManager.Instance.happiness = happiness;
            return;
        }

        // ─────────────────────────────────────
        // All modifiers collected first,
        // applied in one shot at the end.
        // Weights:
        //   Food/famine   handled separately (famine system)
        //   Crime         up to ±25
        //   Healthcare    up to ±20
        //   Education     up to ±15
        //   Power         up to ±12
        //   Civilian goods up to ±15 (flow-based)
        // ─────────────────────────────────────

        // ── CIVILIAN GOODS (flow-based) ──────
        // Based on monthly sales vs monthly demand.
        // No commercial buildings = no sales = penalty.
        int sold = GameManager.Instance.monthlyGoodsSold;
        int demand = GameManager.Instance.monthlyGoodsDemand;

        if (demand <= 0)
        {
            // No commercial buildings at all
            civilianGoodsBonus = GameManager.Instance.population > 200
                ? -10 : 0;
        }
        else
        {
            float salesRatio = Mathf.Clamp((float)sold / demand, 0f, 1.5f);
            if (salesRatio < 0.2f) civilianGoodsBonus = -15;
            else if (salesRatio < 0.5f) civilianGoodsBonus = -8;
            else if (salesRatio < 0.8f) civilianGoodsBonus = -3;
            else if (salesRatio < 1.0f) civilianGoodsBonus = 5;
            else if (salesRatio < 1.3f) civilianGoodsBonus = 10;
            else civilianGoodsBonus = 15;
        }

        // ── POWER ────────────────────────────
        electricityBonus = GameManager.Instance.powerProduced >=
                           GameManager.Instance.powerConsumed ? 12 : -12;

        // ── HEALTHCARE ───────────────────────
        List<TileObject> allTiles = GridManager.Instance.allTiles;
        int hospitals = 0, colleges = 0;
        foreach (TileObject tile in allTiles)
        {
            if (tile == null || tile.tileData == null) continue;
            switch (tile.tileData.buildingType)
            {
                case BuildingType.Hospital:
                    if (tile.isFunded) hospitals++;
                    break;
                case BuildingType.CollegeCampus:
                    if (tile.isFunded) colleges++;
                    break;
            }
        }

        int pop = GameManager.Instance.population;

        // Hospital coverage — 1 hospital covers 500 pop by default,
        // 800 with Healthcare Subsidies policy. No penalty until pop > 400.
        int hospitalCoverage = PolicyManager.Instance != null
            ? PolicyManager.Instance.HospitalCoverage : 500;
        float serviceEff = PolicyManager.Instance != null
            ? PolicyManager.Instance.ServiceEffectivenessModifier : 1f;

        if (hospitals == 0)
            healthcareBonus = pop > 400 ? -15 : 0;
        else
        {
            float coverage = Mathf.Clamp01((float)(hospitals * hospitalCoverage) / pop);
            healthcareBonus = Mathf.RoundToInt(Mathf.Lerp(0f, 22f * serviceEff, coverage));
        }

        // ── EDUCATION ────────────────────────
        // 1 college covers 600 pop by default, 900 with Education Drive.
        // No penalty until pop > 500.
        int collegeCoverage = PolicyManager.Instance != null
            ? PolicyManager.Instance.CollegeCoverage : 600;
        float eduEff = PolicyManager.Instance != null
            ? PolicyManager.Instance.EducationEffectivenessModifier : 1f;

        if (colleges == 0)
            educationBonus = pop > 500 ? -10 : 0;
        else
        {
            float coverage = Mathf.Clamp01((float)(colleges * collegeCoverage) / pop);
            educationBonus = Mathf.RoundToInt(Mathf.Lerp(0f, 18f * serviceEff * eduEff, coverage));
        }

        // ── CRIME ────────────────────────────
        // Max -25 at 100 crime
        crimePenalty = Mathf.RoundToInt(crimeRate * 0.25f);

        // ── APPLY ALL ────────────────────────
        happiness = 50
            + civilianGoodsBonus
            + electricityBonus
            + healthcareBonus
            + educationBonus
            - crimePenalty;

        // Random event modifiers
        if (RandomEventManager.Instance != null)
            happiness += RandomEventManager.Instance.HappinessModifier;

        // Policy modifiers
        if (PolicyManager.Instance != null)
            happiness += PolicyManager.Instance.HappinessModifier;

        // Gas shortage penalty / gas station happiness
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.isGasShortage)
            {
                happiness -= 8; // population unhappy when fuel is scarce
            }
            else if (GameManager.Instance.monthlyGasSupplied > 0)
            {
                happiness += 3; // small bonus when gas stations are running
            }
        }

        happiness = Mathf.Clamp(happiness, 0, 100);
        GameManager.Instance.happiness = happiness;
    }

    // =========================================
    // HELPERS
    // =========================================
    int CountResidentialCoverage(TileObject police, List<TileObject> allTiles)
    {
        int count = 0;
        foreach (TileObject tile in allTiles)
        {
            if (tile == null || tile.tileData == null) continue;
            if (tile.tileData.buildingType != BuildingType.Residential) continue;
            float dist = Vector2Int.Distance(police.gridPosition, tile.gridPosition);
            if (dist <= policeRadius) count++;
        }
        return count;
    }

    // ─────────────────────────────────────────
    // ECONOMIC MODIFIER
    // Used by commercial income and tax income.
    // Now includes happiness thresholds.
    // ─────────────────────────────────────────
    public float GetEconomicModifier()
    {
        float modifier = 1f;

        // Base happiness effect
        modifier += (happiness - 50) * 0.01f;

        // Crime penalty
        modifier -= crimeRate * 0.005f;

        // Threshold penalties
        if (happiness < 10) modifier -= 0.50f;
        else if (happiness < 20) modifier -= 0.30f;
        else if (happiness < 30) modifier -= 0.15f;

        // Threshold bonuses
        if (happiness >= 90) modifier += 0.15f;

        return Mathf.Clamp(modifier, 0.2f, 2f);
    }

    // ─────────────────────────────────────────
    // POPULATION GROWTH MODIFIER
    // ─────────────────────────────────────────
    public float GetPopulationGrowthModifier()
    {
        // Flight and strike conditions gate
        // growth entirely in GameManager —
        // this modifier handles normal scaling.
        if (happiness < 20) return 0f; // no growth
        if (happiness < 30) return 0.25f;

        float modifier = 1f;
        modifier += (happiness - 50) * 0.015f;
        modifier -= crimeRate * 0.01f;

        if (happiness >= 70) modifier += 0.20f;

        return Mathf.Clamp(modifier, 0.1f, 3f);
    }

    // ─────────────────────────────────────────
    // PRODUCTIVITY MODIFIER
    // Applied to building output in GameManager.
    // Combines happiness thresholds + education
    // + healthcare effects.
    // ─────────────────────────────────────────
    public float GetProductivityModifier()
    {
        float modifier = 1f;

        // Happiness thresholds
        if (happiness < 20) modifier -= 0.35f;
        else if (happiness < 30) modifier -= 0.20f;
        else if (happiness >= 70) modifier += 0.10f;

        // Education bonus (uses GameManager.education score)
        int edu = GameManager.Instance.education;
        if (edu >= 80) modifier += 0.10f;
        else if (edu >= 60) modifier += 0.05f;
        else if (edu < 20 && GameManager.Instance.population > 200)
            modifier -= 0.10f;

        // Healthcare bonus
        int health = GameManager.Instance.healthcare;
        if (health >= 80) modifier += 0.05f;
        else if (health < 20 && GameManager.Instance.population > 300)
            modifier -= 0.05f;

        // Crime penalty on productivity
        if (CrimeManager.Instance != null &&
                 CrimeManager.Instance.globalCrime >= 70)
            modifier -= 0.35f;
        else if (CrimeManager.Instance != null &&
                 CrimeManager.Instance.globalCrime >= 40)
            modifier -= 0.15f;

        return Mathf.Clamp(modifier, 0.1f, 2f);
    }

    // ─────────────────────────────────────────
    // TAX CRIME MODIFIER
    // Applied to tax income in CityManager.
    // ─────────────────────────────────────────
    public float GetTaxCrimeModifier()
    {
        if (CrimeManager.Instance == null) return 1f;

        float crime = CrimeManager.Instance.globalCrime;
        if (crime >= 70) return 0.50f;
        else if (crime >= 50) return 0.75f;
        else if (crime >= 30) return 0.90f;
        return 1f;
    }

    // ─────────────────────────────────────────
    // MONTHLY HAPPINESS BONUS
    // Called from GameManager.RunMonthlyUpkeep
    // ─────────────────────────────────────────
    public int GetMonthlyHappinessBonus()
    {
        if (happiness >= 90) return 50;  // thriving economy gift
        return 0;
    }
}