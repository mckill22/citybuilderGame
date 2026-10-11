using UnityEngine;
using TMPro;

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

    private bool isOpen = false;

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetMouseButtonDown(1))
        {
            Close();
            return;
        }

        Refresh();
    }

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

    void Refresh()
    {
        int hap = HappinessManager.Instance != null ? HappinessManager.Instance.happiness : 0;
        int goodsBonus = HappinessManager.Instance != null ? HappinessManager.Instance.civilianGoodsBonus : 0;
        int powerBonus = HappinessManager.Instance != null ? HappinessManager.Instance.electricityBonus : 0;
        int healthBonus = HappinessManager.Instance != null ? HappinessManager.Instance.healthcareBonus : 0;
        int eduBonus = HappinessManager.Instance != null ? HappinessManager.Instance.educationBonus : 0;
        int crimePen = HappinessManager.Instance != null ? HappinessManager.Instance.crimePenalty : 0;
        bool strikeActive = HappinessManager.Instance != null && HappinessManager.Instance.strikeActive;
        bool flight = HappinessManager.Instance != null && HappinessManager.Instance.populationFlight;
        int unhappyTicks = HappinessManager.Instance != null ? HappinessManager.Instance.unhappyTickCounter : 0;
        int strikeTicks = HappinessManager.Instance != null ? HappinessManager.Instance.strikeTicksRemaining : 0;
        int strikeReq = HappinessManager.Instance != null ? HappinessManager.Instance.strikeTicksRequired : 5;
        int flightTicks = HappinessManager.Instance != null ? HappinessManager.Instance.flightTickCounter : 0;
        bool famine = GameManager.Instance != null && GameManager.Instance.isFamine;
        int sold = GameManager.Instance != null ? GameManager.Instance.monthlyGoodsSold : 0;
        int demand = GameManager.Instance != null ? GameManager.Instance.monthlyGoodsDemand : 0;
        int evtMod = RandomEventManager.Instance != null ? RandomEventManager.Instance.HappinessModifier : 0;

        // TITLE
        string hapColor = hap >= 70 ? "#00dd00" : hap >= 40 ? "#ffdd00" : "#ff4444";
        Set(titleText, $"Happiness  <color={hapColor}><b>{hap}%</b></color>");

        // BREAKDOWN
        Set(baseText, FormatRow("Base", 50));
        Set(goodsText, FormatRow("Civilian Goods", goodsBonus));
        Set(powerText, FormatRow("Power", powerBonus));
        Set(healthcareText, FormatRow("Healthcare", healthBonus));
        Set(educationText, FormatRow("Education", eduBonus));
        Set(crimeText, FormatRow("Crime Penalty", -crimePen));

        if (eventsText != null)
        {
            eventsText.gameObject.SetActive(evtMod != 0);
            if (evtMod != 0)
                eventsText.text = FormatRow("Active Events", evtMod);
        }

        // WARNINGS
        if (warningsText != null)
        {
            var sb = new System.Text.StringBuilder();
            if (strikeActive)
                sb.AppendLine($"<color=#ff4444>⚠ Workers Strike active! ({strikeTicks} ticks remaining)</color>");
            if (flight)
                sb.AppendLine($"<color=#ff4444>⚠ Population fleeing! ({flightTicks} ticks unhappy)</color>");
            if (famine)
                sb.AppendLine("<color=#ff8800>⚠ Famine active! Population declining.</color>");
            if (unhappyTicks > 0 && !strikeActive)
                sb.AppendLine($"<color=#ffdd00>⚠ Workers unhappy ({unhappyTicks}/{strikeReq} ticks). Strike in {strikeReq - unhappyTicks} more ticks.</color>");

            warningsText.text = sb.Length > 0
                ? sb.ToString().TrimEnd()
                : "<color=#888888>No warnings</color>";
        }

        // GOODS SALES
        if (goodsSalesText != null)
        {
            if (demand <= 0)
            {
                goodsSalesText.text = "<color=#888888>No commercial buildings active.</color>";
            }
            else
            {
                float ratio = Mathf.Clamp01((float)sold / demand);
                string ratioColor = ratio >= 0.8f ? "#00dd00" : ratio >= 0.5f ? "#ffdd00" : "#ff4444";
                goodsSalesText.text =
                    $"<b>Monthly Goods Sales</b>\n" +
                    $"Sold this month:   <b>{sold}</b>\n" +
                    $"Citizen demand:    <b>{demand}</b>\n" +
                    $"Satisfaction:  <color={ratioColor}><b>{Mathf.RoundToInt(ratio * 100)}%</b></color>";
            }
        }
    }

    void Set(TextMeshProUGUI tmp, string text)
    {
        if (tmp != null) tmp.text = text;
    }

    string FormatRow(string label, int value)
    {
        string sign = value >= 0 ? "+" : "";
        string color = value > 0 ? "#00dd00" : value < 0 ? "#ff4444" : "#888888";
        return $"{label,-20} <color={color}><b>{sign}{value}</b></color>";
    }
}