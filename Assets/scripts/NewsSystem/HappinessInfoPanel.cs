using UnityEngine;
using TMPro;
using System.Text;

// Compact happiness report. Refreshes on simulation ticks rather than every frame.
public class HappinessInfoPanel : MonoBehaviour
{
    public static HappinessInfoPanel Instance;

    [Header("Panel Root")]
    public GameObject panel;
    [Header("Header")]
    public TextMeshProUGUI titleText;
    [Header("Breakdown Rows")]
    public TextMeshProUGUI baseText;
    public TextMeshProUGUI goodsText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI healthcareText;
    public TextMeshProUGUI educationText;
    public TextMeshProUGUI crimeText;
    public TextMeshProUGUI eventsText;
    [Header("Status Warnings")]
    public TextMeshProUGUI warningsText;
    [Header("Goods Sales Summary")]
    public TextMeshProUGUI goodsSalesText;

    private bool isOpen;

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    void Start()
    {
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
        HappinessManager hm = HappinessManager.Instance;
        GameManager gm = GameManager.Instance;
        if (hm == null || gm == null) return;

        int hap = Mathf.Clamp(hm.happiness, 0, 100);
        string scoreColor = hap >= 70 ? "#39C96B" : hap >= 45 ? "#E5B93D" : hap >= 25 ? "#F28C38" : "#E04F5F";
        string state = hap >= 70 ? "HAPPY" : hap >= 45 ? "STABLE" : hap >= 25 ? "UNHAPPY" : "CRITICAL";
        Set(titleText, $"HAPPINESS  <color={scoreColor}><b>{hap}% — {state}</b></color>");

        Set(baseText, FormatRow("Base", 50));
        Set(goodsText, FormatRow("Civilian goods", hm.civilianGoodsBonus));
        Set(powerText, FormatRow("Electricity", hm.electricityBonus));
        Set(healthcareText, FormatRow("Healthcare", hm.healthcareBonus));
        Set(educationText, FormatRow("Education", hm.educationBonus));
        Set(crimeText, FormatRow("Crime impact", -hm.crimePenalty));

        int eventModifier = RandomEventManager.Instance != null
            ? RandomEventManager.Instance.HappinessModifier : 0;
        if (eventsText != null)
        {
            eventsText.gameObject.SetActive(eventModifier != 0);
            if (eventModifier != 0) eventsText.text = FormatRow("Active event", eventModifier);
        }

        StringBuilder warnings = new StringBuilder();
        if (gm.isFamine)
            warnings.AppendLine("<color=#E04F5F>CRITICAL: Famine is reducing population.</color>");
        if (hm.strikeActive)
            warnings.AppendLine($"<color=#E04F5F>CRITICAL: Workers' strike ({hm.strikeTicksRemaining} ticks left).</color>");
        if (hm.populationFlight)
            warnings.AppendLine($"<color=#E04F5F>CRITICAL: Citizens are leaving ({hm.flightTickCounter} unhappy ticks).</color>");
        else if (hm.unhappyTickCounter > 0)
            warnings.AppendLine($"<color=#F28C38>WARNING: Strike risk {hm.unhappyTickCounter}/{Mathf.Max(1, hm.strikeTicksRequired)}.</color>");
        if (gm.isGasShortage)
            warnings.AppendLine("<color=#F28C38>WARNING: Fuel shortage is lowering happiness.</color>");
        if (CrimeManager.Instance != null && CrimeManager.Instance.globalCrime >= 70f)
            warnings.AppendLine("<color=#E04F5F>CRITICAL: High crime is hurting the economy.</color>");

        Set(warningsText, warnings.Length > 0
            ? warnings.ToString().TrimEnd()
            : "<color=#39C96B>No urgent problems.</color>");

        if (goodsSalesText != null)
        {
            int sold = gm.monthlyGoodsSold;
            int demand = gm.monthlyGoodsDemand;
            if (demand <= 0)
                goodsSalesText.text = "<color=#888888>Goods sales: no current demand recorded.</color>";
            else
            {
                float ratio = Mathf.Clamp01((float)sold / demand);
                string ratioColor = ratio >= 0.8f ? "#39C96B" : ratio >= 0.5f ? "#E5B93D" : "#E04F5F";
                goodsSalesText.text =
                    $"Goods sales satisfaction  <color={ratioColor}><b>{Mathf.RoundToInt(ratio * 100f)}%</b></color>\n" +
                    $"Sold <b>{sold}</b> / Demand <b>{demand}</b>";
            }
        }
    }

    void Set(TextMeshProUGUI field, string value)
    {
        if (field != null) field.text = value;
    }

    string FormatRow(string label, int value)
    {
        string sign = value > 0 ? "+" : "";
        string color = value > 0 ? "#39C96B" : value < 0 ? "#E04F5F" : "#888888";
        return $"{label}  <color={color}><b>{sign}{value}</b></color>";
    }
}