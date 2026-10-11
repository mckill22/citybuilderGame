using UnityEngine;
using System.Collections.Generic;

public class LogisticsManager : MonoBehaviour
{
    public static LogisticsManager Instance;

    public int resourcesPerTruck = 5;

    [Header("Truck Visuals")]
    public GameObject truckPrefab;

    private Dictionary<LogisticsRoute, TruckAgent> activeTrucks =
        new Dictionary<LogisticsRoute, TruckAgent>();

    void Awake()
    {
        Instance = this;
        Debug.Log("LogisticsManager Active");
    }

    public void RunLogisticsTick()
    {
        // Guard: grid may not be ready yet on first ticks
        if (GridManager.Instance == null) return;

        GridManager grid = GridManager.Instance;
        List<TileObject> allTiles = grid.allTiles;

        foreach (var tile in allTiles)
        {
            if (tile == null || tile.CurrentBuilding == null) continue;

            BuildingLogistics logistics =
                tile.CurrentBuilding.GetComponent<BuildingLogistics>();
            if (logistics == null) continue;

            foreach (var route in logistics.routes)
            {
                // Only spawn trucks for active routes.
                // Don't destroy trucks for inactive routes here —
                // inactive is temporary (worker shortage, validation).
                // CleanupOrphanedTrucks handles truly deleted routes.
                if (!route.active) continue;

                if (!activeTrucks.ContainsKey(route))
                    SpawnTruckForRoute(route, grid);
            }
        }

        CleanupOrphanedTrucks();
    }

    void SpawnTruckForRoute(LogisticsRoute route, GridManager grid)
    {
        if (truckPrefab == null || route.source == null) return;

        Vector3 spawnPos = route.source.transform.position + Vector3.up * 0.15f;
        GameObject truckGO = Instantiate(truckPrefab, spawnPos, Quaternion.identity);

        TruckAgent agent = truckGO.GetComponent<TruckAgent>();
        if (agent == null) { Destroy(truckGO); return; }

        agent.Init(route, grid);
        activeTrucks[route] = agent;

        Debug.Log($"Truck spawned: {route.source.tileData.buildingType} → {route.destination.tileData.buildingType}");
    }

    public void RemoveTruckForRoute(LogisticsRoute route)
    {
        if (activeTrucks.TryGetValue(route, out TruckAgent agent))
        {
            if (agent != null) { agent.StopTruck(); Destroy(agent.gameObject); }
            activeTrucks.Remove(route);
        }
    }

    void CleanupOrphanedTrucks()
    {
        // Build a set of all routes currently registered
        // across all buildings so we know which are truly gone
        HashSet<LogisticsRoute> registeredRoutes = new HashSet<LogisticsRoute>();

        if (GridManager.Instance != null)
        {
            foreach (var tile in GridManager.Instance.allTiles)
            {
                if (tile == null || tile.CurrentBuilding == null) continue;
                BuildingLogistics logistics =
                    tile.CurrentBuilding.GetComponent<BuildingLogistics>();
                if (logistics == null) continue;
                foreach (var route in logistics.routes)
                    if (route != null) registeredRoutes.Add(route);
            }
        }

        List<LogisticsRoute> toRemove = new List<LogisticsRoute>();

        foreach (var kvp in activeTrucks)
        {
            // Remove truck if:
            // 1. Route is no longer registered in any building (truly orphaned)
            // 2. TruckAgent GameObject has been destroyed
            bool routeGone = kvp.Key == null || !registeredRoutes.Contains(kvp.Key);
            bool agentGone = kvp.Value == null;

            if (routeGone || agentGone)
                toRemove.Add(kvp.Key);
        }

        foreach (var key in toRemove)
        {
            if (activeTrucks.TryGetValue(key, out TruckAgent agent) && agent != null)
            {
                agent.StopTruck();
                Destroy(agent.gameObject);
            }
            activeTrucks.Remove(key);
        }
    }

    public void OnRouteDeleted(LogisticsRoute route)
    {
        RemoveTruckForRoute(route);
    }

    public void ValidateAllRoutes()
    {
        if (GridManager.Instance == null) return;

        List<TileObject> allTiles = GridManager.Instance.allTiles;

        foreach (var tile in allTiles)
        {
            if (tile == null || tile.CurrentBuilding == null) continue;

            BuildingLogistics logistics =
                tile.CurrentBuilding.GetComponent<BuildingLogistics>();
            if (logistics == null) continue;

            List<LogisticsRoute> invalidRoutes = new List<LogisticsRoute>();

            foreach (var route in logistics.routes)
            {
                if (route == null || route.source == null || route.destination == null)
                {
                    invalidRoutes.Add(route);
                    continue;
                }

                if (route.destination.CurrentBuilding == null ||
                    route.source.CurrentBuilding == null)
                {
                    invalidRoutes.Add(route);
                    continue;
                }

                if (!GridManager.Instance.IsConnectedByRoad(route.source, route.destination))
                    invalidRoutes.Add(route);
            }

            foreach (var route in invalidRoutes)
            {
                logistics.RemoveRoute(route);
                Debug.Log($"Route invalidated: {route.source?.tileData.buildingType} → {route.destination?.tileData.buildingType}");
            }
        }
    }
}