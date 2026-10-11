using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================
// CLOCK UI
// Attach to a HUD panel that shows date and
// speed controls.
//
// Unity setup:
//   ── ClockPanel ──
//   dateText          TMP_Text   "15 March 1952"
//   seasonText        TMP_Text   "Spring"
//   speedText         TMP_Text   "► Normal"
//
//   pauseButton       Button → TogglePause()
//   normalButton      Button → SetNormal()
//   fastButton        Button → SetFast()
//   veryFastButton    Button → SetVeryFast()
//
// Speed button highlights:
//   Each button's image tints to show which
//   speed is currently active.
// =============================================
public class ClockUI : MonoBehaviour
{
    public static ClockUI Instance;

    [Header("Date Display")]
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI seasonText;

    [Header("Speed Display")]
    public TextMeshProUGUI speedText;

    [Header("Speed Buttons")]
    public Button pauseButton;
    public Button normalButton;
    public Button fastButton;
    public Button veryFastButton;

    [Header("Active Button Color")]
    public Color activeColor = new Color(1f, 0.85f, 0.2f); // gold
    public Color inactiveColor = new Color(0.6f, 0.6f, 0.6f);

    void Awake() { Instance = this; }

    void Update()
    {
        if (GameClock.Instance == null) return;

        // Date
        if (dateText != null)
            dateText.text = GameClock.Instance.GetDateString();

        // Season
        if (seasonText != null)
        {
            Season s = GameClock.Instance.GetCurrentSeason();
            string seasonLabel;
            string seasonColor;

            switch (s)
            {
                case Season.Spring:
                    seasonLabel = "Spring";
                    seasonColor = "88ff88";
                    break;
                case Season.Summer:
                    seasonLabel = "Summer";
                    seasonColor = "ffdd44";
                    break;
                case Season.Autumn:
                    seasonLabel = "Autumn";
                    seasonColor = "ff8844";
                    break;
                default:
                    seasonLabel = "Winter";
                    seasonColor = "aaddff";
                    break;
            }

            seasonText.text =
                "<color=#" + seasonColor + ">" +
                seasonLabel + "</color>";
        }

        // Speed label
        if (speedText != null)
        {
            switch (GameClock.Instance.currentSpeed)
            {
                case GameClock.GameSpeed.Paused:
                    speedText.text = "❚❚ Paused";
                    break;
                case GameClock.GameSpeed.Normal:
                    speedText.text = "► Normal";
                    break;
                case GameClock.GameSpeed.Fast:
                    speedText.text = "►► Fast";
                    break;
                case GameClock.GameSpeed.VeryFast:
                    speedText.text = "►►► Max";
                    break;
            }
        }

        // Highlight active speed button
        RefreshButtonColors();
    }

    // =========================================
    // BUTTON CALLBACKS
    // Wire these to your speed buttons.
    // =========================================
    public void TogglePause()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.TogglePause();
    }

    public void SetNormal()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.SetNormal();
    }

    public void SetFast()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.SetFast();
    }

    public void SetVeryFast()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.SetVeryFast();
    }

    // =========================================
    // BUTTON HIGHLIGHT
    // =========================================
    void RefreshButtonColors()
    {
        if (GameClock.Instance == null) return;

        var speed = GameClock.Instance.currentSpeed;

        SetButtonColor(pauseButton,
            speed == GameClock.GameSpeed.Paused);

        SetButtonColor(normalButton,
            speed == GameClock.GameSpeed.Normal);

        SetButtonColor(fastButton,
            speed == GameClock.GameSpeed.Fast);

        SetButtonColor(veryFastButton,
            speed == GameClock.GameSpeed.VeryFast);
    }

    void SetButtonColor(Button btn, bool active)
    {
        if (btn == null) return;

        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = active ? activeColor : inactiveColor;
    }
}