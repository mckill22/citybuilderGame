using UnityEngine;

// =============================================
// WEATHER SYSTEM
// Reads GameClock season each month and spawns
// weather particle effects. Exposes modifiers
// that GameManager reads each tick.
//
// Unity setup:
//   rainParticles   — ParticleSystem prefab
//   snowParticles   — ParticleSystem prefab
//   dustParticles   — ParticleSystem prefab (sandstorm)
//   Attach to a persistent GameObject.
//   Set particleHeight to above your tallest
//   building (e.g. 25f).
//
// Particle system tips:
//   Rain:  Cone emitter, Y velocity -20, lifetime 1s
//   Snow:  Sphere emitter, Y velocity -3, lifetime 4s
//   Dust:  Box emitter, X velocity 5, alpha 0.3
// =============================================
public class WeatherSystem : MonoBehaviour
{
    public static WeatherSystem Instance;

    [Header("Particle Prefabs")]
    public GameObject rainPrefab;
    public GameObject snowPrefab;
    public GameObject dustPrefab;

    [Header("Settings")]
    public float particleHeight = 25f;
    public float weatherChangeChance = 0.3f;
    [Header("Map")]
    public Transform ground;
    // Current weather state
    public WeatherType currentWeather { get; private set; }
        = WeatherType.Clear;

    // ─────────────────────────────────────────
    // MODIFIERS — read by GameManager
    // ─────────────────────────────────────────
    public float FarmingModifier { get; private set; } = 1f;
    public float PowerModifier { get; private set; } = 1f;
    public float MovementModifier { get; private set; } = 1f;

    private GameObject activeParticles;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnMonthEnd += OnMonthEnd;
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnMonthEnd -= OnMonthEnd;
    }

    // =========================================
    // MONTHLY UPDATE
    // =========================================
    void OnMonthEnd()
    {
        Season season = GameClock.Instance.GetCurrentSeason();
        RollWeather(season);
    }

    void RollWeather(Season season)
    {
        WeatherType newWeather = WeatherType.Clear;

        float roll = Random.value;

        switch (season)
        {
            case Season.Spring:
                if (roll < 0.5f)
                    newWeather = WeatherType.Rain;
                else
                    newWeather = WeatherType.Clear;
                break;

            case Season.Summer:
                if (roll < 0.15f)
                    newWeather = WeatherType.Dust;
                else if (roll < 0.25f)
                    newWeather = WeatherType.Rain;
                else
                    newWeather = WeatherType.Clear;
                break;

            case Season.Autumn:
                if (roll < 0.4f)
                    newWeather = WeatherType.Rain;
                else
                    newWeather = WeatherType.Clear;
                break;

            case Season.Winter:
                if (roll < 0.55f)
                    newWeather = WeatherType.Snow;
                else if (roll < 0.65f)
                    newWeather = WeatherType.Rain;
                else
                    newWeather = WeatherType.Clear;
                break;
        }

        // Always apply weather so modifiers refresh each month.
        // News only posts when weather actually changes (handled inside SetWeather).
        SetWeather(newWeather);
    }

    // =========================================
    // APPLY WEATHER
    // =========================================
    public void SetWeather(WeatherType weather)
    {
        // Store previous weather before changing it
        WeatherType previousWeather = currentWeather;
        currentWeather = weather;

        // Remove existing particles
        if (activeParticles != null)
        {
            Destroy(activeParticles);
            activeParticles = null;
        }

        Vector3 spawnPos = new Vector3(0f, particleHeight, 0f);
        Quaternion spawnRotation = Quaternion.identity;
        GameObject prefab = null;

        float mapHalfSizeX = 50f;
        float mapHalfSizeZ = 50f;

        if (ground != null)
        {
            // Unity Plane = 10x10 units at scale 1
            mapHalfSizeX = ground.localScale.x * 5f;
            mapHalfSizeZ = ground.localScale.z * 5f;
        }

        Debug.Log($"Weather changed from {previousWeather} to {weather}");

        switch (weather)
        {
            case WeatherType.Rain:
                prefab = rainPrefab;
                spawnRotation = Quaternion.Euler(90f, 90f, -90f);

                FarmingModifier = 1.2f;
                PowerModifier = 0.9f;
                MovementModifier = 0.8f;

                if (previousWeather != WeatherType.Rain)
                {
                    NewsManager.Post(
                        NewsCategory.Social,
                        "Rain falling across the region. Farming output boosted.");
                }
                break;

            case WeatherType.Snow:
                prefab = snowPrefab;
                spawnRotation = Quaternion.Euler(90f, 90f, -90f);

                FarmingModifier = 0.6f;
                PowerModifier = 0.85f;
                MovementModifier = 0.6f;

                if (previousWeather != WeatherType.Snow)
                {
                    NewsManager.Post(
                        NewsCategory.Social,
                        "Snowfall blanketing the city. Farming and movement reduced.");
                }
                break;

            case WeatherType.Dust:
                {
                    prefab = dustPrefab;

                    int edge = Random.Range(0, 4);

                    switch (edge)
                    {
                        // West edge — blows East
                        case 0:
                            spawnPos = new Vector3(
                                -mapHalfSizeX - 2f,
                                2f,
                                Random.Range(-mapHalfSizeZ, mapHalfSizeZ));
                            spawnRotation = Quaternion.Euler(0f, 90f, 0f);
                            break;

                        // East edge — blows West
                        case 1:
                            spawnPos = new Vector3(
                                mapHalfSizeX + 2f,
                                2f,
                                Random.Range(-mapHalfSizeZ, mapHalfSizeZ));
                            spawnRotation = Quaternion.Euler(0f, -90f, 0f);
                            break;

                        // South edge — blows North
                        case 2:
                            spawnPos = new Vector3(
                                Random.Range(-mapHalfSizeX, mapHalfSizeX),
                                2f,
                                -mapHalfSizeZ - 2f);
                            spawnRotation = Quaternion.Euler(0f, 0f, 0f);
                            break;

                        // North edge — blows South
                        case 3:
                            spawnPos = new Vector3(
                                Random.Range(-mapHalfSizeX, mapHalfSizeX),
                                2f,
                                mapHalfSizeZ + 2f);
                            spawnRotation = Quaternion.Euler(0f, 180f, 0f);
                            break;
                    }

                    FarmingModifier = 0.7f;
                    PowerModifier = 0.8f;
                    MovementModifier = 0.7f;

                    if (previousWeather != WeatherType.Dust)
                    {
                        NewsManager.Post(
                            NewsCategory.Social,
                            "Sandstorm warning! Reduced visibility and output.");
                    }
                    break;
                }

            case WeatherType.Clear:
                FarmingModifier = 1f;
                PowerModifier = 1f;
                MovementModifier = 1f;

                if (previousWeather != WeatherType.Clear)
                {
                    NewsManager.Post(
                        NewsCategory.Social,
                        "Skies clearing. Normal conditions restored.");
                }
                break;
        }

        if (prefab != null)
        {
            activeParticles = Instantiate(
                prefab,
                spawnPos,
                spawnRotation);
        }

        // Notify terrain color system
        if (SeasonalColorManager.Instance != null)
            SeasonalColorManager.Instance.OnWeatherChanged(weather);
    }
}

// ─────────────────────────────────────────────
public enum WeatherType
{
    Clear,
    Rain,
    Snow,
    Dust
}