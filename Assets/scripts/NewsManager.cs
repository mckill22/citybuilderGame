using UnityEngine;
using System.Collections.Generic;

// =============================================
// NEWS MANAGER
// Collects game events for display in the
// News UI (built in roadmap item 4).
// Any system can call NewsManager.Post() to
// log an event. The UI simply reads the list.
// =============================================
public enum NewsCategory
{
    Economic,
    Social,
    Political,
    Crime,
    Health,
    Military
}

[System.Serializable]
public class NewsEvent
{
    public NewsCategory category;
    public string message;
    public string date;      // from GameClock at time of posting
    public int tick;      // for sorting / expiry

    public NewsEvent(NewsCategory cat, string msg, string date, int tick)
    {
        category = cat;
        message = msg;
        this.date = date;
        this.tick = tick;
    }
}

public class NewsManager : MonoBehaviour
{
    public static NewsManager Instance;

    // Last 20 events — UI will show the most recent 5
    public List<NewsEvent> events = new List<NewsEvent>();
    public int maxEvents = 20;

    void Awake() { Instance = this; }

    // ─────────────────────────────────────────
    // POST — call from any system to log an event
    // ─────────────────────────────────────────
    public static void Post(NewsCategory category, string message)
    {
        if (Instance == null) return;

        string date = GameClock.Instance != null
            ? GameClock.Instance.GetDateString()
            : "Day 1";

        int tick = GameClock.Instance != null
            ? GameClock.Instance.TotalDaysElapsed
            : 0;

        var evt = new NewsEvent(category, message, date, tick);
        Instance.events.Insert(0, evt); // newest first

        // Trim to max
        if (Instance.events.Count > Instance.maxEvents)
            Instance.events.RemoveAt(Instance.events.Count - 1);

        // Notify UI — triggers shake + badge if panel is closed
        if (NewsPanelUI.Instance != null)
            NewsPanelUI.Instance.OnNewEvent();

        Debug.Log($"[NEWS] [{category}] {date}: {message}");
    }
}