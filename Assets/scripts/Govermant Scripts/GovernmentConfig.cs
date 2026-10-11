using UnityEngine;

// =============================================
// GOVERNMENT CONFIG
// Static lookup — each GovernmentType maps to
// a set of flat mechanical modifiers.
// Add new governments here without touching
// any other game logic.
// =============================================
[System.Serializable]
public class GovernmentConfig
{
    // Tax
    public float taxMultiplier;      // multiplies tax income
    public int maxTaxRate;         // hard cap on tax rate (%)

    // Happiness
    public int happinessFloor;     // minimum happiness allowed
    public int happinessBaseline;  // added to base 50 each tick

    // Production
    public float civilianProductionBonus;  // multiplier on civilian goods output
    public float militaryProductionBonus;  // multiplier on military goods/supplies

    // Crime
    public int crimeBaseline;      // flat added to every city's crime

    // Economy
    public bool commercialIncomeEnabled;  // false = Communism flat income
    public float flatIncomePerPop;         // used when commercial disabled

    public static GovernmentConfig Get(GovernmentType type)
    {
        switch (type)
        {
            // ──────────────────────────────────────
            // DEMOCRACY
            // Balanced. Tax capped at 30% to keep
            // citizens happy. Normal production.
            // ──────────────────────────────────────
            case GovernmentType.Democracy:
                return new GovernmentConfig
                {
                    taxMultiplier = 1.0f,
                    maxTaxRate = 30,
                    happinessFloor = 30,
                    happinessBaseline = 5,
                    civilianProductionBonus = 1.0f,
                    militaryProductionBonus = 1.0f,
                    crimeBaseline = 0,
                    commercialIncomeEnabled = true,
                    flatIncomePerPop = 0f
                };

            // ──────────────────────────────────────
            // DICTATORSHIP
            // High tax, no happiness floor.
            // Production normal, crime higher.
            // ──────────────────────────────────────
            case GovernmentType.Dictatorship:
                return new GovernmentConfig
                {
                    taxMultiplier = 1.4f,
                    maxTaxRate = 60,
                    happinessFloor = 0,
                    happinessBaseline = -10,
                    civilianProductionBonus = 1.0f,
                    militaryProductionBonus = 1.1f,
                    crimeBaseline = 10,
                    commercialIncomeEnabled = true,
                    flatIncomePerPop = 0f
                };

            // ──────────────────────────────────────
            // MONARCHY
            // Flat crown bonus to income.
            // Normal happiness, slight crime reduction
            // from strong central authority.
            // ──────────────────────────────────────
            case GovernmentType.Monarchy:
                return new GovernmentConfig
                {
                    taxMultiplier = 1.2f,
                    maxTaxRate = 40,
                    happinessFloor = 20,
                    happinessBaseline = 0,
                    civilianProductionBonus = 1.0f,
                    militaryProductionBonus = 1.0f,
                    crimeBaseline = -5,
                    commercialIncomeEnabled = true,
                    flatIncomePerPop = 0f
                };

            // ──────────────────────────────────────
            // COMMUNISM
            // No commercial income — flat per-pop
            // income instead. Production bonus.
            // Slight happiness penalty.
            // ──────────────────────────────────────
            case GovernmentType.Communism:
                return new GovernmentConfig
                {
                    taxMultiplier = 1.0f,
                    maxTaxRate = 50,
                    happinessFloor = 10,
                    happinessBaseline = -5,
                    civilianProductionBonus = 1.2f,
                    militaryProductionBonus = 1.2f,
                    crimeBaseline = 5,
                    commercialIncomeEnabled = false,
                    flatIncomePerPop = 0.3f  // 0.3 money per citizen per tick
                };

            // ──────────────────────────────────────
            // FASCISM
            // Military production bonus.
            // Crime heavily suppressed (police state).
            // Happiness capped low.
            // ──────────────────────────────────────
            case GovernmentType.Fascism:
                return new GovernmentConfig
                {
                    taxMultiplier = 1.1f,
                    maxTaxRate = 50,
                    happinessFloor = 0,
                    happinessBaseline = -15,
                    civilianProductionBonus = 1.0f,
                    militaryProductionBonus = 1.25f,
                    crimeBaseline = -15,
                    commercialIncomeEnabled = true,
                    flatIncomePerPop = 0f
                };

            default:
                return Get(GovernmentType.Democracy);
        }
    }
}

// =============================================
// CITY DATA
// One instance per city. Owned by CityManager.
// Holds all per-city state.
// =============================================
[System.Serializable]
public class CityData
{
    // ──────────────────────────────────────
    // IDENTITY
    // ──────────────────────────────────────
    public int cityId;
    public string cityName;
    public bool isCapital;

    // Leader and government live on the capital
    // only; CityManager exposes them globally.
    public string leaderName;
    public GovernmentType governmentType;

    // ──────────────────────────────────────
    // SPATIAL
    // ──────────────────────────────────────
    public Vector2Int townHallPosition;

    // Current border radius in tiles from center.
    // Starts at 5 (giving a 10x10 area when
    // treating the TownHall as center).
    public int borderRadius = 5;

    // ──────────────────────────────────────
    // POPULATION (per-city)
    // ──────────────────────────────────────
    public int population;

    // Population milestone tracker for border
    // expansion. Resets after each expansion.
    public int popAtLastExpansion = 0;
    public int nextExpansionPop = 200; // first ring at 200 pop

    // ──────────────────────────────────────
    // CITY SERVICES (per-city)
    // ──────────────────────────────────────
    [Range(0, 100)] public int happiness = 50;
    [Range(0, 100)] public int crime = 0;
    [Range(0, 100)] public int healthcare = 0;
    [Range(0, 100)] public int education = 0;

    // ──────────────────────────────────────
    // TAXATION
    // ──────────────────────────────────────
    // taxRate is 0–100 (percentage).
    // Clamped to GovernmentConfig.maxTaxRate.
    public int taxRate = 10;

    // ──────────────────────────────────────
    // CONSTRUCTOR
    // ──────────────────────────────────────
    public CityData(
        int id,
        string name,
        Vector2Int townHallPos,
        bool capital = false)
    {
        cityId = id;
        cityName = name;
        townHallPosition = townHallPos;
        isCapital = capital;
        population = 0;
        happiness = 50;
        crime = 0;
        healthcare = 0;
        education = 0;
        taxRate = 10;
        borderRadius = 5;
    }

    // ──────────────────────────────────────
    // BORDER EXPANSION
    // Called by CityManager each tick after
    // population is updated.
    // ──────────────────────────────────────
    public bool TryExpandBorder()
    {
        if (population < nextExpansionPop)
            return false;

        borderRadius++;
        popAtLastExpansion = population;
        nextExpansionPop = population + 200;

        return true;
    }
}