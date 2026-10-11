using UnityEngine;
using System.Collections.Generic;

// =============================================
// CAR AGENT
// Cosmetic vehicle that follows road tiles
// toward a district destination, then picks
// a new one. Loops forever.
//
// Speed sync: movement scales with GameClock
// speed multiplier. Freezes while paused.
// Stuck detection is disabled while paused
// to avoid false positives.
// =============================================
public class CarAgent : MonoBehaviour
{
    [Header("Movement")]
    public float baseMoveSpeed = 4f;    // speed at 1x (Normal)
    public float rotateSpeed = 10f;

    [Header("Stuck Detection")]
    public float stuckTimeout = 3f;
    public float stuckMinMove = 0.1f;

    // ─────────────────────────────────────────
    private GridManager grid;
    private TileObject currentTile;
    private TileObject destinationTile;

    private List<TileObject> path = new List<TileObject>();
    private int pathIndex = 0;

    private Vector3 lastCheckedPos;
    private float stuckTimer = 0f;

    // ─────────────────────────────────────────
    // Speed multipliers mirror GameClock exactly
    // ─────────────────────────────────────────
    private static readonly float[] speedMultipliers = { 0f, 1f, 3f, 8f };

    float SpeedMultiplier
    {
        get
        {
            if (GameClock.Instance == null) return 1f;
            return speedMultipliers[(int)GameClock.Instance.currentSpeed];
        }
    }

    bool IsPaused =>
        GameClock.Instance != null &&
        GameClock.Instance.currentSpeed == GameClock.GameSpeed.Paused;

    // =========================================
    public void InitPath(
        TileObject start,
        TileObject target,
        GridManager gm)
    {
        grid = gm;
        currentTile = start;
        destinationTile = target;

        transform.position =
            start.transform.position + Vector3.up * 0.05f;

        lastCheckedPos = transform.position;

        BuildPath();
    }

    // =========================================
    void Update()
    {
        // Freeze entirely while paused
        if (IsPaused) return;

        if (path == null || pathIndex >= path.Count)
            return;

        float currentSpeed = baseMoveSpeed * SpeedMultiplier;

        TileObject nextTile = path[pathIndex];
        Vector3 targetPos =
            nextTile.transform.position + Vector3.up * 0.05f;

        // Move toward next tile
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            currentSpeed * Time.deltaTime);

        // Smooth rotation toward movement direction
        Vector3 dir = (targetPos - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                rotateSpeed * SpeedMultiplier * Time.deltaTime);
        }

        // Reached tile
        if (Vector3.Distance(transform.position, targetPos) < 0.05f)
        {
            transform.position = targetPos;
            currentTile = nextTile;
            pathIndex++;

            if (pathIndex >= path.Count)
                PickNewDestination();
        }

        // Stuck detection — only accumulate time when not paused
        // (we already return early if paused, but guard is explicit)
        stuckTimer += Time.deltaTime;
        if (stuckTimer >= stuckTimeout)
        {
            if (Vector3.Distance(transform.position, lastCheckedPos) < stuckMinMove)
                RecoverFromStuck();

            lastCheckedPos = transform.position;
            stuckTimer = 0f;
        }
    }

    // =========================================
    // DESTINATION
    // =========================================
    void PickNewDestination()
    {
        List<TileObject> districts = grid.districtTiles;

        if (districts == null || districts.Count < 2)
            return;

        TileObject dest = null;
        int safety = 0;

        while (safety < 20)
        {
            TileObject candidate =
                districts[Random.Range(0, districts.Count)];

            if (candidate != currentTile)
            {
                dest = candidate;
                break;
            }

            safety++;
        }

        if (dest == null) return;

        destinationTile = dest;
        BuildPath();
    }

    // =========================================
    // BFS PATH — road tiles only
    // =========================================
    void BuildPath()
    {
        path.Clear();
        pathIndex = 0;

        if (currentTile == null || destinationTile == null) return;

        Queue<TileObject> queue = new Queue<TileObject>();
        Dictionary<TileObject, TileObject> cameFrom =
            new Dictionary<TileObject, TileObject>();

        queue.Enqueue(currentTile);
        cameFrom[currentTile] = null;

        TileObject found = null;

        while (queue.Count > 0)
        {
            TileObject cur = queue.Dequeue();

            if (cur == destinationTile ||
                IsAdjacentTo(cur, destinationTile))
            {
                found = cur;
                break;
            }

            foreach (TileObject n in GetRoadNeighbors(cur))
            {
                if (cameFrom.ContainsKey(n)) continue;
                cameFrom[n] = cur;
                queue.Enqueue(n);
            }
        }

        if (found == null)
        {
            PickNewDestination();
            return;
        }

        TileObject step = found;
        while (step != null)
        {
            path.Add(step);
            cameFrom.TryGetValue(step, out step);
        }
        path.Reverse();

        if (path.Count > 0 &&
            path[path.Count - 1] != destinationTile)
            path.Add(destinationTile);
    }

    // =========================================
    // STUCK RECOVERY
    // =========================================
    void RecoverFromStuck()
    {
        TileObject nearest = FindNearestRoadTile();
        if (nearest == null) return;

        currentTile = nearest;
        transform.position =
            nearest.transform.position + Vector3.up * 0.05f;

        PickNewDestination();
    }

    TileObject FindNearestRoadTile()
    {
        float minDist = float.MaxValue;
        TileObject nearest = null;

        Vector2Int center = currentTile != null
            ? currentTile.gridPosition
            : new Vector2Int(grid.width / 2, grid.height / 2);

        int radius = 8;

        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int z = center.y - radius; z <= center.y + radius; z++)
            {
                TileObject t = grid.GetTileAt(x, z);
                if (t == null || !t.HasRoad()) continue;

                float d = Vector3.Distance(
                    transform.position,
                    t.transform.position);

                if (d < minDist)
                {
                    minDist = d;
                    nearest = t;
                }
            }
        }

        return nearest;
    }

    // =========================================
    // HELPERS
    // =========================================
    List<TileObject> GetRoadNeighbors(TileObject tile)
    {
        var list = new List<TileObject>(4);
        TryAdd(tile, 1, 0, list);
        TryAdd(tile, -1, 0, list);
        TryAdd(tile, 0, 1, list);
        TryAdd(tile, 0, -1, list);
        return list;
    }

    void TryAdd(TileObject tile, int dx, int dz, List<TileObject> list)
    {
        TileObject n = grid.GetTileAt(
            tile.gridPosition.x + dx,
            tile.gridPosition.y + dz);

        if (n != null && n.HasRoad())
            list.Add(n);
    }

    bool IsAdjacentTo(TileObject a, TileObject b)
    {
        int dx = Mathf.Abs(a.gridPosition.x - b.gridPosition.x);
        int dz = Mathf.Abs(a.gridPosition.y - b.gridPosition.y);
        return (dx + dz) == 1;
    }
}