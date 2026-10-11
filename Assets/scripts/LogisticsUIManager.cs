using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the logistics panel UI.
/// 
/// Panel layout expected in Unity:
///   - logisticsPanel (root)
///     - buildingNameText      (TMP_Text)
///     - inventoryText         (TMP_Text)   ← NEW: shows building inventory
///     - availableTrucksText   (TMP_Text)
///     - resourceDropdown      (TMP_Dropdown)
///     - truckInput            (TMP_InputField)
///     - destinationText       (TMP_Text)
///     - selectDestBtn         (Button → BeginDestinationSelection)
///     - createRouteBtn        (Button → CreateRoute)
///     - noRoadWarningText     (TMP_Text)   ← NEW: shown when no road connection
///     - routesScrollContent   (Transform)  ← NEW: parent for route list items
///     - closeBtn              (Button → ClosePanel)
/// 
/// Route list item prefab needs:
///   - routeLabel             (TMP_Text)
///   - deleteButton           (Button)
/// </summary>
public class LogisticsUIManager : MonoBehaviour
{
    public static LogisticsUIManager Instance;

    [Header("Panel Root")]
    public GameObject logisticsPanel;

    [Header("Building Info")]
    public TMP_Text buildingNameText;
    public TMP_Text inventoryText;          // ← NEW
    public TMP_Text availableTrucksText;

    [Header("New Route UI")]
    public TMP_Dropdown resourceDropdown;
    public TMP_InputField truckInput;
    public TMP_Text destinationText;
    public TMP_Text noRoadWarningText;
    public UnityEngine.UI.Button createRouteButton; // disabled when no road path

    [Header("Routes Window")]
    public GameObject routesPanel;
    public Transform routesContent;
    public GameObject routeItemPrefab;

    private TileObject sourceTile;
    private TileObject destinationTile;
    private bool selectingDestination = false;
    public float destinationClickBlockTimer = 0f;
    // Track spawned route UI items so we can refresh them
    private List<GameObject> routeItems = new List<GameObject>();
    public GameObject logisticsbutton;
    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (destinationClickBlockTimer > 0f)
            destinationClickBlockTimer -= Time.deltaTime;
        if (!selectingDestination) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit[] hits = Physics.RaycastAll(ray, 500f);

            TileObject foundTile = null;

            foreach (RaycastHit hit in hits)
            {
                TileObject tile = hit.collider.GetComponentInParent<TileObject>();

                if (tile == null)
                    continue;

                if (tile == sourceTile)
                    continue;

                if (tile.CurrentBuilding == null)
                    continue;

                BuildingInventory inv =
                    tile.CurrentBuilding.GetComponent<BuildingInventory>();

                if (inv == null)
                    continue;

                foundTile = tile;
                break;
            }

            if (foundTile != null)
            {
                SelectDestination(foundTile);
                selectingDestination = false;

                destinationClickBlockTimer = 0.15f;
            }
        }
    }

    // ─────────────────────────────────────────────
    // OPEN / CLOSE
    // ─────────────────────────────────────────────

    public void OpenPanel(TileObject source)
    {
        sourceTile = source;
        destinationTile = null;
        selectingDestination = false;
        logisticsbutton.SetActive(false);
        logisticsPanel.SetActive(true);

        // Building name
        if (buildingNameText != null)
            buildingNameText.text = source.tileData.buildingType.ToString();

        // Free trucks
        if (availableTrucksText != null)
            availableTrucksText.text = "Free Trucks: " + GameManager.Instance.freeTrucks;

        // Destination reset
        if (destinationText != null)
            destinationText.text = "No Destination Selected";

        // Road warning hidden
        if (noRoadWarningText != null)
            noRoadWarningText.gameObject.SetActive(false);

        // Inventory
        RefreshInventoryDisplay();

        // Resource dropdown
        PopulateResourceDropdown();

        // Active routes list
        RefreshRoutesList();
    }

    public void ClosePanel()
    {
        logisticsPanel.SetActive(false);
        sourceTile = null;
        destinationTile = null;
        selectingDestination = false;

        if (buildingNameText != null) buildingNameText.text = "";
        if (noRoadWarningText != null) noRoadWarningText.gameObject.SetActive(false);

        // Clear the route path visual
        if (RouteVisualizer.Instance != null)
            RouteVisualizer.Instance.ClearPath();

        // Re-enable create button for next open
        SetCreateButtonState(true);

        CloseRoutesPanel();
        ClearRouteItems();
    }

    // ─────────────────────────────────────────────
    // INVENTORY DISPLAY
    // ─────────────────────────────────────────────

    void RefreshInventoryDisplay()
    {
        if (inventoryText == null || sourceTile == null) return;

        if (sourceTile.CurrentBuilding == null)
        {
            inventoryText.text = "";
            return;
        }

        BuildingInventory inv = sourceTile.CurrentBuilding.GetComponent<BuildingInventory>();
        if (inv == null)
        {
            inventoryText.text = "";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>Inventory</b>  [{inv.GetTotalStored()} / {inv.storageCapacity}]");

        foreach (var resource in inv.allowedResources)
        {
            if (resource == ResourceType.None) continue;
            int amount = inv.GetResource(resource);
            sb.AppendLine($"  {resource}: {amount}");
        }

        inventoryText.text = sb.ToString();
    }

    // ─────────────────────────────────────────────
    // ACTIVE ROUTES LIST
    // ─────────────────────────────────────────────

    void RefreshRoutesList()
    {
        ClearRouteItems();

        if (routesContent == null || routeItemPrefab == null)
            return;

        if (sourceTile == null || sourceTile.CurrentBuilding == null)
            return;

        BuildingLogistics logistics =
            sourceTile.CurrentBuilding.GetComponent<BuildingLogistics>();

        if (logistics == null)
            return;

        foreach (var route in logistics.routes)
        {
            LogisticsRoute capturedRoute = route;

            GameObject item =
                Instantiate(routeItemPrefab, routesContent);

            routeItems.Add(item);

            RouteItemUI ui =
                item.GetComponent<RouteItemUI>();

            if (ui != null)
            {
                ui.Setup(capturedRoute, this, logistics);
            }
        }
    }

    void ClearRouteItems()
    {
        foreach (var item in routeItems)
        {
            if (item != null) Destroy(item);
        }
        routeItems.Clear();
    }

    void DeleteRoute(BuildingLogistics logistics, LogisticsRoute route)
    {
        logistics.RemoveRoute(route);
        RefreshRoutesList();

        // Refresh truck count display
        if (availableTrucksText != null)
            availableTrucksText.text = "Free Trucks: " + GameManager.Instance.freeTrucks;
        GameManager.Instance.UpdateGlobalResourceTotals();
    }

    // ─────────────────────────────────────────────
    // DESTINATION SELECTION
    // ─────────────────────────────────────────────

    public void BeginDestinationSelection()
    {
        selectingDestination = true;

        if (noRoadWarningText != null)
            noRoadWarningText.gameObject.SetActive(false);

        // Clear any previous path visual while
        // the player picks a new destination
        if (RouteVisualizer.Instance != null)
            RouteVisualizer.Instance.ClearPath();

        SetCreateButtonState(false);
    }

    public void SelectDestination(TileObject tile)
    {
        destinationTile = tile;

        if (destinationText != null)
            destinationText.text =
                tile.tileData.buildingType.ToString();

        // Check road connectivity
        bool connected = sourceTile != null &&
            GridManager.Instance.IsConnectedByRoad(
                sourceTile, destinationTile);

        // Show/hide warning text
        if (noRoadWarningText != null)
        {
            noRoadWarningText.gameObject.SetActive(!connected);
            if (!connected)
                noRoadWarningText.text =
                    " No road connection to destination!";
        }

        // Draw the path visual (cyan = connected,
        // red quad on dest = not connected)
        if (RouteVisualizer.Instance != null)
            RouteVisualizer.Instance.ShowPath(
                sourceTile, destinationTile);

        // Block the Create Route button if no road
        SetCreateButtonState(connected);
    }

    // ─────────────────────────────────────────────
    // CREATE ROUTE
    // ─────────────────────────────────────────────

    public void CreateRoute()
    {
        if (sourceTile == null || destinationTile == null) return;
        if (sourceTile.CurrentBuilding == null) return;

        BuildingLogistics logistics = sourceTile.CurrentBuilding.GetComponent<BuildingLogistics>();
        if (logistics == null) return;

        string selected = resourceDropdown.options[resourceDropdown.value].text;
        ResourceType resource = (ResourceType)System.Enum.Parse(typeof(ResourceType), selected);
        bool sourceIsStorage =
    IsStorageBuilding(sourceTile);

        bool destinationIsStorage =
            IsStorageBuilding(destinationTile);

        // INVALID:
        // non-storage → non-storage
        if (!sourceIsStorage && !destinationIsStorage)
        {
            if (noRoadWarningText != null)
            {
                noRoadWarningText.gameObject.SetActive(true);
                noRoadWarningText.text =
                    " Invalid destination!";
            }

            return;
        }
        int trucks = 1;
        int.TryParse(truckInput.text, out trucks);
        trucks = Mathf.Max(1, trucks);

        if (trucks > GameManager.Instance.freeTrucks)
        {
            Debug.Log("Not enough free trucks");

            if (noRoadWarningText != null)
            {
                noRoadWarningText.gameObject.SetActive(true);
                noRoadWarningText.text = " Not enough free trucks!";
            }

            return;
        }

        // WORKFORCE CHECK
        if (GameManager.Instance.computedFreeWorkers < trucks)
        {
            Debug.Log("Not enough free workers");

            if (noRoadWarningText != null)
            {
                noRoadWarningText.gameObject.SetActive(true);
                noRoadWarningText.text = " Not enough free workers!";
            }

            return;
        }

        bool created = logistics.AddRoute(sourceTile, destinationTile, resource, trucks);

        if (!created)
        {
            if (noRoadWarningText != null)
            {
                noRoadWarningText.gameObject.SetActive(true);

                BuildingInventory destInv =
                    destinationTile.buildingInventory;

                if (destInv == null ||
                    !destInv.CanStore(resource))
                {
                    noRoadWarningText.text =
                        " Invalid resource type for destination!";
                }
                else
                {
                    noRoadWarningText.text =
                        " No road connection to destination!";
                }
            }

            return;
        }

        Debug.Log($"Route created: {sourceTile.tileData.buildingType} → {destinationTile.tileData.buildingType}");

        // Reset destination
        destinationTile = null;
        if (destinationText != null)
            destinationText.text = "No Destination Selected";
        if (noRoadWarningText != null)
            noRoadWarningText.gameObject.SetActive(false);

        // Clear the path visual now that route is made
        if (RouteVisualizer.Instance != null)
            RouteVisualizer.Instance.ClearPath();

        // Re-enable create button for next route
        SetCreateButtonState(false);

        // Refresh routes list and truck count
        RefreshRoutesList();
        if (availableTrucksText != null)
            availableTrucksText.text =
                "Free Trucks: " + GameManager.Instance.freeTrucks;
        GameManager.Instance.UpdateGlobalResourceTotals();
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    // Enable/disable the Create Route button.
    // Pass true to enable, false to disable.
    void SetCreateButtonState(bool interactable)
    {
        if (createRouteButton != null)
            createRouteButton.interactable = interactable;
    }

    void PopulateResourceDropdown()
    {
        resourceDropdown.ClearOptions();
        if (sourceTile == null || sourceTile.CurrentBuilding == null) return;

        BuildingInventory inv = sourceTile.CurrentBuilding.GetComponent<BuildingInventory>();
        if (inv == null) return;

        List<string> options = new List<string>();
        foreach (var resource in inv.allowedResources)
        {
            if (resource != ResourceType.None)
                options.Add(resource.ToString());
        }

        resourceDropdown.AddOptions(options);
    }
    public bool IsOpen()
    {
        return logisticsPanel != null && logisticsPanel.activeSelf;
    }
    public void OpenRoutesPanel()
    {
        if (routesPanel != null)
            routesPanel.SetActive(true);

        RefreshRoutesList();
    }

    public void CloseRoutesPanel()
    {
        if (routesPanel != null)
            routesPanel.SetActive(false);
    }
    public void RefreshAllRouteItems()
    {
        RouteItemUI[] items =
            routesPanel.GetComponentsInChildren<RouteItemUI>();

        foreach (var item in items)
        {
            item.RefreshExternal();
        }
    }

    public void RefreshFreeTrucksText()
    {
        if (availableTrucksText != null)
            availableTrucksText.text =
                "Free Trucks: " + GameManager.Instance.freeTrucks;
    }
    bool IsStorageBuilding(TileObject tile)
    {
        if (tile == null || tile.CurrentBuilding == null)
            return false;

        return tile.CurrentBuilding.GetComponent<StorageBuilding>() != null;
    }
}