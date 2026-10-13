using UnityEngine;
using System.Collections.Generic;

// Cosmetic pedestrian. Uses a reusable BFS search so walls/water cannot trap
// the agent in a greedy back-and-forth route.
public class CitizenAgent : MonoBehaviour
{
    [Header("Movement")]
    public float baseMoveSpeed = 1.5f;
    public float turnSpeed = 540f;
    public float jumpHeight = 0.035f;

    private GridManager grid;
    private TileObject currentTile;
    private TileObject targetTile;
    private readonly List<TileObject> path = new List<TileObject>();
    private readonly Queue<TileObject> searchQueue = new Queue<TileObject>();
    private readonly Dictionary<TileObject, TileObject> cameFrom =
        new Dictionary<TileObject, TileObject>();
    private int pathIndex;
    private float moveProgress;
    private Vector3 stepStart;
    private Vector3 stepEnd;
    private bool isMoving;
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

    public void Init(TileObject start, GridManager gm)
    {
        if (start == null || gm == null) return;
        grid = gm;
        currentTile = start;
        personalSpeed = Random.Range(0.88f, 1.12f);
        transform.position = start.transform.position + Vector3.up * 0.05f;
        ScheduleDestination(Random.Range(0.1f, 0.8f));
    }

    void Update()
    {
        if (IsPaused || !isMoving || pathIndex >= path.Count) return;

        float currentSpeed = baseMoveSpeed * personalSpeed * SpeedMultiplier;
        float stepDistance = Vector3.Distance(stepStart, stepEnd);
        float delta = stepDistance > 0.001f ? currentSpeed * Time.deltaTime / stepDistance : 1f;
        moveProgress = Mathf.Min(1f, moveProgress + delta);

        Vector3 position = Vector3.Lerp(stepStart, stepEnd, moveProgress);
        position.y += Mathf.Sin(moveProgress * Mathf.PI) * jumpHeight;
        transform.position = position;

        Vector3 direction = stepEnd - stepStart;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(direction.normalized),
                turnSpeed * SpeedMultiplier * Time.deltaTime);

        if (moveProgress < 1f) return;

        transform.position = stepEnd;
        currentTile = path[pathIndex++];
        if (pathIndex < path.Count)
            StartStep();
        else
        {
            isMoving = false;
            ScheduleDestination(Random.Range(0.35f, 1.5f));
        }
    }

    void ScheduleDestination(float delay)
    {
        CancelInvoke(nameof(PickNewDestination));
        Invoke(nameof(PickNewDestination), delay);
    }

    void PickNewDestination()
    {
        if (grid == null || currentTile == null) return;
        List<TileObject> districts = grid.districtTiles;
        if (districts == null || districts.Count < 2)
        {
            ScheduleDestination(2f);
            return;
        }

        for (int attempt = 0; attempt < 4; attempt++)
        {
            TileObject candidate = districts[Random.Range(0, districts.Count)];
            if (candidate == null || candidate == currentTile || !IsWalkable(candidate))
                continue;

            targetTile = candidate;
            if (BuildWalkablePath(currentTile, targetTile)) return;
        }

        ScheduleDestination(1.5f);
    }

    bool BuildWalkablePath(TileObject from, TileObject to)
    {
        path.Clear();
        pathIndex = 0;
        isMoving = false;
        if (grid == null || from == null || to == null ||
            !IsWalkable(from) || !IsWalkable(to))
            return false;

        searchQueue.Clear();
        cameFrom.Clear();
        searchQueue.Enqueue(from);
        cameFrom[from] = null;

        bool found = false;
        while (searchQueue.Count > 0)
        {
            TileObject current = searchQueue.Dequeue();
            if (current == to)
            {
                found = true;
                break;
            }

            Vector2Int p = current.gridPosition;
            TryVisit(p.x + 1, p.y, current);
            TryVisit(p.x - 1, p.y, current);
            TryVisit(p.x, p.y + 1, current);
            TryVisit(p.x, p.y - 1, current);
        }

        if (!found) return false;

        TileObject step = to;
        while (step != from)
        {
            path.Add(step);
            if (!cameFrom.TryGetValue(step, out step))
            {
                path.Clear();
                return false;
            }
        }
        path.Reverse();

        if (path.Count == 0) return false;
        StartStep();
        return true;
    }

    void TryVisit(int x, int z, TileObject parent)
    {
        TileObject next = grid.GetTileAt(x, z);
        if (next == null || cameFrom.ContainsKey(next) || !IsWalkable(next)) return;
        cameFrom[next] = parent;
        searchQueue.Enqueue(next);
    }

    bool IsWalkable(TileObject tile)
    {
        return tile != null && tile.tileData != null &&
               tile.tileData.terrainType != TerrainType.Water;
    }

    void StartStep()
    {
        if (pathIndex >= path.Count)
        {
            isMoving = false;
            return;
        }

        stepStart = transform.position;
        stepEnd = path[pathIndex].transform.position + Vector3.up * 0.05f;
        moveProgress = 0f;
        isMoving = true;
    }

    void OnDisable()
    {
        CancelInvoke(nameof(PickNewDestination));
        isMoving = false;
    }
}