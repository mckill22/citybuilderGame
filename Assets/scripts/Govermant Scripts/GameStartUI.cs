using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameStartUI : MonoBehaviour
{
    public static GameStartUI Instance;

    // ──────────────────────────────────────
    // PANEL ROOTS
    // ──────────────────────────────────────
    [Header("Panels")]
    public GameObject governmentPanel;
    public GameObject leaderNamePanel;
    public GameObject capitalNamePanel;
    public GameObject countryNamePanel;      // NEW — step 4
    public GameObject placeTownHallPanel;

    // ──────────────────────────────────────
    // GOVERNMENT SELECTION
    // ──────────────────────────────────────
    [Header("Government")]
    public TMP_Text governmentDescriptionText;

    // ──────────────────────────────────────
    // LEADER NAME
    // ──────────────────────────────────────
    [Header("Leader Name")]
    public TMP_InputField leaderNameInput;
    public TMP_Text leaderNameError;

    // ──────────────────────────────────────
    // CAPITAL NAME
    // ──────────────────────────────────────
    [Header("Capital Name")]
    public TMP_InputField capitalNameInput;
    public TMP_Text capitalNameError;

    // ──────────────────────────────────────
    // COUNTRY NAME  ← NEW
    // ──────────────────────────────────────
    [Header("Country Name")]
    public TMP_InputField countryNameInput;
    public TMP_Text countryNameError;

    // ──────────────────────────────────────
    // REFERENCES
    // ──────────────────────────────────────
    [Header("References")]
    public BuildManager buildManager;

    // ──────────────────────────────────────
    // INTERNAL STATE
    // ──────────────────────────────────────
    private GovernmentType selectedGov = GovernmentType.Democracy;

    private enum SetupState
    {
        ChooseGovernment,
        EnterLeaderName,
        EnterCapitalName,
        EnterCountryName,   // NEW
        PlaceTownHall,
        Done
    }

    private SetupState state = SetupState.ChooseGovernment;

    void Awake() { Instance = this; }

    void Start()
    {
        ShowPanel(SetupState.ChooseGovernment);
        RefreshGovernmentDescription(selectedGov);
    }

    // =========================================
    // STEP 1 — GOVERNMENT
    // =========================================
    public void SelectGovernment(int govIndex)
    {
        selectedGov = (GovernmentType)govIndex;
        RefreshGovernmentDescription(selectedGov);
    }

    public void ConfirmGovernment()
    {
        state = SetupState.EnterLeaderName;
        ShowPanel(SetupState.EnterLeaderName);
    }

    void RefreshGovernmentDescription(GovernmentType gov)
    {
        if (governmentDescriptionText == null) return;

        string desc;
        switch (gov)
        {
            case GovernmentType.Democracy:
                desc = "<b>Democracy</b>\nBalanced rule. Tax capped at 30%.\nCitizens enjoy a happiness bonus.";
                break;
            case GovernmentType.Dictatorship:
                desc = "<b>Dictatorship</b>\nHigh tax (up to 60%). No happiness floor.\nMore crime. Slight military boost.";
                break;
            case GovernmentType.Monarchy:
                desc = "<b>Monarchy</b>\nCrown bonus: +20% tax income.\nCrime slightly reduced.";
                break;
            case GovernmentType.Communism:
                desc = "<b>Communism</b>\nNo commercial income — flat state wage.\nProduction +20%. Slight happiness penalty.";
                break;
            case GovernmentType.Fascism:
                desc = "<b>Fascism</b>\nMilitary production +25%.\nCrime strongly suppressed. Happiness -15.";
                break;
            default:
                desc = "";
                break;
        }

        governmentDescriptionText.text = desc;
    }

    // =========================================
    // STEP 2 — LEADER NAME
    // =========================================
    public void ConfirmLeaderName()
    {
        string name = leaderNameInput != null
            ? leaderNameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(name))
        {
            ShowError(leaderNameError, "Please enter a leader name.");
            return;
        }

        HideError(leaderNameError);
        state = SetupState.EnterCapitalName;
        ShowPanel(SetupState.EnterCapitalName);
    }

    // =========================================
    // STEP 3 — CAPITAL NAME
    // =========================================
    public void ConfirmCapitalName()
    {
        string name = capitalNameInput != null
            ? capitalNameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(name))
        {
            ShowError(capitalNameError, "Please enter a capital name.");
            return;
        }

        HideError(capitalNameError);
        state = SetupState.EnterCountryName;
        ShowPanel(SetupState.EnterCountryName);
    }

    // =========================================
    // STEP 4 — COUNTRY NAME  ← NEW
    // =========================================
    public void ConfirmCountryName()
    {
        string name = countryNameInput != null
            ? countryNameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(name))
        {
            ShowError(countryNameError, "Please enter a country name.");
            return;
        }

        HideError(countryNameError);

        // All data collected — pass to CityManager
        string leaderName = leaderNameInput != null ? leaderNameInput.text.Trim() : "Leader";
        string capitalName = capitalNameInput != null ? capitalNameInput.text.Trim() : "Capital";

        CityManager.Instance.SetupData(
            selectedGov,
            leaderName,
            capitalName,
            name);         // country name

        state = SetupState.PlaceTownHall;
        ShowPanel(SetupState.PlaceTownHall);

        if (buildManager != null)
            buildManager.BeginTownHallPlacement();
    }

    // =========================================
    // STEP 5 — TOWN HALL PLACED
    // =========================================
    public void OnCapitalFounded()
    {
        state = SetupState.Done;
        gameObject.SetActive(false);
    }

    // =========================================
    // HELPERS
    // =========================================
    void ShowPanel(SetupState s)
    {
        if (governmentPanel != null)
            governmentPanel.SetActive(s == SetupState.ChooseGovernment);
        if (leaderNamePanel != null)
            leaderNamePanel.SetActive(s == SetupState.EnterLeaderName);
        if (capitalNamePanel != null)
            capitalNamePanel.SetActive(s == SetupState.EnterCapitalName);
        if (countryNamePanel != null)
            countryNamePanel.SetActive(s == SetupState.EnterCountryName);
        if (placeTownHallPanel != null)
            placeTownHallPanel.SetActive(s == SetupState.PlaceTownHall);
    }

    void ShowError(TMP_Text errorText, string msg)
    {
        if (errorText == null) return;
        errorText.text = msg;
        errorText.gameObject.SetActive(true);
    }

    void HideError(TMP_Text errorText)
    {
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

    public bool IsSetupDone() => state == SetupState.Done;

    // Camera should be frozen only during name/gov entry steps.
    // Unfreezes at PlaceTownHall so the player can look around.
    public bool IsCameraFrozen() =>
        state != SetupState.PlaceTownHall &&
        state != SetupState.Done;
}