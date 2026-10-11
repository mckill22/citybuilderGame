using UnityEngine;

// =============================================
// SEASONAL COLOR MANAGER
// Updates all terrain tile colors when the
// season changes or weather changes.
// Layered on top of base terrain colors —
// season adds a subtle tint, snow adds white.
//
// Unity setup:
//   Attach to any persistent GameObject.
//   No Inspector wiring needed — reads from
//   GameClock and WeatherSystem automatically.
// =============================================
public class SeasonalColorManager : MonoBehaviour
{
    public static SeasonalColorManager Instance;

    [Header("Season Tint Strength (0-1)")]
    [Range(0f, 1f)]
    public float seasonTintStrength = 0.25f;

    [Header("Snow Tint Strength (0-1)")]
    [Range(0f, 1f)]
    public float snowTintStrength = 0.45f;

    [Header("Season Colors")]
    public Color springTint = new Color(0.6f, 1.0f, 0.5f); // bright fresh green
    public Color summerTint = new Color(1.0f, 0.95f, 0.6f); // warm yellow
    public Color autumnTint = new Color(1.0f, 0.65f, 0.2f); // deep orange
    public Color winterTint = new Color(0.75f, 0.85f, 1.0f); // cold blue-white

    [Header("Weather Tints")]
    public Color snowOverlay = new Color(0.92f, 0.96f, 1.0f); // white-blue snow
    public Color rainOverlay = new Color(0.7f, 0.75f, 0.8f);  // grey-blue rain

    private Season lastSeason = Season.Spring;
    private WeatherType lastWeather = WeatherType.Clear;

    void Awake() { Instance = this; }

    void Start()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnMonthEnd += OnMonthEnd;

        // Apply initial colors
        RefreshAllTileColors();
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnMonthEnd -= OnMonthEnd;
    }

    void OnMonthEnd()
    {
        if (GameClock.Instance == null) return;
        Season current = GameClock.Instance.GetCurrentSeason();
        if (current != lastSeason)
        {
            lastSeason = current;
            RefreshAllTileColors();
        }
    }

    // Called by WeatherSystem when weather changes
    public void OnWeatherChanged(WeatherType weather)
    {
        lastWeather = weather;
        RefreshAllTileColors();
    }

    // =========================================
    // CORE REFRESH
    // =========================================
    public void RefreshAllTileColors()
    {
        if (GridManager.Instance == null) return;

        foreach (var tile in GridManager.Instance.allTiles)
        {
            if (tile == null || tile.tileData == null) continue;
            ApplyTileColor(tile);
        }
    }

    public void ApplyTileColor(TileObject tile)
    {
        if (tile == null || tile.tileData == null) return;

        // 1 — Get base terrain color
        Color baseColor = GetBaseTerrainColor(tile.tileData.terrainType);

        // 2 — Apply season tint (skip water)
        if (tile.tileData.terrainType != TerrainType.Water)
        {
            Color season = GetSeasonTint();
            baseColor = Color.Lerp(baseColor, season, seasonTintStrength);
        }

        // 3 — Apply weather overlay
        if (lastWeather == WeatherType.Snow &&
            tile.tileData.terrainType != TerrainType.Water)
        {
            baseColor = Color.Lerp(baseColor, snowOverlay, snowTintStrength);
        }
        else if (lastWeather == WeatherType.Rain &&
                 tile.tileData.terrainType != TerrainType.Water)
        {
            baseColor = Color.Lerp(baseColor, rainOverlay, 0.15f);
        }

        // 4 — Apply city border tint on top if tile is claimed
        if (tile.tileData.cityId >= 0)
        {
            Color cityTint = GetCityTint(tile.tileData.cityId);
            baseColor = Color.Lerp(baseColor, cityTint, 0.2f);
        }

        // Write to renderer
        Renderer rend = tile.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = baseColor;
    }

    // =========================================
    // HELPERS
    // =========================================
    Color GetBaseTerrainColor(TerrainType terrain)
    {
        switch (terrain)
        {
            case TerrainType.Clear: return new Color(0.3f, 0.8f, 0.3f);
            case TerrainType.Forest: return new Color(0.1f, 0.5f, 0.1f);
            case TerrainType.Water: return new Color(0.1f, 0.4f, 0.8f);
            case TerrainType.Mountain: return new Color(0.5f, 0.5f, 0.5f);
            case TerrainType.Sand: return new Color(0.9f, 0.8f, 0.4f);
            default: return Color.white;
        }
    }

    Color GetSeasonTint()
    {
        if (GameClock.Instance == null) return Color.white;
        switch (GameClock.Instance.GetCurrentSeason())
        {
            case Season.Spring: return springTint;
            case Season.Summer: return summerTint;
            case Season.Autumn: return autumnTint;
            case Season.Winter: return winterTint;
            default: return Color.white;
        }
    }

    Color GetCityTint(int cityId)
    {
        switch (cityId)
        {
            case 0: return new Color(0.9f, 0.9f, 1.0f);
            case 1: return new Color(1.0f, 0.95f, 0.8f);
            case 2: return new Color(0.9f, 1.0f, 0.9f);
            case 3: return new Color(1.0f, 0.88f, 0.88f);
            default: return new Color(0.95f, 0.95f, 0.95f);
        }
    }
}