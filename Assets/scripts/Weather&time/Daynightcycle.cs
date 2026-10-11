using UnityEngine;

// =============================================
// DAY / NIGHT CYCLE
//
// One full cycle spans an entire month.
// The sun moves smoothly between days using
// GameClock.DayProgress.
//
// Day 1  = beginning of cycle
// Day 15 = midday
// Day 30 = end of cycle
// =============================================
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    [Header("Sun")]
    public Light sunLight;

    [Header("Sun Intensity")]
    public float maxIntensity = 1.2f;
    public float nightIntensity = 0f;

    [Header("Sun Colors")]
    public Color dawnColor = new Color(1.0f, 0.6f, 0.3f);
    public Color dayColor = new Color(1.0f, 0.98f, 0.9f);
    public Color duskColor = new Color(1.0f, 0.45f, 0.2f);
    public Color nightColor = new Color(0.1f, 0.1f, 0.3f);

    [Header("Ambient Light")]
    public Color ambientDay = new Color(0.5f, 0.5f, 0.55f);
    public Color ambientNight = new Color(0.03f, 0.03f, 0.08f);
    public Color ambientDawn = new Color(0.3f, 0.2f, 0.15f);

    [Header("Seasonal Tint")]
    public Color springTint = new Color(0.9f, 1.0f, 0.8f);
    public Color summerTint = new Color(1.0f, 0.98f, 0.8f);
    public Color autumnTint = new Color(1.0f, 0.85f, 0.6f);
    public Color winterTint = new Color(0.85f, 0.9f, 1.0f);

    // 0 = midnight
    // 0.25 = dawn
    // 0.5 = midday
    // 0.75 = dusk
    private float timeOfDay;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (GameClock.Instance == null)
            return;

        timeOfDay =
            (GameClock.Instance.day - 1 +
             GameClock.Instance.DayProgress)
            / GameClock.DaysPerMonth;

        ApplyLighting();
    }

    void ApplyLighting()
    {
        if (sunLight == null)
            return;

        float sunAngle = (timeOfDay * 360f) - 90f;

        sunLight.transform.rotation =
            Quaternion.Euler(sunAngle, -30f, 0f);

        Color sunColor;
        float intensity;

        if (timeOfDay < 0.2f)
        {
            float t = timeOfDay / 0.2f;

            sunColor = Color.Lerp(
                nightColor,
                dawnColor,
                t);

            intensity = Mathf.Lerp(
                nightIntensity,
                maxIntensity * 0.4f,
                t);

            RenderSettings.ambientLight =
                Color.Lerp(
                    ambientNight,
                    ambientDawn,
                    t);
        }
        else if (timeOfDay < 0.35f)
        {
            float t =
                (timeOfDay - 0.2f) / 0.15f;

            sunColor = Color.Lerp(
                dawnColor,
                dayColor,
                t);

            intensity = Mathf.Lerp(
                maxIntensity * 0.4f,
                maxIntensity,
                t);

            RenderSettings.ambientLight =
                Color.Lerp(
                    ambientDawn,
                    ambientDay,
                    t);
        }
        else if (timeOfDay < 0.65f)
        {
            sunColor = dayColor;
            intensity = maxIntensity;
            RenderSettings.ambientLight =
                ambientDay;
        }
        else if (timeOfDay < 0.75f)
        {
            float t =
                (timeOfDay - 0.65f) / 0.1f;

            sunColor = Color.Lerp(
                dayColor,
                duskColor,
                t);

            intensity = Mathf.Lerp(
                maxIntensity,
                maxIntensity * 0.3f,
                t);

            RenderSettings.ambientLight =
                Color.Lerp(
                    ambientDay,
                    ambientDawn,
                    t);
        }
        else if (timeOfDay < 0.85f)
        {
            float t =
                (timeOfDay - 0.75f) / 0.1f;

            sunColor = Color.Lerp(
                duskColor,
                nightColor,
                t);

            intensity = Mathf.Lerp(
                maxIntensity * 0.3f,
                nightIntensity,
                t);

            RenderSettings.ambientLight =
                Color.Lerp(
                    ambientDawn,
                    ambientNight,
                    t);
        }
        else
        {
            sunColor = nightColor;
            intensity = nightIntensity;
            RenderSettings.ambientLight =
                ambientNight;
        }

        Color seasonTint = GetSeasonTint();

        sunLight.color =
            Color.Lerp(
                sunColor,
                seasonTint,
                0.15f);

        sunLight.intensity = intensity;
    }

    Color GetSeasonTint()
    {
        if (GameClock.Instance == null)
            return Color.white;

        switch (GameClock.Instance.GetCurrentSeason())
        {
            case Season.Spring:
                return springTint;

            case Season.Summer:
                return summerTint;

            case Season.Autumn:
                return autumnTint;

            case Season.Winter:
                return winterTint;

            default:
                return dayColor;
        }
    }

    public bool IsNight =>
        timeOfDay > 0.8f || timeOfDay < 0.2f;

    public bool IsDawn =>
        timeOfDay >= 0.2f && timeOfDay < 0.35f;

    public bool IsDay =>
        timeOfDay >= 0.35f && timeOfDay < 0.65f;

    public float TimeOfDay => timeOfDay;
}