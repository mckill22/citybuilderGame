using UnityEngine;
using System.Collections.Generic;

// Cosmetic vehicle. Road BFS buffers are reused between trips; unreachable
// destinations are retried later rather than recursively searching forever.
public class CarAgent : MonoBehaviour
{
    [Header("Movement")]
    public float baseMoveSpeed = 4f;
    public float rotateSpeed = 240f;

    [Header("Stuck Detection")]
    public float stuckTimeout = 3f;
    public float stuckMinMove = 0.1f;

    private GridManager grid;
    private TileObject currentTile;
    private TileObject destinationTile;
    private readonly List<TileObject> path = new List<TileObject>();
    private readonly Queue<TileObject> searchQueue = new Queue<TileObject>();
    private readonly Dictionary<TileObject, TileObject> cameFrom =
        new Dictionary<TileObject, TileObject>();
    private int pathIndex;
    private Vector3 lastCheckedPos;
    private float stuckTimer;
    private float personalSpeed = 1f;

    private static readonly float[] speedMultipliers = { 0f, 1f, 3f, 8f };

    float SpeedMultiplier
    {
        get
        {
            if (GameClock.Instance == null) return 1f;
            int index = Mathf.Clamp((int)GameClock.Instance.currentSpeed, 0, speedMultipliers.Length - 1);
            return speedMultipliers[index];
        }
    }

    bool IsPaused => GameClock.Instance != null &&
                     GameClock.Instance.currentSpeed == GameClock.GameSpeed.Paused;

    public void InitPath(TileObject start, TileObject target, GridManager gm)
    {
        if (start == null || gm == null) return;
        grid = gm;
        currentTile = start;
        destinationTile = target;
        personalSpeed = Random.Range(0.9f, 1.1f);
        transform.position = start.transform.position + Vector3.up * 0.05f;
        lastCheckedPos = transform.position;
        if (!BuildPath()) ScheduleDestinationRetry(1.5f);
    }

    void Update()
    {
        if (IsPaused || grid == null || pathIndex >= path.Count) return;

        float currentSpeed = baseMoveSpeed * personalSpeed * SpeedMultiplier;
        TileObject nextTile = path[pathIndex];
        if (nextTile == null)
        {
            RecoverFromStuck();
            return;
        }

        // A road may be removed after path creation. Re-plan instead of
        // driving over a disconnected tile; the destination itself needn't be road.
        if (pathIndex > 0 && nextTile != destinationTile && !nextTile.HasRoad())
        {
            if (!BuildPath()) ScheduleDestinationRetry(1.5f);
            return;
        }

        Vector3 targetPos = nextTile.transform.position + Vector3.up * 0.05f;
        Vector3 previousPosition = transform.position;
        transform.position = Vector3.MoveTowards(
            transform.position, targetPos, currentSpeed * Time.deltaTime);

        Vector3 direction = targetPos - previousPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(direction.normalized),
                rotateSpeed * SpeedMultiplier * Time.deltaTime);

        if ((transform.position - targetPos).sqrMagnitude < 0.0025f)
        {
            transform.position = targetPos;
            currentTile = nextTile;
            pathIndex++;
            if (pathIndex >= path.Count)
            {
                PickNewDestination();
                return;
            }
        }

        stuckTimer += Time.deltaTime;
        if (stuckTimer >= stuckTimeout)
        {
            if (Vector3.Distance(transform.position, lastCheckedPos) < stuckMinMove)
                RecoverFromStuck();

            lastCheckedPos = transform.position;
            stuckTimer = 0f;
        }
    }

    void ScheduleDestinationRetry(float delay)
    {
        CancelInvoke(nameof(PickNewDestination));
        Invoke(nameof(PickNewDestination), delay);
    }

    void PickNewDestination()
    {
        if (grid == null) return;
        List<TileObject> districts = grid.districtTiles;
        if (districts == null || districts.Count < 2)
        {
            ScheduleDestinationRetry(2f);
            return;
        }

        for (int attempt = 0; attempt < 4; attempt++)
        {
            TileObject candidate = districts[Random.Range(0, districts.Count)];
            if (candidate == null || candidate == currentTile) continue;
            destinationTile = candidate;
            if (BuildPath()) return;
        }

        ScheduleDestinationRetry(1.5f);
    }

    bool BuildPath()
    {
        path.Clear();
        pathIndex = 0;
        if (grid == null || currentTile == null || destinationTile == null)
            return false;

        searchQueue.Clear();
        cameFrom.Clear();
        searchQueue.Enqueue(currentTile);
        cameFrom[currentTile] = null;
        TileObject found = null;

        while (searchQueue.Count > 0)
        {
            TileObject current = searchQueue.Dequeue();
            if (current == destinationTile || IsAdjacentTo(current, destinationTile))
            {
                found = current;
                break;
            }

            Vector2Int p = current.gridPosition;
            TryVisitRoad(p.x + 1, p.y, current);
            TryVisitRoad(p.x - 1, p.y, current);
            TryVisitRoad(p.x, p.y + 1, current);
            TryVisitRoad(p.x, p.y - 1, current);
        }

        if (found == null) return false;

        TileObject step = found;
        while (step != null)
        {
            path.Add(step);
            if (!cameFrom.TryGetValue(step, out step))
            {
                path.Clear();
                return false;
            }
        }
        path.Reverse();

        if (path.Count > 0 && path[path.Count - 1] != destinationTile)
            path.Add(destinationTile);

        lastCheckedPos = transform.position;
        stuckTimer = 0f;
        return path.Count > 0;
    }

    void TryVisitRoad(int x, int z, TileObject parent)
    {
        TileObject next = grid.GetTileAt(x, z);
        if (next == null || cameFrom.ContainsKey(next) || !next.HasRoad()) return;
        cameFrom[next] = parent;
        searchQueue.Enqueue(next);
    }

    void RecoverFromStuck()
    {
        TileObject nearest = FindNearestRoadTile();
        if (nearest == null)
        {
            path.Clear();
            pathIndex = 0;
            ScheduleDestinationRetry(1.5f);
            return;
        }

        currentTile = nearest;
        transform.position = nearest.transform.position + Vector3.up * 0.05f;
        PickNewDestination();
    }

    TileObject FindNearestRoadTile()
    {
        if (grid == null) return null;
        float minDistance = float.MaxValue;
        TileObject nearest = null;
        Vector2Int center = currentTile != null
            ? currentTile.gridPosition
            : new Vector2Int(grid.width / 2, grid.height / 2);
        const int radius = 8;

        for (int x = center.x - radius; x <= center.x + radius; x++)
        for (int z = center.y - radius; z <= center.y + radius; z++)
        {
            TileObject tile = grid.GetTileAt(x, z);
            if (tile == null || !tile.HasRoad()) continue;
            float distance = (transform.position - tile.transform.position).sqrMagnitude;
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = tile;
            }
        }
        return nearest;
    }

    bool IsAdjacentTo(TileObject a, TileObject b)
    {
        int dx = Mathf.Abs(a.gridPosition.x - b.gridPosition.x);
        int dz = Mathf.Abs(a.gridPosition.y - b.gridPosition.y);
        return dx + dz == 1;
    }

    void OnDisable()
    {
        CancelInvoke(nameof(PickNewDestination));
    }
}