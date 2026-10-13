using UnityEngine;
using TMPro;

// Compact food report. Values refresh when opened and on simulation ticks.
public class FoodInfoPanel : MonoBehaviour
{
    public static FoodInfoPanel Instance;

    [Header("Panel Root")]
    public GameObject panel;
    [Header("Header")]
    public TextMeshProUGUI titleText;
    [Header("Production")]
    public TextMeshProUGUI farmOutputText;
    public TextMeshProUGUI reserveText;
    public TextMeshProUGUI totalAvailableText;
    [Header("Consumption")]
    public TextMeshProUGUI populationText;
    public TextMeshProUGUI residentialText;
    public TextMeshProUGUI demandText;
    [Header("Balance")]
    public TextMeshProUGUI surplusText;
    public TextMeshProUGUI shortageTicksText;
    [Header("Status")]
    public TextMeshProUGUI statusText;

    private bool isOpen;

    void Awake() { Instance = this; }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (GameClock.Instance != null) GameClock.Instance.OnTick += OnTick;
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnTick -= OnTick;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
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
        GameManager gm = GameManager.Instance;
        GridManager grid = GridManager.Instance;
        if (gm == null || grid == null || grid.allTiles == null) return;

        int homes = 0;
        int storedFood = 0;

        foreach (TileObject tile in grid.allTiles)
        {
            if (tile == null || tile.tileData == null) continue;

            if (tile.tileData.buildingType == BuildingType.Residential)
                homes++;

            BuildingInventory inventory = tile.buildingInventory;
            if (inventory != null)
                storedFood += Mathf.Max(0, inventory.GetResource(ResourceType.Farming));

        }

        int farmOutput = Mathf.Max(0, gm.foodProducedLastTick);
        int demand = Mathf.Max(0, gm.foodDemandLastTick);
        int available = storedFood + Mathf.Max(0, gm.foodStock);
        int net = farmOutput - demand;
        string netColor = net > 0 ? "#39C96B" : net == 0 ? "#E5B93D" : "#F28C38";
        string supplyColor = available <= 0 ? "#E04F5F"
            : available < Mathf.Max(1, demand * 3) ? "#F28C38"
            : available < Mathf.Max(1, demand * 8) ? "#E5B93D" : "#39C96B";

        Set(titleText, "FOOD");
        Set(farmOutputText, $"Production / last tick   <color=#39C96B><b>+{farmOutput}</b></color>");
        Set(reserveText, $"Reserve              <b>{gm.foodStock}</b>");
        Set(totalAvailableText, $"Available now        <color={supplyColor}><b>{available}</b></color>");
        Set(populationText, $"Population           <b>{gm.population}</b>");
        Set(residentialText, $"Homes                <b>{homes}</b>");
        Set(demandText, $"Demand / last tick   <b>{demand}</b>");
        Set(surplusText, $"Net / last tick       <color={netColor}><b>{(net > 0 ? "+" : "")}{net}</b></color>");

        int threshold = Mathf.Max(1, gm.foodShortageThreshold);
        string shortageColor = gm.foodShortageCounter <= 0 ? "#39C96B"
            : gm.foodShortageCounter >= threshold - 1 ? "#E04F5F" : "#F28C38";
        Set(shortageTicksText,
            $"Shortage              <color={shortageColor}><b>{gm.foodShortageCounter}/{threshold}</b></color>");

        if (gm.isFamine)
            Set(statusText, "<color=#E04F5F><b>CRITICAL — Famine is reducing population.</b></color>");
        else if (gm.foodShortageCounter > 0)
            Set(statusText, $"<color=#F28C38><b>WARNING — Food shortage. {Mathf.Max(0, threshold - gm.foodShortageCounter)} tick(s) until famine.</b></color>");
        else if (available <= demand || (net < 0 && available < demand * 5))
            Set(statusText, "<color=#E5B93D><b>LOW — Food reserves need attention.</b></color>");
        else if (net < 0)
            Set(statusText, "<color=#E5B93D><b>WATCH — Reserves are covering a production deficit.</b></color>");
        else
            Set(statusText, "<color=#39C96B><b>STABLE — Recent production covers demand.</b></color>");
    }

    void Set(TextMeshProUGUI field, string value)
    {
        if (field != null) field.text = value;
    }
}