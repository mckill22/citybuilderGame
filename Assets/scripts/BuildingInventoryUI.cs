using UnityEngine;
using TMPro;

/// <summary>
/// Attach this component to building prefabs (or the TileObject).
/// When the tile is selected, UIManager calls Show(); when deselected, Hide().
/// 
/// In Unity, create a World Space Canvas child of each building prefab
/// with a TMP_Text child, and wire it to the inventoryCanvas/inventoryLabel fields,
/// OR use a single shared overlay driven by UIManager.ShowBuildingPanel.
/// 
/// The simplest integration: UIManager.ShowBuildingPanel already calls this
/// via RefreshBuildingInventoryOverlay if you wire up the sharedInventoryPanel.
/// </summary>
public class BuildingInventoryUI : MonoBehaviour
{
    [Header("World-space label (optional — attach above building)")]
    public TMP_Text worldLabel;   // A TMP label in a world-space canvas above the building

    [Header("Auto-refresh interval (seconds, 0 = manual only)")]
    public float refreshInterval = 1f;

    private BuildingInventory inventory;
    private float timer;
    private bool isVisible = false;

    void Awake()
    {
        inventory = GetComponent<BuildingInventory>();
        if (inventory == null)
            inventory = GetComponentInParent<BuildingInventory>();

        if (worldLabel != null)
            worldLabel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isVisible || inventory == null || worldLabel == null) return;

        timer += Time.deltaTime;
        if (refreshInterval > 0 && timer >= refreshInterval)
        {
            timer = 0f;
            RefreshLabel();
        }
    }

    public void Show()
    {
        // Don't show inventory overlay while logistics UI is open
        if (LogisticsUIManager.Instance != null &&
            LogisticsUIManager.Instance.IsOpen())
        {
            Hide();
            return;
        }

        isVisible = true;

        if (worldLabel != null)
            worldLabel.gameObject.SetActive(true);

        RefreshLabel();
    }

    public void Hide()
    {
        isVisible = false;
        if (worldLabel != null)
            worldLabel.gameObject.SetActive(false);
    }

    public void RefreshLabel()
    {
        if (inventory == null || worldLabel == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>{GetBuildingName()}</b>");
        sb.AppendLine($"[{inventory.GetTotalStored()}/{inventory.storageCapacity}]");

        foreach (var resource in inventory.allowedResources)
        {
            if (resource == ResourceType.None) continue;
            int amount = inventory.GetResource(resource);
            if (amount > 0)
                sb.AppendLine($"{ResourceIcon(resource)} {amount}");
        }

        worldLabel.text = sb.ToString();
    }

    string GetBuildingName()
    {
        TileObject tile = GetComponent<TileObject>();
        if (tile == null) tile = GetComponentInParent<TileObject>();
        if (tile != null) return tile.tileData.buildingType.ToString();
        return gameObject.name;
    }

    string ResourceIcon(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Wood: return "🪵";
            case ResourceType.Oil: return "🛢";
            case ResourceType.Farming: return "🌾";
            case ResourceType.Minerals: return "⛏";
            case ResourceType.Iron: return "🔩";
            case ResourceType.CivilianGoods: return "📦";
            case ResourceType.HeavySupplies: return "🔧";
            case ResourceType.MilitarySupplies: return "🎒";
            case ResourceType.Gasoline: return "⛽";
            case ResourceType.Trucks: return "🚛";
            case ResourceType.HeavyTrucks: return "🚚";
            case ResourceType.Tanks: return "🛡";
            default: return "•";
        }
    }

    /// <summary>
    /// Build a formatted inventory string for use in the Logistics panel
    /// (called by LogisticsUIManager.RefreshInventoryDisplay).
    /// </summary>
    public static string GetInventorySummary(BuildingInventory inv)
    {
        if (inv == null) return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>Inventory</b>  [{inv.GetTotalStored()} / {inv.storageCapacity}]");

        foreach (var resource in inv.allowedResources)
        {
            if (resource == ResourceType.None) continue;
            sb.AppendLine($"  {resource}: {inv.GetResource(resource)}");
        }

        return sb.ToString();
    }
}