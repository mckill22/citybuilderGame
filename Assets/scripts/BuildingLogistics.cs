using UnityEngine;
using System.Collections.Generic;

public class BuildingLogistics : MonoBehaviour
{
    public List<LogisticsRoute> routes = new List<LogisticsRoute>();

    /// <summary>
    /// Adds a logistics route. Validates road connectivity before adding.
    /// Returns true if route was created, false if rejected.
    /// </summary>
    public bool AddRoute(
        TileObject source,
        TileObject destination,
        ResourceType resource,
        int trucks
    )
    {
        // Validate road connectivity
        if (!GridManager.Instance.IsConnectedByRoad(source, destination))
        {
            Debug.LogWarning($"Cannot create route: no road connection between {source.tileData.buildingType} and {destination.tileData.buildingType}");
            return false;
        }
        // Validate destination inventory accepts resource
        BuildingInventory destInv =
            destination.buildingInventory;

        if (destInv == null || !destInv.CanStore(resource))
        {
            Debug.LogWarning(
                $"Destination cannot store {resource}"
            );

            return false;
        }

        LogisticsRoute route = new LogisticsRoute();
        route.source = source;
        route.destination = destination;
        route.resourceType = resource;
        route.assignedTrucks = trucks;
        route.active = true;

        routes.Add(route);
        return true;
    }

    /// <summary>
    /// Removes a route and notifies LogisticsManager to despawn the truck.
    /// </summary>
    public void RemoveRoute(LogisticsRoute route)
    {
        if (!routes.Contains(route)) return;

        route.active = false;
        routes.Remove(route);

        // 🔥 immediate truck pool refresh
        GameManager.Instance.UpdateGlobalResourceTotals();

        // optional: notify system (if you still use truck objects)
        if (LogisticsManager.Instance != null)
            LogisticsManager.Instance.OnRouteDeleted(route);
    }
}