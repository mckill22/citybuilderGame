using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================
// CITY ROW UI
// Attach to the city row prefab used by
// GovernmentPanelUI's city list.
//
// Prefab child layout (all wired in Inspector):
//
//   [★] Baghdad          Pop: 412
//   😊 Happiness: 72%   🔴 Crime: 8%
//   [Rename]
//   ┌─────────────────┐  [✓]
//   │  new name...    │
//   └─────────────────┘
//
// The rename input and confirm button are
// hidden until the player clicks [Rename].
// =============================================
public class CityRowUI : MonoBehaviour
{
    [Header("Display")]
    public TextMeshProUGUI cityNameText;   // "★ Baghdad" or "City 2"
    public TextMeshProUGUI cityStatsText;  // "Pop: 412 | 😊72% | Crime 8%"

    [Header("Rename")]
    public Button renameButton;
    public GameObject renameGroup;       // parent of input + confirm
    public TMP_InputField renameInputField;
    public Button confirmRenameBtn;
    public Button cancelRenameBtn;

    // ─────────────────────────────────────────
    private CityData city;
    private GovernmentPanelUI panel;

    // =========================================
    public void Setup(CityData cityData,
                      GovernmentPanelUI ownerPanel)
    {
        city = cityData;
        panel = ownerPanel;

        RefreshDisplay();

        // Hide rename controls until needed
        if (renameGroup != null)
            renameGroup.SetActive(false);

        // Wire buttons
        if (renameButton != null)
            renameButton.onClick.AddListener(OnRenameClicked);

        if (confirmRenameBtn != null)
            confirmRenameBtn.onClick.AddListener(OnConfirmRename);

        if (cancelRenameBtn != null)
            cancelRenameBtn.onClick.AddListener(OnCancelRename);
    }

    // =========================================
    // DISPLAY
    // =========================================
    void RefreshDisplay()
    {
        if (city == null) return;

        // Name line — capital gets a star
        if (cityNameText != null)
        {
            string prefix = city.isCapital ? "★ " : "    ";
            cityNameText.text = prefix + city.cityName;
            cityNameText.color = city.isCapital
                ? new Color(1f, 0.85f, 0.2f) // gold for capital
                : Color.white;
        }

        // Stats line
        if (cityStatsText != null)
        {
            string happColor = city.happiness >= 70 ? "00ff00"
                : city.happiness >= 40 ? "ffff00"
                : "ff4444";

            string crimeColor = city.crime <= 30 ? "00ff00"
                : city.crime <= 60 ? "ffff00"
                : "ff4444";

            cityStatsText.text =
                "Pop: " + city.population +
                "   <color=#" + happColor + ">Happiness " +
                city.happiness + "%</color>" +
                "   <color=#" + crimeColor + ">Crime " +
                city.crime + "%</color>";
        }
    }

    // =========================================
    // RENAME FLOW
    // =========================================
  public  void OnRenameClicked()
    {
        if (renameGroup != null)
            renameGroup.SetActive(true);

        if (renameInputField != null)
        {
            renameInputField.text = city.cityName;
            renameInputField.Select();
            renameInputField.ActivateInputField();
        }

        if (renameButton != null)
            renameButton.gameObject.SetActive(false);
    }

  public  void OnConfirmRename()
    {
        string newName = renameInputField != null
            ? renameInputField.text.Trim()
            : "";

        if (!string.IsNullOrEmpty(newName) &&
            panel != null)
        {
            panel.RenameCity(city, newName);
        }

        CloseRenameGroup();
    }

   public void OnCancelRename()
    {
        CloseRenameGroup();
    }

    void CloseRenameGroup()
    {
        if (renameGroup != null)
            renameGroup.SetActive(false);

        if (renameButton != null)
            renameButton.gameObject.SetActive(true);

        // Refresh display in case name changed
        RefreshDisplay();
    }

    // =========================================
    // LIVE UPDATE
    // Stats change each game tick — refresh
    // display every frame while panel is open.
    // =========================================
    void Update()
    {
        if (city == null) return;
        RefreshDisplay();
    }
}