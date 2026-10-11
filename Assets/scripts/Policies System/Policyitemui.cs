using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PolicyItemUI : MonoBehaviour
{
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI policyNameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI toggleLabel;
    public Button toggleButton;
    public Image activeIndicator;

    [Header("Colors")]
    public Color activeColor = new Color(0.2f, 0.9f, 0.2f);
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);
    public Color lockedColor = new Color(0.8f, 0.3f, 0.3f);

    private PolicyType policy;
    private PolicyPanelUI panelUI;

    public void Setup(PolicyType p, PolicyPanelUI panel)
    {
        policy = p;
        panelUI = panel;

        if (toggleButton != null)
        {
            // RemoveAllListeners prevents duplicate
            // listeners if the item is reused or rebuilt
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(OnToggleClicked);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (PolicyManager.Instance == null) return;

        bool active = PolicyManager.Instance.IsActive(policy);
        bool allowed = PolicyManager.Instance.IsAllowedForGovernment(policy);
        bool crisisOk = policy != PolicyType.MartialLaw ||
                        PolicyManager.Instance.IsCrisisActive() || active;
        bool canToggle = allowed && (active || crisisOk);
        int cost = PolicyManager.Instance.GetCostPerTick(policy);

        if (policyNameText != null)
            policyNameText.text = PolicyManager.Instance.GetPolicyName(policy);
        if (categoryText != null)
            categoryText.text = PolicyManager.Instance.GetCategory(policy);

        if (costText != null)
            costText.text = cost > 0 ? $"${cost}/tick" : "Free";

        if (descText != null)
            descText.text = PolicyManager.Instance.GetActivateDescription(policy);

        if (statusText != null)
        {
            if (!allowed)
            {
                statusText.text = "<color=#ff4444>Locked (Government)</color>";
                statusText.color = lockedColor;
            }
            else if (policy == PolicyType.MartialLaw && !active &&
                     !PolicyManager.Instance.IsCrisisActive())
            {
                statusText.text = "<color=#ff8800>Requires crisis</color>";
                statusText.color = lockedColor;
            }
            else if (active)
            {
                statusText.text = "<color=#00dd00>Active</color>";
                statusText.color = activeColor;
            }
            else
            {
                statusText.text = "<color=#888888>Inactive</color>";
                statusText.color = inactiveColor;
            }
        }

        if (toggleButton != null)
            toggleButton.interactable = canToggle;
        if (toggleLabel != null)
            toggleLabel.text = active ? "Lift" : "Enact";

        if (activeIndicator != null)
            activeIndicator.color = active ? activeColor : inactiveColor;
    }

    private int lastClickFrame = -1;

    public void OnToggleClicked()
    {
        // Guard against double-fire when button is wired
        // both in Inspector and via AddListener in code
        if (Time.frameCount == lastClickFrame) return;
        lastClickFrame = Time.frameCount;

        if (PolicyManager.Instance == null) return;
        PolicyManager.Instance.Toggle(policy);
        Refresh();
    }
}