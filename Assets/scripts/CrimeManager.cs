using UnityEngine;
using System.Collections.Generic;

public class CrimeManager : MonoBehaviour
{
    public static CrimeManager Instance;

    [Range(0, 100)]
    public float globalCrime;

    public int policeCoverageRadius = 6;

    [Header("Theft")]
    public float theftCrimeThreshold = 70f;  // crime must be above this
    public int theftAmountPerTick = 5;    // resources stolen per tick
    // Cooldown so theft doesn't fire every tick
    private int theftCooldown = 0;

    void Awake() { Instance = this; }

    public void RunCrimeTick()
    {
        if (GridManager.Instance == null || GridManager.Instance.allTiles == null ||
            GameManager.Instance == null)
            return;

        List<TileObject> allTiles = GridManager.Instance.allTiles;

        int totalCrime = 0;
        int districtCount = 0;

        // RESET COVERAGE
        foreach (TileObject tile in allTiles)
        {
            if (tile == null || tile.tileData == null) continue;
            tile.tileData.hasPoliceCoverage = false;
        }

        // APPLY POLICE COVERAGE — funded only
        foreach (TileObject tile in allTiles)
        {
            if (tile == null || tile.tileData == null) continue;
            if (tile.tileData.buildingType != BuildingType.PoliceStation) continue;
            if (!tile.isFunded) continue;
            ApplyPoliceCoverage(tile);
        }

        int eventCrimeModifier = RandomEventManager.Instance != null
            ? RandomEventManager.Instance.CrimeModifier : 0;
        int policyCrimeModifier = PolicyManager.Instance != null
            ? PolicyManager.Instance.CrimeModifier : 0;

        // CALCULATE CRIME PER DISTRICT TILE
        foreach (TileObject tile in allTiles)
        {
            if (tile == null || tile.tileData == null) continue;
            if (!tile.HasDistrict()) continue;

            districtCount++;

            // Use the owning city's happiness where available, not the capital's
            // national value, so district safety reflects local conditions.
            int localHappiness = GameManager.Instance.happiness;
            if (CityManager.Instance != null)
            {
                CityData owner = CityManager.Instance.GetCityAt(tile.gridPosition);
                if (owner != null) localHappiness = owner.happiness;
            }

            float crime = Mathf.Max(0, 50 - localHappiness) * 0.8f;
            if (!tile.tileData.hasPoliceCoverage)
                crime += 25f;

            // Apply modifiers to district values so the city and national
            // averages use the same scale as the crime map.
            crime += eventCrimeModifier + policyCrimeModifier;
            tile.tileData.crimeLevel = Mathf.Clamp(crime, 0f, 100f);
            totalCrime += Mathf.RoundToInt(tile.tileData.crimeLevel);
        }

        float prevCrime = globalCrime;
        globalCrime = districtCount > 0
            ? (float)totalCrime / districtCount
            : 0f;

        // NEWS: crime crossing thresholds
        if (prevCrime < 70f && globalCrime >= 70f)
            NewsManager.Post(NewsCategory.Crime,
                "Crime Wave! Crime has reached dangerous levels. Resources at risk.");
        else if (prevCrime >= 70f && globalCrime < 70f)
            NewsManager.Post(NewsCategory.Crime,
                "Crime reducing. Police presence improving city safety.");

        // THEFT at high crime
        RunTheftTick(allTiles);
    }

    // ─────────────────────────────────────────
    // THEFT
    // At high crime, random resources are stolen
    // from a random building each few ticks.
    // ─────────────────────────────────────────
    void RunTheftTick(List<TileObject> allTiles)
    {
        if (globalCrime < theftCrimeThreshold) return;

        if (theftCooldown > 0)
        {
            theftCooldown--;
            return;
        }

        // Pick a random building with resources
        List<TileObject> candidates = new List<TileObject>();
        foreach (var tile in allTiles)
        {
            if (tile == null || tile.CurrentBuilding == null) continue;
            BuildingInventory inv = tile.buildingInventory;
            if (inv == null) continue;
            if (inv.GetTotalStored() > 0)
                candidates.Add(tile);
        }

        if (candidates.Count == 0) return;

        TileObject target = candidates[Random.Range(0, candidates.Count)];
        BuildingInventory targetInv = target.buildingInventory;

        // Steal from a random allowed resource
        foreach (var resource in targetInv.allowedResources)
        {
            if (resource == ResourceType.None) continue;
            int available = targetInv.GetResource(resource);
            if (available <= 0) continue;

            int stolen = Mathf.Min(theftAmountPerTick, available);
            targetInv.RemoveResource(resource, stolen);

            NewsManager.Post(NewsCategory.Crime,
                $"Theft! {stolen} {resource} stolen from " +
                $"{target.tileData.buildingType}. Crime is out of control.");
            break;
        }

        // Cooldown: 3 ticks between thefts
        theftCooldown = 3;
    }

    void ApplyPoliceCoverage(TileObject station)
    {
        GridManager grid = GridManager.Instance;

        for (int x = -policeCoverageRadius; x <= policeCoverageRadius; x++)
        {
            for (int z = -policeCoverageRadius; z <= policeCoverageRadius; z++)
            {
                TileObject tile = grid.GetTileAt(
                    station.gridPosition.x + x,
                    station.gridPosition.y + z);

                if (tile == null || tile.tileData == null) continue;

                float dist = Vector2Int.Distance(
                    station.gridPosition, tile.gridPosition);
                if (dist <= policeCoverageRadius)
                    tile.tileData.hasPoliceCoverage = true;
            }
        }
    }
}