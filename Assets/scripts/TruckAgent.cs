using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TruckAgent : MonoBehaviour
{
    [Header("Route")]
    public LogisticsRoute route;

    [Header("Movement")]
    public float speed = 5f;
    private List<TileObject> path = new List<TileObject>();
    private int pathIndex = 0;

    [Header("Cargo")]
    public ResourceType cargoType;
    public int cargoAmount;
    public bool loaded = false;

    private enum TruckState { Idle, DrivingToSource, Loading, DrivingToDestination, Unloading }
    private TruckState state = TruckState.Idle;

    private GridManager grid;
    private readonly Queue<TileObject> pathSearchQueue = new Queue<TileObject>();
    private readonly Dictionary<TileObject, TileObject> pathSearchParents =
        new Dictionary<TileObject, TileObject>();
    private bool isActive = false;
    private bool driveSuccess;
    public void Init(LogisticsRoute assignedRoute, GridManager gm)
    {
        route = assignedRoute;
        grid = gm;
        cargoType = route.resourceType;
        isActive = true;
        StartCoroutine(TruckLoop());
    }
    bool IsPaused()
    {
        return GameClock.Instance != null &&
               GameClock.Instance.IsPaused;
    }
    IEnumerator TruckLoop()
    {
        while (isActive && route != null && route.active)
        {
            // --- Drive to Source ---
            state = TruckState.DrivingToSource;
            yield return StartCoroutine(DriveToTile(route.source));
            bool reachedSource = driveSuccess;

            if (route == null || !route.active)
                break;

            // Temporary road block or disconnected network: retry later instead
            // of silently terminating this truck's route coroutine.
            if (!reachedSource)
            {
                yield return new WaitForSeconds(2f);
                continue;
            }

            // --- Load ---
            state = TruckState.Loading;
            yield return StartCoroutine(LoadCargo());

            if (cargoAmount <= 0)
            {
                // Nothing to pick up, wait a moment and retry
                yield return new WaitForSeconds(2f);
                continue;
            }

            // --- Drive to Destination ---
            state = TruckState.DrivingToDestination;
            bool reachedDest = false;
            while (isActive && route != null && route.active &&
                   cargoAmount > 0 && !reachedDest)
            {
                yield return StartCoroutine(DriveToTile(route.destination));
                reachedDest = driveSuccess;
                if (!reachedDest && route != null && route.active)
                    yield return new WaitForSeconds(2f);
            }

            if (!reachedDest || route == null || !route.active)
                break;

            // --- Unload ---
            state = TruckState.Unloading;
            yield return StartCoroutine(UnloadCargo());

            // Small pause before next trip
            yield return new WaitForSeconds(0.5f);
        }

        state = TruckState.Idle;
    }

    IEnumerator DriveToTile(TileObject target)
    {
        driveSuccess = false;

        if (target == null || grid == null) yield break;

        TileObject startRoadTile = FindNearestRoadTile(transform.position);
        List<TileObject> newPath = FindRoadPath(startRoadTile, target);

        if (newPath == null || newPath.Count == 0)
            yield break;

        path = newPath;
        pathIndex = 0;

        while (pathIndex < path.Count)
        {
            TileObject nextTile = path[pathIndex];
            if (nextTile == null) yield break;

            // If the road network changes during a trip, fail this attempt so
            // the outer route loop can search again instead of crossing a gap.
            if (pathIndex > 0 && nextTile != target && !nextTile.HasRoad())
                yield break;

            Vector3 targetPos = nextTile.transform.position + Vector3.up * 0.05f;

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                while (IsPaused())
                    yield return null;

                Vector3 dir = (targetPos - transform.position).normalized;

                if (dir != Vector3.zero)
                    transform.rotation = Quaternion.Lerp(
                        transform.rotation,
                        Quaternion.LookRotation(dir),
                        Time.deltaTime * 10f
                    );

                float weatherMod = WeatherSystem.Instance != null
                    ? WeatherSystem.Instance.MovementModifier : 1f;

                // Gas shortage slows trucks to 40%
                float gasMod = (GameManager.Instance != null &&
                    GameManager.Instance.isGasShortage) ? 0.4f : 1f;

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    speed * weatherMod * gasMod * Time.deltaTime
                );

                yield return null;
            }

            transform.position = targetPos;
            pathIndex++;
        }

        driveSuccess = true;
    }

    IEnumerator LoadCargo()
    {
        if (route == null || route.source == null) yield break;

        BuildingInventory inv = route.source.buildingInventory;
        if (inv == null) yield break;

        // Check source building worker ratio.
        // No workers = nothing loaded. Understaffed = slower loading.
        float workRatio = 1f;
        if (GameManager.Instance != null)
        {
            int demand = GameManager.Instance.GetWorkerCost(route.source);
            if (demand > 0)
                workRatio = GameManager.Instance.GetLogisticsWorkRatio(route.source);
        }

        // No workers at all — truck arrives but loads nothing
        if (workRatio <= 0f)
        {
            cargoAmount = 0;
            loaded = false;
            // Brief pause before retry
            float w = 0f;
            while (w < 1f) { if (!IsPaused()) w += Time.deltaTime; yield return null; }
            yield break;
        }

        int maxCarry = route.assignedTrucks * LogisticsManager.Instance.resourcesPerTruck;
        int available = inv.GetResource(cargoType);
        int toLoad = Mathf.Min(maxCarry, available);

        if (toLoad > 0 && inv.RemoveResource(cargoType, toLoad))
        {
            cargoAmount = toLoad;
            loaded = true;
        }
        else
        {
            cargoAmount = 0;
            loaded = false;
        }

        // Loading time scales with worker ratio:
        // fully staffed = 0.3s, half staffed = ~1.5s, 25% staffed = ~2.7s
        float loadTime = Mathf.Lerp(3f, 0.3f, workRatio);
        float t = 0f;
        while (t < loadTime)
        {
            if (!IsPaused()) t += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator UnloadCargo()
    {
        if (route == null || route.destination == null || cargoAmount <= 0) yield break;

        BuildingInventory inv = route.destination.buildingInventory;
        if (inv == null) { cargoAmount = 0; loaded = false; yield break; }

        int space = inv.GetRemainingCapacity();
        int toUnload = Mathf.Min(cargoAmount, space);

        if (toUnload > 0)
            inv.AddResource(cargoType, toUnload);

        cargoAmount = 0;
        loaded = false;

        yield return new WaitForSeconds(0.3f); // brief unloading pause
    }

    // BFS road pathfinding
    List<TileObject> FindRoadPath(TileObject start, TileObject goal)
    {
        if (start == null || goal == null) return null;

        // We allow the last step to be the goal tile (building tile) even if not a road
        Queue<TileObject> queue = pathSearchQueue;
        Dictionary<TileObject, TileObject> cameFrom = pathSearchParents;
        queue.Clear();
        cameFrom.Clear();

        queue.Enqueue(start);
        cameFrom[start] = null;

        TileObject found = null;

        while (queue.Count > 0)
        {
            TileObject current = queue.Dequeue();

            if (current == goal || IsAdjacentTo(current, goal))
            {
                found = current;
                break;
            }

            Vector2Int p = current.gridPosition;
            TryVisitRoadNeighbor(p.x + 1, p.y, current, goal, queue, cameFrom);
            TryVisitRoadNeighbor(p.x - 1, p.y, current, goal, queue, cameFrom);
            TryVisitRoadNeighbor(p.x, p.y + 1, current, goal, queue, cameFrom);
            TryVisitRoadNeighbor(p.x, p.y - 1, current, goal, queue, cameFrom);
        }

        if (found == null) return null;

        // Reconstruct path
        path.Clear();
        TileObject step = found;

        while (step != null)
        {
            path.Add(step);
            cameFrom.TryGetValue(step, out step);
        }

        path.Reverse();

        // Add goal tile at end if we stopped adjacent
        if (path.Count > 0 && path[path.Count - 1] != goal)
            path.Add(goal);

        return path;
    }

    bool IsAdjacentTo(TileObject a, TileObject b)
    {
        int dx = Mathf.Abs(a.gridPosition.x - b.gridPosition.x);
        int dz = Mathf.Abs(a.gridPosition.y - b.gridPosition.y);
        return (dx + dz) == 1;
    }

    void TryVisitRoadNeighbor(
        int x, int z, TileObject parent, TileObject goal,
        Queue<TileObject> queue, Dictionary<TileObject, TileObject> cameFrom)
    {
        TileObject neighbor = grid.GetTileAt(x, z);
        if (neighbor == null || cameFrom.ContainsKey(neighbor)) return;
        if (!neighbor.HasRoad() && neighbor != goal) return;

        cameFrom[neighbor] = parent;
        queue.Enqueue(neighbor);
    }

    TileObject FindNearestRoadTile(Vector3 worldPos)
    {
        // Check a radius of tiles to find the closest road tile
        float minDist = float.MaxValue;
        TileObject nearest = null;

        for (int x = 0; x < grid.width; x++)
        {
            for (int z = 0; z < grid.height; z++)
            {
                TileObject t = grid.GetTileAt(x, z);
                if (t == null || !t.HasRoad()) continue;
                float d = Vector3.Distance(worldPos, t.transform.position);
                if (d < minDist) { minDist = d; nearest = t; }
            }
        }

        return nearest;
    }

    public void StopTruck()
    {
        isActive = false;
        StopAllCoroutines();
    }
}