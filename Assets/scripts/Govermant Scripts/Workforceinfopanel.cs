using UnityEngine;
using TMPro;
using System.Collections.Generic;

// =============================================
// WORKFORCE INFO PANEL
// Shows a breakdown of workforce allocation
// by building category and highlights where
// workers are lacking.
//
// Unity setup:
//   Attach to an ALWAYS-ACTIVE GameObject
//   (not on the panel itself).
//   Wire panel and TMP fields in Inspector.
//   Call Toggle() from the workforce icon.
// =============================================
public class WorkforceInfoPanel : MonoBehaviour
{
    public static WorkforceInfoPanel Instance;

    [Header("Panel Root")]
    public GameObject panel;

    [Header("Summary")]
    public TextMeshProUGUI totalText;       // "Workers: 234 / 420 (55 free)"
    public TextMeshProUGUI logisticsText;   // "Logistics: 40 workers (8 trucks)"

    [Header("Category Breakdown")]
    public TextMeshProUGUI powerText;       // "Power Plants:  30 / 45"
    public TextMeshProUGUI servicesText;    // "Services:      75 / 80"
    public TextMeshProUGUI farmText;        // "Farms:         50 / 60"
    public TextMeshProUGUI extractorsText;  // "Extractors:    44 / 60"
    public TextMeshProUGUI industrialText;  // "Industrial:    80 / 100"
    public TextMeshProUGUI commercialText;  // "Commercial:    20 / 30"
    public TextMeshProUGUI storageText;     // "Storage:       10 / 20"

    [Header("Warnings")]
    public TextMeshProUGUI warningsText;    // lists understaffed categories

    private bool isOpen = false;

    void Awake() { Instance = this; }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
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

    void OnTick() { if (isOpen) Refresh(); }

    public void Toggle() { if (isOpen) Close(); else Open(); }

    public void Open()
    {
        isOpen = true;
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);
    }

    void Refresh()
    {
        if (GameManager.Instance == null ||
            GridManager.Instance == null) return;

        var gm = GameManager.Instance;

        // ── CATEGORY COUNTERS ────────────────
        int powerDemand = 0, powerAssigned = 0;
        int servicesDemand = 0, servicesAssigned = 0;
        int farmDemand = 0, farmAssigned = 0;
        int extractDemand = 0, extractAssigned = 0;
        int indDemand = 0, indAssigned = 0;
        int comDemand = 0, comAssigned = 0;
        int storDemand = 0, storAssigned = 0;

        foreach (var tile in GridManager.Instance.allTiles)
        {
            if (tile == null || tile.tileData == null ||
                tile.CurrentBuilding == null) continue;

            int demand = gm.GetWorkerCost(tile);
            if (demand <= 0) continue;

            float prod = gm.GetBuildingProductivity(tile);
            int assigned = Mathf.RoundToInt(prod * demand);

            switch (tile.tileData.buildingType)
            {
                case BuildingType.PowerPlant:
                    powerDemand += demand;
                    powerAssigned += assigned;
                    break;
                case BuildingType.Hospital:
                case BuildingType.PoliceStation:
                case BuildingType.CollegeCampus:
                    servicesDemand += demand;
                    servicesAssigned += assigned;
                    break;
                case BuildingType.Farm:
                    farmDemand += demand;
                    farmAssigned += assigned;
                    break;
                case BuildingType.OilExtractor:
                case BuildingType.Mine:
                case BuildingType.LumberYard:
                case BuildingType.IronMine:
                    extractDemand += demand;
                    extractAssigned += assigned;
                    break;
                case BuildingType.Industrial:
                case BuildingType.HeavyComplex:
                    indDemand += demand;
                    indAssigned += assigned;
                    break;
                case BuildingType.Commercial:
                    comDemand += demand;
                    comAssigned += assigned;
                    break;
                case BuildingType.GeneralStorage:
                case BuildingType.OilStorage:
                case BuildingType.IndustrialStorage:
                case BuildingType.HardwareStorage:
                    storDemand += demand;
                    storAssigned += assigned;
                    break;
            }
        }

        // ── SUMMARY ──────────────────────────
        string workerColor = gm.freeWorkers <= 0 ? "#ff4444"
                           : gm.freeWorkers < 20 ? "#ffdd00"
                           : "#00dd00";

        if (totalText != null)
            totalText.text =
                $"Workers: <b>{gm.employedWorkers} / {gm.totalWorkers}</b>" +
                $"  <color={workerColor}>({gm.freeWorkers} free)</color>";

        if (logisticsText != null)
            logisticsText.text =
                $"Logistics:  <b>{gm.logisticsWorkers}</b> workers" +
                $"  ({gm.usedTrucks} trucks active)";

        // ── CATEGORY ROWS ────────────────────
        SetRow(powerText, "Power Plants", powerAssigned, powerDemand);
        SetRow(servicesText, "Services", servicesAssigned, servicesDemand);
        SetRow(farmText, "Farms", farmAssigned, farmDemand);
        SetRow(extractorsText, "Extractors", extractAssigned, extractDemand);
        SetRow(industrialText, "Industrial", indAssigned, indDemand);
        SetRow(commercialText, "Commercial", comAssigned, comDemand);
        SetRow(storageText, "Storage", storAssigned, storDemand);

        // ── WARNINGS ─────────────────────────
        if (warningsText != null)
        {
            var sb = new System.Text.StringBuilder();

            if (powerDemand > 0 && powerAssigned < powerDemand)
                sb.AppendLine("<color=#ff4444>⚠ Power plants understaffed — reduced power output!</color>");
            if (servicesDemand > 0 && servicesAssigned < servicesDemand * 0.5f)
                sb.AppendLine("<color=#ff8800>⚠ Services severely understaffed.</color>");
            if (farmDemand > 0 && farmAssigned < farmDemand * 0.5f)
                sb.AppendLine("<color=#ff8800>⚠ Farms understaffed — food shortage risk.</color>");
            if (gm.freeWorkers <= 0)
                sb.AppendLine("<color=#ffdd00>⚠ No free workers. Build more residential.</color>");
            else if (gm.freeWorkers < 10)
                sb.AppendLine("<color=#ffdd00>⚠ Worker pool almost exhausted.</color>");

            warningsText.text = sb.Length > 0
                ? sb.ToString().TrimEnd()
                : "<color=#888888>Workforce healthy.</color>";
        }
    }

    void SetRow(TextMeshProUGUI tmp, string label, int assigned, int demand)
    {
        if (tmp == null) return;
        if (demand == 0) { tmp.text = $"{label,-14} <color=#888888>—</color>"; return; }

        float ratio = (float)assigned / demand;
        string color = ratio >= 1f ? "#00dd00"
                     : ratio >= 0.5f ? "#ffdd00"
                     : "#ff4444";

        tmp.text = $"{label,-14} <color={color}><b>{assigned} / {demand}</b></color>";
    }
}