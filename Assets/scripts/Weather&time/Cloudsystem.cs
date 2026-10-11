using UnityEngine;
using System.Collections.Generic;

// =============================================
// CLOUD SYSTEM
// Spawns chunky voxel-style cloud prefabs that
// drift slowly across the map. Cloud density
// varies per season. Clouds scale up when the
// camera is zoomed out so they're visible at
// altitude.
//
// Unity setup:
//   cloudPrefabs    — array of voxel cloud prefabs
//                     (make a few different shapes)
//   cloudHeight     — Y position of clouds (e.g. 35f)
//   cloudAreaSize   — how far clouds spread (match grid)
//   Attach to a persistent GameObject.
// =============================================
public class CloudSystem : MonoBehaviour
{
    public static CloudSystem Instance;

    [Header("Cloud Prefabs")]
    public GameObject[] cloudPrefabs;   // assign 2-4 voxel cloud prefabs

    [Header("Settings")]
    public float cloudHeight = 35f;  // above buildings
    public float cloudAreaSize = 120f; // spawn area (match grid width * tileSize)
    public float driftSpeed = 1.5f; // units per second
    public float cloudScale = 3f;   // base scale

    [Header("Season Cloud Counts")]
    public int springCloudCount = 6;
    public int summerCloudCount = 2;
    public int autumnCloudCount = 8;
    public int winterCloudCount = 12;

    private List<CloudInstance> activeClouds = new List<CloudInstance>();
    private Season lastSeason = Season.Spring;

    void Awake() { Instance = this; }

    void Start()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnMonthEnd += OnMonthEnd;

        // Spawn initial clouds
        RefreshClouds();
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnMonthEnd -= OnMonthEnd;
    }

    void Update()
    {
        if (cloudPrefabs == null || cloudPrefabs.Length == 0) return;

        // Pause cloud movement when game is paused
        if (GameClock.Instance != null && GameClock.Instance.IsPaused) return;

        float dt = Time.deltaTime;

        // Move clouds, wrap around when they leave the area
        for (int i = activeClouds.Count - 1; i >= 0; i--)
        {
            var cloud = activeClouds[i];
            if (cloud.obj == null)
            {
                activeClouds.RemoveAt(i);
                continue;
            }

            cloud.obj.transform.position +=
                cloud.driftDir * driftSpeed * dt;

            // Wrap to opposite side when out of bounds
            Vector3 pos = cloud.obj.transform.position;
            float half = cloudAreaSize * 0.5f;
            if (pos.x > half) pos.x = -half;
            if (pos.x < -half) pos.x = half;
            if (pos.z > half) pos.z = -half;
            if (pos.z < -half) pos.z = half;
            cloud.obj.transform.position = pos;
        }
    }

    void OnMonthEnd()
    {
        Season current = GameClock.Instance.GetCurrentSeason();
        if (current != lastSeason)
        {
            lastSeason = current;
            RefreshClouds();
        }
    }

    // =========================================
    void RefreshClouds()
    {
        // Clear existing
        foreach (var c in activeClouds)
            if (c.obj != null) Destroy(c.obj);
        activeClouds.Clear();

        if (cloudPrefabs == null || cloudPrefabs.Length == 0) return;

        int count = GetCloudCount();
        for (int i = 0; i < count; i++)
            SpawnCloud();
    }

    void SpawnCloud()
    {
        GameObject prefab =
            cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];

        float half = cloudAreaSize * 0.5f;
        Vector3 pos = new Vector3(
            Random.Range(-half, half),
            cloudHeight + Random.Range(-3f, 5f),
            Random.Range(-half, half));

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);

        // Vary scale slightly for visual diversity
        float scale = cloudScale * Random.Range(0.7f, 1.4f);
        obj.transform.localScale = Vector3.one * scale;

        // Random horizontal drift direction
        float angle = Random.Range(0f, 360f);
        Vector3 drift = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            0f,
            Mathf.Sin(angle * Mathf.Deg2Rad));

        activeClouds.Add(new CloudInstance { obj = obj, driftDir = drift });
    }

    int GetCloudCount()
    {
        if (GameClock.Instance == null) return 4;
        switch (GameClock.Instance.GetCurrentSeason())
        {
            case Season.Spring: return springCloudCount;
            case Season.Summer: return summerCloudCount;
            case Season.Autumn: return autumnCloudCount;
            case Season.Winter: return winterCloudCount;
            default: return 4;
        }
    }

    // ── PUBLIC ───────────────────────────────
    // Called externally to force a weather refresh
    public void ForceRefresh() => RefreshClouds();
}

// ─────────────────────────────────────────────
class CloudInstance
{
    public GameObject obj;
    public Vector3 driftDir;
}