using UnityEngine;
using System;

// =============================================
// GAME CLOCK
// Singleton. Owns all time progression.
//
// Architecture:
//   - Real time → ticks (economy)
//   - Ticks → days
//   - Days → months → years
//   - Speed is a multiplier on tick rate
//   - Pause stops all ticks
//
// Other systems subscribe via:
//   GameClock.Instance.OnTick     (every economy tick)
//   GameClock.Instance.OnDayEnd   (end of each day)
//   GameClock.Instance.OnMonthEnd (end of each month — upkeep, policies)
//   GameClock.Instance.OnYearEnd  (end of each year)
//
// Unity setup:
//   Attach to a persistent GameObject in scene.
//   Wire speedButtons in Inspector or call
//   SetSpeed() from UI buttons.
// =============================================
public class GameClock : MonoBehaviour
{
    public static GameClock Instance;

    // ─────────────────────────────────────────
    // DATE STATE
    // ─────────────────────────────────────────
    [Header("Current Date")]
    public int day = 1;
    public int month = 1;
    public int year = 1950;   // starting year

    // ─────────────────────────────────────────
    // TICK SETTINGS
    // One economy tick = one in-game day.
    // basTickInterval is seconds of real time
    // per tick at speed 1x.
    // ─────────────────────────────────────────
    [Header("Tick Settings")]
    public float baseTickInterval = 2f;  // seconds per tick at 1x

    // ─────────────────────────────────────────
    // SPEED
    // ─────────────────────────────────────────
    public enum GameSpeed
    {
        Paused = 0,
        Normal = 1,
        Fast = 2,
        VeryFast = 3
    }

    [Header("Speed")]
    public GameSpeed currentSpeed = GameSpeed.Normal;

    // Real-time multipliers per speed level
    private static readonly float[] speedMultipliers =
        new float[] { 0f, 1f, 3f, 8f };

    // ─────────────────────────────────────────
    // CALENDAR CONSTANTS
    // ─────────────────────────────────────────
    public const int DaysPerMonth = 30;   // simplified
    public const int MonthsPerYear = 12;

    private static readonly string[] monthNames =
    {
        "January", "February", "March",    "April",
        "May",     "June",     "July",     "August",
        "September","October", "November", "December"
    };

    // ─────────────────────────────────────────
    // EVENTS
    // Subscribe to these from other systems.
    // ─────────────────────────────────────────
    public event Action OnTick;       // every economy tick (= 1 day)
    public event Action OnDayEnd;     // same as tick, alias for clarity
    public event Action OnMonthEnd;   // every 30 ticks
    public event Action OnYearEnd;    // every 360 ticks

    // ─────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────
    private float timer = 0f;

    // =========================================
    void Awake()
    {
        Instance = this;
    }

    // =========================================
    void Update()
    {
        if (currentSpeed == GameSpeed.Paused) return;

        float multiplier =
            speedMultipliers[(int)currentSpeed];

        float interval =
            baseTickInterval / multiplier;

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f;
            AdvanceTick();
        }
    }

    // =========================================
    // TICK ADVANCE
    // =========================================
    void AdvanceTick()
    {
        // Fire economy tick first
        OnTick?.Invoke();
        OnDayEnd?.Invoke();

        // Advance date
        day++;

        if (day > DaysPerMonth)
        {
            day = 1;
            month++;

            OnMonthEnd?.Invoke();

            if (month > MonthsPerYear)
            {
                month = 1;
                year++;

                OnYearEnd?.Invoke();
            }
        }
    }

    // =========================================
    // SPEED CONTROLS
    // Wire these to UI buttons.
    // =========================================
    public void SetSpeed(int speedIndex)
    {
        currentSpeed = (GameSpeed)Mathf.Clamp(
            speedIndex, 0, 3);
    }

    public void Pause()
    {
        currentSpeed = GameSpeed.Paused;
    }

    public void SetNormal()
    {
        currentSpeed = GameSpeed.Normal;
    }

    public void SetFast()
    {
        currentSpeed = GameSpeed.Fast;
    }

    public void SetVeryFast()
    {
        currentSpeed = GameSpeed.VeryFast;
    }

    public void TogglePause()
    {
        if (currentSpeed == GameSpeed.Paused)
            SetNormal();
        else
            Pause();
    }

    public bool IsPaused =>
        currentSpeed == GameSpeed.Paused;

    // =========================================
    // DATE HELPERS
    // =========================================

    // "15 March 1952"
    public string GetDateString()
    {
        return day + " " + GetMonthName() + " " + year;
    }

    // "March 1952"
    public string GetMonthYearString()
    {
        return GetMonthName() + " " + year;
    }

    public string GetMonthName()
    {
        return monthNames[
            Mathf.Clamp(month - 1, 0, 11)];
    }
    public float DayProgress
    {
        get
        {
            if (currentSpeed == GameSpeed.Paused)
                return 0f;

            float multiplier =
                speedMultipliers[(int)currentSpeed];

            float interval =
                baseTickInterval / multiplier;

            return Mathf.Clamp01(timer / interval);
        }
    }
    // Which season are we in?
    public Season GetCurrentSeason()
    {
        switch (month)
        {
            case 3: case 4: case 5: return Season.Spring;
            case 6: case 7: case 8: return Season.Summer;
            case 9: case 10: case 11: return Season.Autumn;
            default: return Season.Winter;
        }
    }

    // Days elapsed since game start
    public int TotalDaysElapsed =>
        (year - 1950) * MonthsPerYear * DaysPerMonth +
        (month - 1) * DaysPerMonth +
        (day - 1);
}


// =============================================
// SEASON ENUM
// Used by weather system and farming modifiers.
// Lives here so any system can reference it
// without depending on a separate file.
// =============================================
public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}