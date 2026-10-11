using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================
// FOOD INFO PANEL
// Opens when the player clicks the food icon
// in the HUD. Shows a detailed breakdown of
// food production, consumption, reserves, and
// famine status.
//
// Unity setup:
//   Attach to a panel GameObject.
//   Wire all TMP fields in the Inspector.
//   Call Toggle() from the food icon's
//   onClick event.
//
// Panel layout suggestion:
//   ┌─────────────────────────────┐
//   │  🌾 Food Summary            │
//   │─────────────────────────────│
//   │  PRODUCTION                 │
//   │  Farm output / tick:  +32   │
//   │  Starting reserve:    480   │
//   │  Total available:     512   │
//   │─────────────────────────────│
//   │  CONSUMPTION                │
//   │  Population:          400   │
//   │  Residential tiles:   16    │
//   │  Food needed / tick:  16    │
//   │─────────────────────────────│
//   │  BALANCE                    │
//   │  Surplus / tick:      +16   │
//   │  Shortage ticks:      0/3   │
//   │─────────────────────────────│
//   │  STATUS:  ✓ Well fed        │
//   └─────────────────────────────┘
// =============================================
public class FoodInfoPanel : MonoBehaviour
{
    public static FoodInfoPanel Instance;

    [Header("Panel Root")]
    public GameObject panel;

    [Header("Header")]
    public TextMeshProUGUI titleText;

    [Header("Production")]
    public TextMeshProUGUI farmOutputText;    // "Farm output / tick:  +32"
    public TextMeshProUGUI reserveText;       // "Starting reserve:  480"
    public TextMeshProUGUI totalAvailableText;// "Total available:  512"

    [Header("Consumption")]
    public TextMeshProUGUI populationText;    // "Population:  400"
    public TextMeshProUGUI residentialText;   // "Residential tiles:  16"
    public TextMeshProUGUI demandText;        // "Food needed / tick:  16"

    [Header("Balance")]
    public TextMeshProUGUI surplusText;       // "Surplus / tick:  +16"
    public TextMeshProUGUI shortageTicksText; // "Shortage ticks:  0 / 3"

    [Header("Status")]
    public TextMeshProUGUI statusText;        // "✓ Well fed" / "⚠ Famine!"

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

    void OnTick()
    {
        if (isOpen) Refresh();
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
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);
    }

    // =========================================
    // REFRESH
    // =========================================
    void Refresh()
    {
        if (GameManager.Instance == null) return;
        var gm = GameManager.Instance;

        // ── COUNT FARMS AND RESIDENTIALS ─────
        int residentialCount = 0;
        int farmOutputPerTick = 0;

        foreach (var tile in GridManager.Instance.allTiles)
        {
            if (tile == null || tile.tileData == null) continue;

            if (tile.tileData.buildingType == BuildingType.Residential)
                residentialCount++;

            if (tile.tileData.buildingType == BuildingType.Farm &&
                tile.CurrentBuilding != null)
            {
                // Estimate farm output using productivity
                float prod = gm.GetBuildingProductivity(tile);
                float eventMod = RandomEventManager.Instance != null
                    ? RandomEventManager.Instance.FarmingModifier : 1f;
                farmOutputPerTick += Mathf.RoundToInt(5 * prod * eventMod);
            }
        }

        int foodDemandPerTick = residentialCount;
        int totalAvailable = gm.farming; // already includes foodStock
        int surplus = farmOutputPerTick - foodDemandPerTick;

        // ── TITLE ────────────────────────────
        if (titleText != null)
            titleText.text = "Food Summary";

        // ── PRODUCTION ───────────────────────
        if (farmOutputText != null)
        {
            string droughtNote =
                RandomEventManager.Instance != null &&
                RandomEventManager.Instance.FarmingModifier < 1f
                    ? " <color=#ff8800>(drought -50%)</color>"
                    : "";
            farmOutputText.text =
                $"Farm output / tick:  <b>+{farmOutputPerTick}</b>{droughtNote}";
        }

        if (reserveText != null)
            reserveText.text =
                $"Starting reserve:  <b>{gm.foodStock}</b>";

        if (totalAvailableText != null)
        {
            string availColor = totalAvailable > foodDemandPerTick * 10
                ? "#00dd00" : totalAvailable > 0 ? "#ffdd00" : "#ff4444";
            totalAvailableText.text =
                $"Total in storage:  " +
                $"<color={availColor}><b>{totalAvailable}</b></color>";
        }

        // ── CONSUMPTION ──────────────────────
        if (populationText != null)
            populationText.text =
                $"Population:  <b>{gm.population}</b>";

        if (residentialText != null)
            residentialText.text =
                $"Residential buildings:  <b>{residentialCount}</b>";

        if (demandText != null)
            demandText.text =
                $"Food consumed / tick:  <b>{foodDemandPerTick}</b>";

        // ── BALANCE ──────────────────────────
        if (surplusText != null)
        {
            string sign = surplus >= 0 ? "+" : "";
            string surplusColor = surplus >= 0 ? "#00dd00" : "#ff4444";
            surplusText.text =
                $"Net / tick:  " +
                $"<color={surplusColor}><b>{sign}{surplus}</b></color>";
        }

        if (shortageTicksText != null)
        {
            string shortColor = gm.foodShortageCounter == 0
                ? "#00dd00" : "#ff8800";
            shortageTicksText.text =
                $"Shortage ticks:  " +
                $"<color={shortColor}><b>" +
                $"{gm.foodShortageCounter} / {gm.foodShortageThreshold}" +
                $"</b></color>";
        }

        // ── STATUS ───────────────────────────
        if (statusText != null)
        {
            if (gm.isFamine)
                statusText.text =
                    "<color=#ff4444><b>☠ FAMINE ACTIVE — Population declining!</b></color>";
            else if (gm.foodShortageCounter > 0)
                statusText.text =
                    $"<color=#ff8800><b>⚠ Food shortage! " +
                    $"Famine in {gm.foodShortageThreshold - gm.foodShortageCounter}" +
                    $" more ticks.</b></color>";
            else if (surplus <= 0 && gm.foodStock <= 0)
                statusText.text =
                    "<color=#ffdd00><b>⚠ Breaking even. " +
                    "Build more farms.</b></color>";
            else if (surplus <= 0 && gm.foodStock > 0)
                statusText.text =
                    $"<color=#ffdd00><b>⚠ Farms can't keep up. " +
                    $"Reserve covers ~{gm.foodStock / Mathf.Max(1, -surplus)} ticks." +
                    $"</b></color>";
            else
                statusText.text =
                    "<color=#00dd00><b>✓ Well fed. " +
                    "Food supply is healthy.</b></color>";
        }
    }
}