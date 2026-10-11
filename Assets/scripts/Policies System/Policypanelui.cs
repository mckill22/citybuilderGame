using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// =============================================
// POLICY PANEL UI
// Shows all available policies grouped by
// category. Each policy has a toggle button,
// status indicator, cost, and description.
//
// Unity setup:
//   policyPanel        — root panel GO
//   policyItemPrefab   — prefab with PolicyItemUI
//   contentParent      — ScrollView content parent
//   Attach to a persistent always-active GO.
//   Call Toggle() from your policies button.
// =============================================
public class PolicyPanelUI : MonoBehaviour
{
    public static PolicyPanelUI Instance;

    [Header("Panel")]
    public GameObject policyPanel;
    public Transform contentParent;
    public GameObject policyItemPrefab;

    [Header("Status Text")]
    public TextMeshProUGUI activePolicyCostText; // "Active policies cost: $X/tick"

    private bool isOpen = false;
    public bool IsOpen => isOpen;
    private List<GameObject> spawnedItems = new List<GameObject>();

    void Awake() { Instance = this; }

    void Start()
    {
        if (policyPanel != null) policyPanel.SetActive(false);

        // Refresh on each tick so costs and states stay live
        if (GameClock.Instance != null)
            GameClock.Instance.OnTick += OnTick;
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnTick -= OnTick;
    }

    void Update()
    {
        if (!isOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetMouseButtonDown(1))
            Close();
    }

    void OnTick()
    {
        if (isOpen) RefreshAll();
    }

    // =========================================
    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        isOpen = true;
        if (policyPanel != null) policyPanel.SetActive(true);
        BuildItems();
    }

    public void Close()
    {
        isOpen = false;
        if (policyPanel != null) policyPanel.SetActive(false);
    }

    // =========================================
    // BUILD ITEM LIST
    // =========================================
    void BuildItems()
    {
        // Clear old
        foreach (var item in spawnedItems)
            if (item != null) Destroy(item);
        spawnedItems.Clear();

        if (PolicyManager.Instance == null ||
            contentParent == null ||
            policyItemPrefab == null) return;

        // Group by category
        string currentCategory = "";
        foreach (var policy in PolicyManager.Instance.GetAllPolicies())
        {
            string cat = PolicyManager.Instance.GetCategory(policy);
            if (cat != currentCategory)
            {
                currentCategory = cat;
                // Spawn a category header if your prefab supports it
                // (or just let the UI layout show them in order)
            }

            GameObject itemGO = Instantiate(policyItemPrefab, contentParent);
            spawnedItems.Add(itemGO);

            PolicyItemUI itemUI = itemGO.GetComponent<PolicyItemUI>();
            if (itemUI != null)
                itemUI.Setup(policy, this);
        }

        RefreshCostText();
    }

    // =========================================
    // REFRESH
    // =========================================
    public void RefreshAll()
    {
        foreach (var item in spawnedItems)
        {
            if (item == null) continue;
            PolicyItemUI ui = item.GetComponent<PolicyItemUI>();
            if (ui != null) ui.Refresh();
        }
        RefreshCostText();
    }

    void RefreshCostText()
    {
        if (activePolicyCostText == null ||
            PolicyManager.Instance == null) return;

        int totalCost = 0;
        foreach (var policy in PolicyManager.Instance.GetAllPolicies())
            if (PolicyManager.Instance.IsActive(policy))
                totalCost += PolicyManager.Instance.GetCostPerTick(policy);

        activePolicyCostText.text = totalCost > 0
            ? $"Active policy cost: <color=#ff8800><b>${totalCost}/tick</b></color>"
            : "<color=#888888>No active policy costs</color>";
    }
}