using UnityEngine;
using System.Collections.Generic;

// =============================================
// ROUTE VISUALIZER
// Draws a glowing path along road tiles between
// an origin building and a destination building.
// Used by LogisticsUIManager when the player
// selects a destination before creating a route.
//
// Unity setup:
//   Attach to a persistent GameObject in scene.
//   Assign pathMaterial in Inspector —
//   use an Unlit/Color or particle material
//   with a bright colour (e.g. cyan/yellow).
//   tileSize should match GridManager.tileSize.
//
// The visualizer spawns one flat quad per road
// tile in the path, slightly above ground level,
// then destroys them all when ClearPath() is called.
// =============================================
public class RouteVisualizer : MonoBehaviour
{
    public static RouteVisualizer Instance;

    [Header("Visual Settings")]
    public Material pathMaterial;        // Unlit bright material
    public Color pathColor = new Color(0f, 1f, 1f, 0.6f);  // cyan
    public Color noPathColor = new Color(1f, 0.2f, 0.2f, 0.6f); // red
    public float tileSize = 5f;   // match GridManager.tileSize
    public float yOffset = 0.15f; // height above ground
    public float quadScale = 0.6f;  // lane width as fraction of tileSize

    // ─────────────────────────────────────────
    private List<GameObject> pathQuads =
        new List<GameObject>();

    void Awake() { Instance = this; }

    // =========================================
    // SHOW PATH
    // Call this when destination is selected.
    // Draws cyan connected segments if a road
    // path exists, red indicator on destination
    // tile if not connected.
    // =========================================
    public void ShowPath(TileObject origin, TileObject destination)
    {
        ClearPath();

        if (origin == null || destination == null) return;

        List<TileObject> path =
            GridManager.Instance.GetRoadPath(
                origin, destination);

        if (path == null || path.Count == 0)
        {
            // No connection — show red indicator
            // on the destination tile only
            SpawnTileMarker(destination, noPathColor);
            return;
        }

        // Draw one stretched segment between each
        // consecutive pair of tiles so the path is
        // continuous regardless of tile spacing.
        for (int i = 0; i < path.Count - 1; i++)
        {
            SpawnSegment(path[i], path[i + 1], pathColor);
        }

        // Cap the path with a small marker on the
        // very first and last tile so it reads
        // clearly even from directly above.
        SpawnTileMarker(path[0], pathColor);
        SpawnTileMarker(path[path.Count - 1], pathColor);
    }

    // =========================================
    // CLEAR PATH
    // Call on panel close or destination change.
    // =========================================
    public void ClearPath()
    {
        foreach (var quad in pathQuads)
        {
            if (quad != null)
                Destroy(quad);
        }

        pathQuads.Clear();
    }

    // =========================================
    // SPAWN SEGMENT
    // Stretches a quad to exactly span the
    // distance between two adjacent tile centers,
    // oriented to face from A to B. This is what
    // makes consecutive tiles connect with no gap
    // regardless of quadScale.
    // =========================================
    void SpawnSegment(TileObject a, TileObject b, Color color)
    {
        if (a == null || b == null || pathMaterial == null)
            return;

        Vector3 posA = a.transform.position + Vector3.up * yOffset;
        Vector3 posB = b.transform.position + Vector3.up * yOffset;

        Vector3 mid = (posA + posB) * 0.5f;
        float dist = Vector3.Distance(posA, posB);

        GameObject quad =
            GameObject.CreatePrimitive(PrimitiveType.Quad);

        Destroy(quad.GetComponent<Collider>());

        quad.transform.position = mid;

        // Lie flat on the ground (quad's face points
        // straight up) then spin around the vertical
        // axis so the quad's local Y (length) axis
        // aligns with the direction from A to B.
        Vector3 dir = (posB - posA).normalized;

        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        quad.transform.rotation =
            Quaternion.Euler(90f, 0f, 0f) *
            Quaternion.Euler(0f, 0f, -angle);

        // Width = lane thickness (tileSize * quadScale)
        // Length = exact distance to next tile center,
        // with a small overlap so segments never gap
        // even on slightly uneven tile spacing.
        float width = tileSize * quadScale;
        float length = dist + (tileSize * 0.05f);

        quad.transform.localScale =
            new Vector3(width, length, 1f);

        ApplyMaterial(quad, color);

        quad.transform.SetParent(transform);
        pathQuads.Add(quad);
    }

    // =========================================
    // SPAWN TILE MARKER
    // Small fixed-size quad centered on a single
    // tile — used for path endpoints and the
    // "no connection" red indicator.
    // =========================================
    void SpawnTileMarker(TileObject tile, Color color)
    {
        if (tile == null || pathMaterial == null)
            return;

        GameObject quad =
            GameObject.CreatePrimitive(PrimitiveType.Quad);

        Destroy(quad.GetComponent<Collider>());

        quad.transform.position =
            tile.transform.position + Vector3.up * yOffset;

        quad.transform.rotation =
            Quaternion.Euler(90f, 0f, 0f);

        float size = tileSize * quadScale;
        quad.transform.localScale = new Vector3(size, size, 1f);

        ApplyMaterial(quad, color);

        quad.transform.SetParent(transform);
        pathQuads.Add(quad);
    }

    // =========================================
    // APPLY MATERIAL
    // Instances the material so each quad can
    // have its own color independently.
    // =========================================
    void ApplyMaterial(GameObject quad, Color color)
    {
        Renderer rend = quad.GetComponent<Renderer>();
        rend.material = new Material(pathMaterial);
        rend.material.color = color;
    }
}