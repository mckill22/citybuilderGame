using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================
// NEWS ROW UI
// Attach to the news row prefab.
//
// Prefab layout:
//   [colored bar] [date text] [message text]
//
// Unity setup:
//   categoryBar   — Image, tinted by category color
//   dateText      — TMP, shows "15 March 1952"
//   messageText   — TMP, shows event message
// =============================================
public class NewsRowUI : MonoBehaviour
{
    public Image categoryBar;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI messageText;

    public void Setup(NewsEvent evt, Color categoryColor)
    {
        if (categoryBar != null)
            categoryBar.color = categoryColor;

        if (dateText != null)
            dateText.text = evt.date;

        if (messageText != null)
            messageText.text = evt.message;
    }
}