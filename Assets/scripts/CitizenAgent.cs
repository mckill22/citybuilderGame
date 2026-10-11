using UnityEngine;
using System.Collections.Generic;

// =============================================
// CITIZEN AGENT
// Cosmetic pedestrian that walks between
// district tiles using only walkable terrain
// (no water). Loops forever until destroyed.
//
// Speed sync: movement scales with GameClock
// speed multiplier. Freezes while paused.
// =============================================
public class CitizenAgent : MonoBehaviour
{
    [Header("Movement")]
    public float baseMoveSpeed = 1.5f;  // speed at 1x (Normal)
    public float jumpHeight = 0.10f;

    // ─────────────────────────────────────────
    private GridManager grid;
    private TileObject currentTile;
    private TileObject targetTile;

    private List<TileObject> path = new List<TileObject>();
    private int pathIndex = 0;

    private float moveProgress = 0f;
    private Vector3 stepStart;
    private Vector3 stepEnd;

    private bool isMoving = false;

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
    public void Init(TileObject start, GridManager gm)
    {
        grid = gm;
        currentTile = start;

        transform.position =
            start.transform.position + Vector3.up * 0.05f;

        PickNewDestination();
    }

    // =========================================
    void Update()
    {
        // Freeze entirely while paused
        if (IsPaused) return;

        if (!isMoving) return;

        float currentSpeed = baseMoveSpeed * SpeedMultiplier;

        float stepDist = Vector3.Distance(stepStart, stepEnd);
        float dt = stepDist > 0.001f
            ? (currentSpeed * Time.deltaTime) / stepDist
            : 1f;

        moveProgress = Mathf.Min(moveProgress + dt, 1f);

        // Position with hop arc
        Vector3 pos = Vector3.Lerp(stepStart, stepEnd, moveProgress);
        pos.y += Mathf.Sin(moveProgress * Mathf.PI) * jumpHeight;
        transform.position = pos;

        // Face direction
        Vector3 dir = (stepEnd - stepStart).normalized;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 8f * SpeedMultiplier);
        }

        // Reached current step
        if (moveProgress >= 1f)
        {
            currentTile = path[pathIndex];
            pathIndex++;

            if (pathIndex < path.Count)
                StartStep();
            else
            {
                isMoving = false;
                PickNewDestination();
            }
        }
    }

    // =========================================
    // DESTINATION + PATH
    // =========================================
    void PickNewDestination()
    {
        List<TileObject> districts = grid.districtTiles;

        if (districts == null || districts.Count < 2)
        {
            Invoke(nameof(PickNewDestination), 2f);
            return;
        }

        TileObject dest = null;
        int safety = 0;

        while (safety < 20)
        {
            TileObject candidate =
                districts[Random.Range(0, districts.Count)];

            if (candidate != currentTile &&
                candidate.tileData.terrainType != TerrainType.Water)
            {
                dest = candidate;
                break;
            }

            safety++;
        }

        if (dest == null)
        {
            Invoke(nameof(PickNewDestination), 2f);
            return;
        }

        targetTile = dest;
        BuildWalkablePath(currentTile, targetTile);
    }

    // =========================================
    // PATH BUILDING
    // =========================================
    void BuildWalkablePath(TileObject from, TileObject to)
    {
        path.Clear();
        pathIndex = 0;

        Vector2Int cur = from.gridPosition;
        Vector2Int dst = to.gridPosition;

        int safety = 0;

        while (cur != dst && safety < 200)
        {
            safety++;

            int dx = dst.x - cur.x;
            int dz = dst.y - cur.y;

            bool moveX = Mathf.Abs(dx) >= Mathf.Abs(dz);

            int nx = cur.x + (moveX ? (int)Mathf.Sign(dx) : 0);
            int nz = cur.y + (moveX ? 0 : (int)Mathf.Sign(dz));

            TileObject next = grid.GetTileAt(nx, nz);

            if (next == null ||
                next.tileData.terrainType == TerrainType.Water)
            {
                nx = cur.x + (moveX ? 0 : (int)Mathf.Sign(dx));
                nz = cur.y + (moveX ? (int)Mathf.Sign(dz) : 0);
                next = grid.GetTileAt(nx, nz);
            }

            if (next == null ||
                next.tileData.terrainType == TerrainType.Water)
            {
                Invoke(nameof(PickNewDestination), 0.5f);
                return;
            }

            path.Add(next);
            cur = next.gridPosition;
        }

        if (path.Count == 0)
        {
            Invoke(nameof(PickNewDestination), 1f);
            return;
        }

        StartStep();
    }

    void StartStep()
    {
        stepStart = transform.position;
        stepEnd = path[pathIndex].transform.position + Vector3.up * 0.05f;
        moveProgress = 0f;
        isMoving = true;
    }
}