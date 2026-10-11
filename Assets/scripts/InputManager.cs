using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    public Camera cam;
    LayerMask tileLayer;
    private TileObject selectedTile;

    void Start()
    {
        tileLayer = LayerMask.GetMask("Tile");
    }

    void Update()
    {
        // Block during logistics destination pick
        if (LogisticsUIManager.Instance != null &&
            LogisticsUIManager.Instance.destinationClickBlockTimer > 0f)
            return;

        // Block during build mode (except TownHall
        // placement which BuildManager handles itself)
        if (BuildManagerInstanceIsActive()) return;

        // Block if pointer is over UI
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
            HandleClick();

        // Right-click deselects
        if (Input.GetMouseButtonDown(1))
        {
            if (selectedTile != null)
            {
                selectedTile.ResetColor();
                selectedTile = null;
            }

            UIManager.Instance.CloseAllPanels();
        }
    }

    void HandleClick()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, 100f, tileLayer))
            return;

        TileObject tile =
            hit.collider.GetComponentInParent<TileObject>();

        if (tile == null) return;

        SelectTile(tile);
    }

    void SelectTile(TileObject tile)
    {
        // Clicking the same tile again deselects
        if (selectedTile == tile)
        {
            selectedTile.ResetColor();
            selectedTile = null;
            UIManager.Instance.CloseAllPanels();
            return;
        }

        // Deselect previous
        if (selectedTile != null)
            selectedTile.ResetColor();

        selectedTile = tile;
        selectedTile.Highlight();

        // ── TOWN HALL CLICK ──────────────────
        // Switches city view instead of opening
        // a building inventory panel.
        // UIManager.ShowBuildingPanel handles this
        // branch internally.
        if (tile.tileData != null &&
            tile.tileData.buildingType == BuildingType.TownHall)
        {
            UIManager.Instance.ShowBuildingPanel(tile);
            return;
        }

        // ── NORMAL BUILDING CLICK ────────────
        if (tile.HasDistrict())
        {
            UIManager.Instance.ShowBuildingPanel(tile);
        }
        else
        {
            // Empty tile — close panels
            UIManager.Instance.CloseAllPanels();
        }
    }

    bool BuildManagerInstanceIsActive()
    {
        BuildManager bm = FindObjectOfType<BuildManager>();
        return bm != null && bm.IsInBuildMode();
    }
}