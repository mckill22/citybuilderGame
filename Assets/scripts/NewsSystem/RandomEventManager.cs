using UnityEngine;
using System.Collections.Generic;

// =============================================
// RANDOM EVENT MANAGER
// Fires on GameClock.OnMonthEnd.
// Each event has conditions, a duration in
// months, and mechanical modifiers that other
// systems read via static accessors.
// =============================================
public enum RandomEventType
{
    TradeBoom,
    MarketCrash,
    ResourceDiscovery,
    InternationalAid,
    Sanctions,
    DiseaseOutbreak,
    SerialKiller,
    Drought,
    LaborDispute
}

[System.Serializable]
public class ActiveRandomEvent
{
    public RandomEventType type;
    public int monthsRemaining;
    public string title;

    public ActiveRandomEvent(RandomEventType t, int duration, string title)
    {
        type = t;
        monthsRemaining = duration;
        this.title = title;
    }
}

public class RandomEventManager : MonoBehaviour
{
    public static RandomEventManager Instance;

    // ─────────────────────────────────────────
    // ACTIVE EVENTS
    // ─────────────────────────────────────────
    public List<ActiveRandomEvent> activeEvents = new List<ActiveRandomEvent>();

    // ─────────────────────────────────────────
    // SETTINGS
    // ─────────────────────────────────────────
    [Header("Event Chances (0-1 per month)")]
    public float tradeBoomChance = 0.06f;
    public float marketCrashChance = 0.04f;
    public float resourceDiscoveryChance = 0.05f;
    public float internationalAidChance = 0.05f;
    public float sanctionsChance = 0.03f;
    public float diseaseOutbreakChance = 0.06f;
    public float serialKillerChance = 0.04f;
    public float droughtChance = 0.05f;
    public float laborDisputeChance = 0.05f;

    // ─────────────────────────────────────────
    // MODIFIERS — read by other systems
    // ─────────────────────────────────────────
    public float ProductionModifier { get; private set; } = 1f;
    public float TaxModifier { get; private set; } = 1f;
    public float FarmingModifier { get; private set; } = 1f;
    public int CrimeModifier { get; private set; } = 0;
    public int HappinessModifier { get; private set; } = 0;

    void Awake() { Instance = this; }

    void Start()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnMonthEnd += RunMonthlyEvents;
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnMonthEnd -= RunMonthlyEvents;
    }

    // =========================================
    // MONTHLY TICK
    // =========================================
    void RunMonthlyEvents()
    {
        // 1 — Age existing events, remove expired
        TickActiveEvents();

        // 2 — Roll for new events
        TrySpawnEvents();

        // 3 — Recalculate all modifiers
        RecalculateModifiers();
    }

    // ─────────────────────────────────────────
    void TickActiveEvents()
    {
        List<ActiveRandomEvent> toRemove = new List<ActiveRandomEvent>();

        foreach (var evt in activeEvents)
        {
            evt.monthsRemaining--;
            if (evt.monthsRemaining <= 0)
            {
                toRemove.Add(evt);
                NewsManager.Post(CategoryForType(evt.type),
                    $"{evt.title} has ended.");
            }
        }

        foreach (var evt in toRemove)
            activeEvents.Remove(evt);
    }

    // ─────────────────────────────────────────
    void TrySpawnEvents()
    {
        int happiness = GameManager.Instance.happiness;
        int crime = GameManager.Instance.crime;
        int healthcare = GameManager.Instance.healthcare;
        int pop = GameManager.Instance.population;
        Season season = GameClock.Instance.GetCurrentSeason();

        float evtMod = PolicyManager.Instance != null
            ? PolicyManager.Instance.RandomEventChanceModifier : 1f;

        // TRADE BOOM — happiness above average
        if (!HasActive(RandomEventType.TradeBoom) &&
            happiness > 50 &&
            Random.value < tradeBoomChance * evtMod)
            Spawn(RandomEventType.TradeBoom, 3,
                "Trade Boom! Production boosted +20% for 3 months.");

        // MARKET CRASH
        if (!HasActive(RandomEventType.MarketCrash) &&
            Random.value < marketCrashChance * evtMod)
            Spawn(RandomEventType.MarketCrash, 2,
                "Market Crash! Tax income reduced -30% for 2 months.");

        // RESOURCE DISCOVERY — has storage
        if (!HasActive(RandomEventType.ResourceDiscovery) &&
            pop > 100 &&
            Random.value < resourceDiscoveryChance * evtMod)
            SpawnResourceDiscovery();

        // INTERNATIONAL AID — player struggling
        if (!HasActive(RandomEventType.InternationalAid) &&
            happiness < 40 &&
            Random.value < internationalAidChance * evtMod)
            SpawnInternationalAid();

        // SANCTIONS
        if (!HasActive(RandomEventType.Sanctions) &&
            Random.value < sanctionsChance * evtMod)
            Spawn(RandomEventType.Sanctions, 3,
                "International Sanctions! Tax income reduced -20% for 3 months.");

        // DISEASE OUTBREAK — low healthcare
        if (!HasActive(RandomEventType.DiseaseOutbreak) &&
            healthcare < 40 && pop > 150 &&
            Random.value < diseaseOutbreakChance * evtMod)
            Spawn(RandomEventType.DiseaseOutbreak, 2,
                "Disease Outbreak! Low healthcare is spreading illness across the city.");

        // SERIAL KILLER — some existing crime
        if (!HasActive(RandomEventType.SerialKiller) &&
            crime > 30 &&
            Random.value < serialKillerChance * evtMod)
            Spawn(RandomEventType.SerialKiller, 2,
                "Serial Killer Active! Crime surging. Assign more police immediately.");

        // DROUGHT — Summer only
        if (!HasActive(RandomEventType.Drought) &&
            season == Season.Summer &&
            Random.value < droughtChance * evtMod)
            Spawn(RandomEventType.Drought, 3,
                "Drought! Severe heat is cutting farming output -50% for 3 months.");

        // LABOR DISPUTE — unhappy workers
        if (!HasActive(RandomEventType.LaborDispute) &&
            happiness < 35 && pop > 100 &&
            Random.value < laborDisputeChance * evtMod)
            Spawn(RandomEventType.LaborDispute, 1,
                "Labor Dispute! Workers demanding better conditions. Production -25% for 1 month.");
    }

    // ─────────────────────────────────────────
    // SPECIAL SPAWN HANDLERS
    // ─────────────────────────────────────────
    void SpawnResourceDiscovery()
    {
        // Find a random storage building and add resources
        List<TileObject> storages = new List<TileObject>();
        foreach (var tile in GridManager.Instance.allTiles)
        {
            if (tile == null || tile.CurrentBuilding == null) continue;
            if (tile.CurrentBuilding.GetComponent<StorageBuilding>() != null)
                storages.Add(tile);
        }

        if (storages.Count == 0) return;

        TileObject target = storages[Random.Range(0, storages.Count)];
        BuildingInventory inv = target.buildingInventory;
        if (inv == null) return;

        // Pick a random resource the storage accepts
        ResourceType[] options = { ResourceType.Iron, ResourceType.Oil,
                                   ResourceType.Minerals, ResourceType.Wood };
        foreach (var res in options)
        {
            if (inv.CanStore(res))
            {
                int amount = Random.Range(200, 500);
                inv.AddResource(res, amount);
                NewsManager.Post(NewsCategory.Economic,
                    $"Resource Discovery! {amount} {res} found and delivered to storage.");
                return;
            }
        }
    }

    void SpawnInternationalAid()
    {
        int gift = Random.Range(300, 800);
        GameManager.Instance.money += gift;
        Spawn(RandomEventType.InternationalAid, 1,
            $"International Aid! Received ${gift} in emergency relief funds.");
    }

    // ─────────────────────────────────────────
    // RECALCULATE ALL MODIFIERS
    // Called after event list changes.
    // Other systems multiply their output by
    // these values each tick.
    // ─────────────────────────────────────────
    void RecalculateModifiers()
    {
        ProductionModifier = 1f;
        TaxModifier = 1f;
        FarmingModifier = 1f;
        CrimeModifier = 0;
        HappinessModifier = 0;

        foreach (var evt in activeEvents)
        {
            switch (evt.type)
            {
                case RandomEventType.TradeBoom:
                    ProductionModifier += 0.20f;
                    break;
                case RandomEventType.MarketCrash:
                    TaxModifier -= 0.30f;
                    break;
                case RandomEventType.Sanctions:
                    TaxModifier -= 0.20f;
                    break;
                case RandomEventType.DiseaseOutbreak:
                    HappinessModifier -= 10;
                    break;
                case RandomEventType.SerialKiller:
                    CrimeModifier += 20;
                    break;
                case RandomEventType.Drought:
                    FarmingModifier -= 0.50f;
                    break;
                case RandomEventType.LaborDispute:
                    ProductionModifier -= 0.25f;
                    break;
            }
        }

        // Clamp modifiers to sane ranges
        ProductionModifier = Mathf.Clamp(ProductionModifier, 0.1f, 2f);
        TaxModifier = Mathf.Clamp(TaxModifier, 0.1f, 2f);
        FarmingModifier = Mathf.Clamp(FarmingModifier, 0.1f, 2f);
    }

    // ─────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────
    void Spawn(RandomEventType type, int months, string message)
    {
        activeEvents.Add(new ActiveRandomEvent(type, months,
            message.Split('!')[0])); // title = text before first !
        NewsManager.Post(CategoryForType(type), message);
    }

    bool HasActive(RandomEventType type)
    {
        foreach (var e in activeEvents)
            if (e.type == type) return true;
        return false;
    }

    NewsCategory CategoryForType(RandomEventType type)
    {
        switch (type)
        {
            case RandomEventType.TradeBoom:
            case RandomEventType.MarketCrash:
            case RandomEventType.ResourceDiscovery:
            case RandomEventType.InternationalAid:
                return NewsCategory.Economic;
            case RandomEventType.Sanctions:
                return NewsCategory.Political;
            case RandomEventType.DiseaseOutbreak:
                return NewsCategory.Health;
            case RandomEventType.SerialKiller:
                return NewsCategory.Crime;
            case RandomEventType.Drought:
            case RandomEventType.LaborDispute:
                return NewsCategory.Social;
            default:
                return NewsCategory.Economic;
        }
    }
}