using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================
// CITY FOUND BUTTON
// Attach to the "Found New City" button in
// your build UI.
//
// Unity setup:
//   - Attach this script to the button GameObject
//   - Wire buildManager in Inspector
//   - Wire requirementText to a TMP label that
//     shows "Need 1250 pop" when locked
//   - The Button component's onClick should call
//     OnClick() on this script
// =============================================
public class CityFoundButton : MonoBehaviour
{
    [Header("References")]
    public BuildManager buildManager;

    [Header("UI")]
    // The Button component on this GameObject
    private Button button;

    // Optional: label showing "Need X pop"
    public TextMeshProUGUI requirementText;

    // Optional: lock icon shown when unavailable
    public GameObject lockIcon;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Update()
    {
        if (CityManager.Instance == null ||
     !CityManager.Instance.gameStarted)
        {
            if (button != null)
                button.interactable = false;

            return;
        }

        

        bool canFound = CityManager.Instance
            .CanFoundNewCity();

        // Enable / disable the button
        if (button != null)
            button.interactable = canFound;

        // Lock icon
        if (lockIcon != null)
            lockIcon.SetActive(!canFound);

        // Requirement text
        if (requirementText != null)
        {
            if (canFound)
            {
                requirementText.text =
                    "Found New City";
            }
            else
            {
                int needed = CityManager.Instance
                    .GetNextCityThreshold();

                int current = CityManager.Instance
                    .GetTotalPopulation();

                int remaining = needed - current;

                requirementText.text =
                    "Need " + remaining +
                    " more pop\n(" +
                    current + " / " + needed + ")";
            }
        }
    }

    // ─────────────────────────────────────────
    // Called by the Button's onClick event
    // ─────────────────────────────────────────
    public void OnClick()
    {
        if (buildManager == null) return;
        buildManager.BeginTownHallPlacement();
    }
}