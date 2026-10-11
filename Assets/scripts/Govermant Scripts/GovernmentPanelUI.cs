using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GovernmentPanelUI : MonoBehaviour
{
    public static GovernmentPanelUI Instance;

    // ─────────────────────────────────────────
    // COUNTRY OVERVIEW
    // ─────────────────────────────────────────
    [Header("Country Overview")]
    public TextMeshProUGUI countryNameText;     // "Republic of X"
    public TextMeshProUGUI leaderNameText;      // "Leader: Y"
    public TextMeshProUGUI governmentTypeText;  // "Democracy"
    public TextMeshProUGUI governmentDescText;  // one-line summary
    public TextMeshProUGUI totalPopText;        // "Total Population: 1247"

    // ─────────────────────────────────────────
    // TAX CONTROLS
    // ─────────────────────────────────────────
    [Header("Tax Controls")]
    public TextMeshProUGUI taxRateText;    // "Tax Rate: 15%"
    public TextMeshProUGUI taxCapText;     // "Max: 30% (Democracy)"
    public TextMeshProUGUI taxIncomeText;  // "Est. income / tick: ~42$"

    // ─────────────────────────────────────────
    // MONTHLY ECONOMY SUMMARY
    // ─────────────────────────────────────────
    [Header("Monthly Economy")]
    public TextMeshProUGUI monthlyIncomeText;   // "Income:  +$1240"
    public TextMeshProUGUI monthlyExpensesText; // "Expenses: -$230"
    public TextMeshProUGUI monthlyNetText;      // "Net:      +$1010"

    // ─────────────────────────────────────────
    // CITY LIST
    // ─────────────────────────────────────────
    [Header("City List")]
    public Transform cityListContent;  // Vertical Layout Group
    public GameObject cityRowPrefab;    // prefab with CityRowUI

    // ─────────────────────────────────────────
    private List<GameObject> spawnedRows = new List<GameObject>();

    void Awake() { Instance = this; }

    // =========================================
    // OPEN — called by BuildManager.OpenGovernmentPanel
    // =========================================
    public void OnPanelOpened()
    {
        // Rebuild the city row list on open
        // (new cities may have been founded)
        RefreshCityList();
    }

    // =========================================
    // UPDATE — all live stats refresh here
    // Overview + tax run every frame so they
    // are always current when panel is visible.
    // City rows manage their own Update loops.
    // =========================================
    void Update()
    {
        if (!gameObject.activeSelf) return;

        if (CityManager.Instance == null ||
            !CityManager.Instance.gameStarted)
            return;

        RefreshOverview();
        RefreshTax();
        RefreshEconomy();
    }

    // =========================================
    // OVERVIEW
    // =========================================
    void RefreshOverview()
    {
        var cap = CityManager.Instance.Capital;
        if (cap == null) return;

        if (countryNameText != null)
            countryNameText.text =
                CityManager.Instance.countryName;

        if (leaderNameText != null)
            leaderNameText.text =
                "Leader: " + cap.leaderName;

        if (governmentTypeText != null)
            governmentTypeText.text =
                cap.governmentType.ToString();

        if (governmentDescText != null)
            governmentDescText.text =
                GetGovDescription(cap.governmentType);

        if (totalPopText != null)
            totalPopText.text =
                "Total Population: " +
                CityManager.Instance.GetTotalPopulation();
    }

    // =========================================
    // TAX
    // =========================================
    void RefreshTax()
    {
        int rate = CityManager.Instance.globalTaxRate;
        GovernmentConfig cfg = CityManager.Instance.Config;

        if (taxRateText != null)
            taxRateText.text = "Tax Rate: " + rate + "%";

        if (taxCapText != null)
            taxCapText.text =
                "Max: " + cfg.maxTaxRate + "%" +
                " (" + CityManager.Instance.GovernmentType + ")";

        if (taxIncomeText != null)
        {
            int pop = CityManager.Instance.GetTotalPopulation();
            int est;

            if (cfg.commercialIncomeEnabled)
                est = Mathf.RoundToInt(
                    pop * (rate / 100f) *
                    cfg.taxMultiplier * 0.75f);
            else
                est = Mathf.RoundToInt(
                    pop * cfg.flatIncomePerPop);

            taxIncomeText.text =
                "Est. income / tick: ~" + est + "$";
        }
    }

    // =========================================
    // MONTHLY ECONOMY SUMMARY
    // All figures are per-month estimates so the
    // player sees the same cadence as upkeep.
    // =========================================
    void RefreshEconomy()
    {
        if (monthlyIncomeText == null &&
            monthlyExpensesText == null &&
            monthlyNetText == null) return;

        GovernmentConfig cfg = CityManager.Instance.Config;
        int rate = CityManager.Instance.globalTaxRate;
        int pop = CityManager.Instance.GetTotalPopulation();
        int daysPerMonth = GameClock.DaysPerMonth; // 30

        // ── INCOME ──────────────────────────
        // Tax income per tick × 30 days
        int taxPerTick = 0;
        if (rate > 0)
        {
            if (cfg.commercialIncomeEnabled)
                taxPerTick = Mathf.RoundToInt(
                    pop * (rate / 100f) * cfg.taxMultiplier);
            else
                taxPerTick = Mathf.RoundToInt(
                    pop * cfg.flatIncomePerPop);
        }

        // Happiness modifier (average estimate at current happiness)
        float happinessMod = 0.5f + (GameManager.Instance.happiness / 100f);
        taxPerTick = Mathf.RoundToInt(taxPerTick * happinessMod);

        // Commercial income per tick × 30 days
        // Estimate: count commercial buildings × avg sales per tick
        int commercialBuildings = 0;
        foreach (var tile in GridManager.Instance.allTiles)
        {
            if (tile == null || tile.tileData == null) continue;
            if (tile.tileData.buildingType == BuildingType.Commercial &&
                tile.CurrentBuilding != null)
                commercialBuildings++;
        }
        // Each commercial sells up to 5 goods/tick at $4 each
        float ecoMod = HappinessManager.Instance != null
            ? HappinessManager.Instance.GetEconomicModifier() : 1f;
        int commercialPerTick = Mathf.RoundToInt(commercialBuildings * 5 * 4 * ecoMod);

        int monthlyIncome = (taxPerTick + commercialPerTick) * daysPerMonth;

        // ── EXPENSES ────────────────────────
        // Upkeep costs are already per-month
        int policeCount = 0;
        int hospitalCount = 0;
        int collegeCount = 0;

        foreach (var tile in GridManager.Instance.allTiles)
        {
            if (tile == null || tile.tileData == null ||
                tile.CurrentBuilding == null) continue;
            switch (tile.tileData.buildingType)
            {
                case BuildingType.PoliceStation: policeCount++; break;
                case BuildingType.Hospital: hospitalCount++; break;
                case BuildingType.CollegeCampus: collegeCount++; break;
            }
        }

        int monthlyExpenses =
            policeCount * GameManager.Instance.policeUpkeep +
            hospitalCount * GameManager.Instance.hospitalUpkeep +
            collegeCount * GameManager.Instance.collegeUpkeep;

        int net = monthlyIncome - monthlyExpenses;

        // ── DISPLAY ─────────────────────────
        if (monthlyIncomeText != null)
            monthlyIncomeText.text =
                "Income:   <color=#00dd00>+$" + monthlyIncome + "</color>";

        if (monthlyExpensesText != null)
            monthlyExpensesText.text =
                "Expenses: <color=#ff4444>-$" + monthlyExpenses + "</color>";

        if (monthlyNetText != null)
        {
            string netColor = net >= 0 ? "#00dd00" : "#ff4444";
            string netSign = net >= 0 ? "+" : "";
            monthlyNetText.text =
                "Net:      <color=" + netColor + ">" + netSign + "$" + net + "</color>";
        }
    }

    // =========================================
    // TAX BUTTONS
    // =========================================
    public void IncreaseTax()
    {
        if (CityManager.Instance == null) return;
        int cap = CityManager.Instance.Config.maxTaxRate;
        CityManager.Instance.globalTaxRate =
            Mathf.Clamp(
                CityManager.Instance.globalTaxRate + 1,
                0, cap);
    }

    public void DecreaseTax()
    {
        if (CityManager.Instance == null) return;
        CityManager.Instance.globalTaxRate =
            Mathf.Clamp(
                CityManager.Instance.globalTaxRate - 1,
                0, 100);
    }

    // =========================================
    // CITY LIST
    // Rebuilt on panel open and after rename.
    // =========================================
    void RefreshCityList()
    {
        foreach (var row in spawnedRows)
            if (row != null) Destroy(row);
        spawnedRows.Clear();

        if (cityListContent == null ||
            cityRowPrefab == null ||
            CityManager.Instance == null) return;

        foreach (var city in CityManager.Instance.cities)
        {
            GameObject rowGO = Instantiate(
                cityRowPrefab, cityListContent);
            spawnedRows.Add(rowGO);

            CityRowUI rowUI =
                rowGO.GetComponent<CityRowUI>();
            if (rowUI != null)
                rowUI.Setup(city, this);
        }
    }

    // =========================================
    // RENAME — called by CityRowUI
    // =========================================
    public void RenameCity(CityData city, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        city.cityName = newName.Trim();
        // Rebuild rows so the new name shows
        // immediately in the list
        RefreshCityList();
    }

    // =========================================
    // GOV DESCRIPTION
    // =========================================
    string GetGovDescription(GovernmentType gov)
    {
        switch (gov)
        {
            case GovernmentType.Democracy:
                return "Tax cap 30% · Happiness +5 · Balanced";
            case GovernmentType.Dictatorship:
                return "Tax cap 60% · Happiness -10 · Crime +10";
            case GovernmentType.Monarchy:
                return "Tax +20% income · Crime -5 · Stable";
            case GovernmentType.Communism:
                return "Flat income · Production +20% · No commerce";
            case GovernmentType.Fascism:
                return "Military +25% · Crime -15 · Happiness -15";
            default:
                return "";
        }
    }
}