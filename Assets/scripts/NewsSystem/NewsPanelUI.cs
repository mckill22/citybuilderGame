using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NewsPanelUI : MonoBehaviour
{
    public static NewsPanelUI Instance;

    [Header("Panel")]
    public GameObject newsPanel;
    public Transform newsContent;
    public GameObject newsRowPrefab;
    public int maxRowsShown = 12;

    [Header("Toggle Button")]
    public GameObject notificationBadge;    // (!) icon, child of button

    [Header("Active Events Banner")]
    public TextMeshProUGUI activeEventsText;

    [Header("Category Colors")]
    public Color economicColor = new Color(0.2f, 0.9f, 0.2f);
    public Color socialColor = new Color(1.0f, 0.8f, 0.2f);
    public Color crimeColor = new Color(1.0f, 0.3f, 0.3f);
    public Color healthColor = new Color(0.3f, 0.8f, 1.0f);
    public Color politicalColor = new Color(0.8f, 0.5f, 1.0f);
    public Color militaryColor = new Color(1.0f, 0.5f, 0.2f);

    private List<GameObject> spawnedRows = new List<GameObject>();
    private bool isOpen = false;
    private int lastEventCount = 0;

    void Awake()
    {
        Instance = this;

        // Hide panel immediately in Awake so it's
        // never briefly visible before Start runs.
        // Also ensure badge starts hidden.
        if (newsPanel != null) newsPanel.SetActive(false);
        SetBadge(false);
    }

    void Start()
    {
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
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();

        if (isOpen && Input.GetMouseButtonDown(1))
            Close();
    }

    void OnTick()
    {
        // Always check for new events so badge shows
        // even if the panel was never opened yet.
        if (NewsManager.Instance != null)
        {
            int count = NewsManager.Instance.events.Count;
            if (count != lastEventCount)
            {
                lastEventCount = count;
                if (isOpen)
                    RebuildRows();
                else
                    SetBadge(true);
            }
        }

        if (isOpen)
            RefreshActiveEventsBanner();
    }

    // =========================================
    // CALLED BY NewsManager.Post
    // =========================================
    public void OnNewEvent()
    {
        if (isOpen)
            RebuildRows();
        else
            SetBadge(true);
    }

    // =========================================
    // TOGGLE / OPEN / CLOSE
    // =========================================
    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        isOpen = true;
        if (newsPanel != null) newsPanel.SetActive(true);
        SetBadge(false);
        RebuildRows();
        RefreshActiveEventsBanner();
    }

    public void Close()
    {
        isOpen = false;
        if (newsPanel != null) newsPanel.SetActive(false);
    }

    // =========================================
    // BADGE
    // =========================================
    void SetBadge(bool visible)
    {
        if (notificationBadge != null)
            notificationBadge.SetActive(visible);
    }

    // =========================================
    // ROW BUILDING
    // =========================================
    void RefreshIfChanged()
    {
        if (NewsManager.Instance == null) return;
        if (NewsManager.Instance.events.Count != lastEventCount)
            RebuildRows();
    }

    void RebuildRows()
    {
        foreach (var row in spawnedRows)
            if (row != null) Destroy(row);
        spawnedRows.Clear();

        if (NewsManager.Instance == null ||
            newsContent == null ||
            newsRowPrefab == null) return;

        int shown = Mathf.Min(
            NewsManager.Instance.events.Count, maxRowsShown);

        for (int i = 0; i < shown; i++)
        {
            NewsEvent evt = NewsManager.Instance.events[i];
            GameObject rowGO = Instantiate(newsRowPrefab, newsContent);
            spawnedRows.Add(rowGO);

            NewsRowUI row = rowGO.GetComponent<NewsRowUI>();
            if (row != null)
                row.Setup(evt, GetCategoryColor(evt.category));
        }

        lastEventCount = NewsManager.Instance.events.Count;
    }

    // =========================================
    // ACTIVE EVENTS BANNER
    // =========================================
    void RefreshActiveEventsBanner()
    {
        if (activeEventsText == null) return;

        if (RandomEventManager.Instance == null ||
            RandomEventManager.Instance.activeEvents.Count == 0)
        {
            activeEventsText.text = "<color=#888888>No active events</color>";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>Active Events:</b>");
        foreach (var evt in RandomEventManager.Instance.activeEvents)
            sb.AppendLine($"  • {evt.title} <color=#aaaaaa>({evt.monthsRemaining}mo left)</color>");

        activeEventsText.text = sb.ToString();
    }

    // =========================================
    // HELPERS
    // =========================================
    Color GetCategoryColor(NewsCategory cat)
    {
        switch (cat)
        {
            case NewsCategory.Economic: return economicColor;
            case NewsCategory.Social: return socialColor;
            case NewsCategory.Crime: return crimeColor;
            case NewsCategory.Health: return healthColor;
            case NewsCategory.Political: return politicalColor;
            case NewsCategory.Military: return militaryColor;
            default: return Color.white;
        }
    }
}