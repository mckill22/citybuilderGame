using UnityEngine;
using System.Collections.Generic;

// =============================================
// POLICY MANAGER
// Singleton. Manages all government policies.
// Policies are country-wide and affect all cities.
//
// Unity setup:
//   Attach to a persistent GameObject.
//   PolicyPanelUI calls Toggle() on each policy.
// =============================================
public enum PolicyType
{
    // Political
    MartialLaw,
    FreePress,
    OpenBorders,
    ClosedBorders,
    StatePropaganda,

    // Economic
    NationalizedIndustry,
    FreeMarket,
    Austerity,
    TaxExemption,

    // Social
    HealthcareSubsidies,
    EducationDrive,
    HousingInitiative,
    FoodSubsidies,
    WorkersRights
}

public class PolicyManager : MonoBehaviour
{
    public static PolicyManager Instance;

    private HashSet<PolicyType> activePolicies = new HashSet<PolicyType>();

    // ─────────────────────────────────────────
    // COSTS per tick (money drained each tick)
    // ─────────────────────────────────────────
    private static readonly Dictionary<PolicyType, int> policyCosts =
        new Dictionary<PolicyType, int>
    {
        { PolicyType.MartialLaw,           20 },
        { PolicyType.FreePress,             0 },
        { PolicyType.OpenBorders,           0 },
        { PolicyType.ClosedBorders,         0 },
        { PolicyType.StatePropaganda,      15 },
        { PolicyType.NationalizedIndustry,  0 },
        { PolicyType.FreeMarket,            0 },
        { PolicyType.Austerity,             0 },
        { PolicyType.TaxExemption,          0 },
        { PolicyType.HealthcareSubsidies,  30 },
        { PolicyType.EducationDrive,       25 },
        { PolicyType.HousingInitiative,    20 },
        { PolicyType.FoodSubsidies,        25 },
        { PolicyType.WorkersRights,         0 },
    };

    // ─────────────────────────────────────────
    // GOVERNMENT RESTRICTIONS
    // null entry = available to all governments
    // ─────────────────────────────────────────
    private static readonly Dictionary<PolicyType, GovernmentType[]> allowedGovs =
        new Dictionary<PolicyType, GovernmentType[]>
    {
        { PolicyType.MartialLaw,
            new[]{ GovernmentType.Dictatorship, GovernmentType.Monarchy,
                   GovernmentType.Communism,    GovernmentType.Fascism } },
        { PolicyType.FreePress,
            new[]{ GovernmentType.Democracy } },
        { PolicyType.NationalizedIndustry,
            new[]{ GovernmentType.Communism, GovernmentType.Dictatorship,
                   GovernmentType.Fascism } },
        { PolicyType.FreeMarket,
            new[]{ GovernmentType.Democracy, GovernmentType.Monarchy } },
    };

    // ─────────────────────────────────────────
    // CONFLICTS — pairs that cannot coexist
    // ─────────────────────────────────────────
    private static readonly List<(PolicyType, PolicyType)> conflicts =
        new List<(PolicyType, PolicyType)>
    {
        (PolicyType.MartialLaw,           PolicyType.FreePress),
        (PolicyType.NationalizedIndustry, PolicyType.FreeMarket),
        (PolicyType.OpenBorders,          PolicyType.ClosedBorders),
        (PolicyType.Austerity,            PolicyType.HealthcareSubsidies),
        (PolicyType.Austerity,            PolicyType.EducationDrive),
    };

    void Awake() { Instance = this; }

    void Start()
    {
        if (GameClock.Instance != null)
        {
            GameClock.Instance.OnTick += OnTick;
            GameClock.Instance.OnMonthEnd += OnMonthEnd;
        }
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
        {
            GameClock.Instance.OnTick -= OnTick;
            GameClock.Instance.OnMonthEnd -= OnMonthEnd;
        }
    }

    // =========================================
    // TICK — drain per-tick policy costs
    // =========================================
    void OnTick()
    {
        if (GameManager.Instance == null) return;
        foreach (var policy in activePolicies)
        {
            int cost = GetCostPerTick(policy);
            if (cost > 0) GameManager.Instance.money -= cost;
        }
    }

    // =========================================
    // MONTH END — Martial Law crisis check
    // =========================================
    void OnMonthEnd()
    {
        if (!IsActive(PolicyType.MartialLaw)) return;
        if (GameManager.Instance == null) return;

        int crime = GameManager.Instance.crime;
        int happiness = GameManager.Instance.happiness;

        if (crime < 40 && happiness > 35)
            NewsManager.Post(NewsCategory.Political,
                "Crisis resolved. Consider lifting Martial Law to restore public happiness.");
    }

    // =========================================
    // TOGGLE
    // =========================================
    public bool Toggle(PolicyType policy)
    {
        return IsActive(policy) ? Deactivate(policy) : Activate(policy);
    }

    // =========================================
    // ACTIVATE
    // =========================================
    public bool Activate(PolicyType policy)
    {
        if (!IsAllowedForGovernment(policy))
        {
            NewsManager.Post(NewsCategory.Political,
                $"{GetPolicyName(policy)} is not available under the current government.");
            return false;
        }

        if (policy == PolicyType.MartialLaw && !IsCrisisActive())
        {
            NewsManager.Post(NewsCategory.Political,
                "Martial Law requires a crisis: crime > 60 or happiness < 25.");
            return false;
        }

        // Deactivate conflicting policies first
        foreach (var (a, b) in conflicts)
        {
            if (a == policy && IsActive(b)) Deactivate(b);
            if (b == policy && IsActive(a)) Deactivate(a);
        }

        activePolicies.Add(policy);
        NewsManager.Post(NewsCategory.Political,
            $"Policy enacted: {GetPolicyName(policy)}. {GetActivateDescription(policy)}");
        return true;
    }

    // =========================================
    // DEACTIVATE
    // =========================================
    public bool Deactivate(PolicyType policy)
    {
        if (!activePolicies.Contains(policy)) return false;
        activePolicies.Remove(policy);
        NewsManager.Post(NewsCategory.Political,
            $"Policy lifted: {GetPolicyName(policy)}.");
        return true;
    }

    // =========================================
    // QUERIES
    // =========================================
    public bool IsActive(PolicyType policy) =>
        activePolicies.Contains(policy);

    public bool IsAllowedForGovernment(PolicyType policy)
    {
        if (!allowedGovs.ContainsKey(policy)) return true;
        if (CityManager.Instance == null) return true;
        GovernmentType gov = CityManager.Instance.GovernmentType;
        foreach (var g in allowedGovs[policy])
            if (g == gov) return true;
        return false;
    }

    public bool IsCrisisActive()
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.crime > 60 ||
               GameManager.Instance.happiness < 25;
    }

    public int GetCostPerTick(PolicyType policy) =>
        policyCosts.TryGetValue(policy, out int c) ? c : 0;

    // =========================================
    // MODIFIERS — read by existing systems
    // =========================================
    public int HappinessModifier =>
        (IsActive(PolicyType.MartialLaw) ? -15 : 0) +
        (IsActive(PolicyType.FreePress) ? +8 : 0) +
        (IsActive(PolicyType.ClosedBorders) ? +5 : 0) +
        (IsActive(PolicyType.StatePropaganda) ? +10 : 0) +
        (IsActive(PolicyType.FreeMarket) ? +5 : 0) +
        (IsActive(PolicyType.Austerity) ? -20 : 0) +
        (IsActive(PolicyType.TaxExemption) ? +12 : 0) +
        (IsActive(PolicyType.WorkersRights) ? +5 : 0);

    public int CrimeModifier =>
        (IsActive(PolicyType.MartialLaw) ? -30 : 0) +
        (IsActive(PolicyType.OpenBorders) ? +10 : 0) +
        (IsActive(PolicyType.ClosedBorders) ? -5 : 0);

    public float ProductionModifier =>
        (IsActive(PolicyType.NationalizedIndustry) ? 1.15f : 1f) *
        (IsActive(PolicyType.WorkersRights) ? 0.95f : 1f);

    public bool CommercialEnabled =>
        !IsActive(PolicyType.NationalizedIndustry);

    public float CommercialIncomeModifier =>
        CommercialEnabled ? (IsActive(PolicyType.FreeMarket) ? 1.25f : 1f) : 0f;

    public float TaxModifier =>
        (IsActive(PolicyType.NationalizedIndustry) ? 0.85f : 1f) *
        (IsActive(PolicyType.FreeMarket) ? 0.90f : 1f) *
        (IsActive(PolicyType.TaxExemption) ? 0.60f : 1f);

    public float UpkeepModifier =>
        IsActive(PolicyType.Austerity) ? 0.5f : 1f;

    public int HospitalCoverage =>
        IsActive(PolicyType.HealthcareSubsidies) ? 800 : 500;

    public int CollegeCoverage =>
        IsActive(PolicyType.EducationDrive) ? 900 : 600;

    public float ServiceEffectivenessModifier =>
        IsActive(PolicyType.Austerity) ? 0.7f : 1f;

    public float ResidentialCapacityModifier =>
        IsActive(PolicyType.HousingInitiative) ? 1.2f : 1f;

    public float PopulationGrowthModifier =>
        (IsActive(PolicyType.MartialLaw) ? 0.80f : 1f) *
        (IsActive(PolicyType.OpenBorders) ? 1.30f : 1f) *
        (IsActive(PolicyType.ClosedBorders) ? 0.80f : 1f);

    public int FoodShortageThreshold(int baseThreshold) =>
        IsActive(PolicyType.FoodSubsidies) ? baseThreshold * 2 : baseThreshold;

    public int StrikeHappinessThreshold(int baseThreshold) =>
        IsActive(PolicyType.WorkersRights) ? baseThreshold - 8 : baseThreshold;

    public float RandomEventChanceModifier =>
        IsActive(PolicyType.FreePress) ? 1.25f : 1f;

    public float EducationEffectivenessModifier =>
        IsActive(PolicyType.StatePropaganda) ? 0.8f : 1f;

    // =========================================
    // UI HELPERS
    // =========================================
    public string GetPolicyName(PolicyType p)
    {
        switch (p)
        {
            case PolicyType.MartialLaw: return "Martial Law";
            case PolicyType.FreePress: return "Free Press";
            case PolicyType.OpenBorders: return "Open Borders";
            case PolicyType.ClosedBorders: return "Closed Borders";
            case PolicyType.StatePropaganda: return "State Propaganda";
            case PolicyType.NationalizedIndustry: return "Nationalized Industry";
            case PolicyType.FreeMarket: return "Free Market";
            case PolicyType.Austerity: return "Austerity";
            case PolicyType.TaxExemption: return "Tax Exemption";
            case PolicyType.HealthcareSubsidies: return "Healthcare Subsidies";
            case PolicyType.EducationDrive: return "Education Drive";
            case PolicyType.HousingInitiative: return "Housing Initiative";
            case PolicyType.FoodSubsidies: return "Food Subsidies";
            case PolicyType.WorkersRights: return "Workers Rights";
            default: return p.ToString();
        }
    }

    public string GetActivateDescription(PolicyType p)
    {
        switch (p)
        {
            case PolicyType.MartialLaw: return "Crime -30, happiness -15, growth -20%. Costs $20/tick.";
            case PolicyType.FreePress: return "Happiness +8. Random events +25% more frequent.";
            case PolicyType.OpenBorders: return "Population growth +30%, crime +10.";
            case PolicyType.ClosedBorders: return "Population growth -20%, crime -5, happiness +5.";
            case PolicyType.StatePropaganda: return "Happiness +10, education effectiveness -20%. Costs $15/tick.";
            case PolicyType.NationalizedIndustry: return "Production +15%, tax -15%, commercial income disabled.";
            case PolicyType.FreeMarket: return "Commercial income +25%, tax -10%, happiness +5.";
            case PolicyType.Austerity: return "Upkeep halved, happiness -20, services -30%.";
            case PolicyType.TaxExemption: return "Happiness +12, tax income -40%.";
            case PolicyType.HealthcareSubsidies: return "Each hospital covers 800 pop. Costs $30/tick.";
            case PolicyType.EducationDrive: return "Each college covers 900 pop. Costs $25/tick.";
            case PolicyType.HousingInitiative: return "Residential capacity +20%. Costs $20/tick.";
            case PolicyType.FoodSubsidies: return "Famine threshold doubled. Costs $25/tick.";
            case PolicyType.WorkersRights: return "Strike threshold raised, happiness +5, production -5%.";
            default: return "";
        }
    }

    public string GetCategory(PolicyType p)
    {
        switch (p)
        {
            case PolicyType.MartialLaw:
            case PolicyType.FreePress:
            case PolicyType.OpenBorders:
            case PolicyType.ClosedBorders:
            case PolicyType.StatePropaganda: return "Political";
            case PolicyType.NationalizedIndustry:
            case PolicyType.FreeMarket:
            case PolicyType.Austerity:
            case PolicyType.TaxExemption: return "Economic";
            default: return "Social";
        }
    }

    public List<PolicyType> GetAllPolicies() =>
        new List<PolicyType>((PolicyType[])System.Enum.GetValues(typeof(PolicyType)));
}