using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows InActiveMessagePanel when the player hasn't touched anything for a few seconds.
///  - Ingredient stage -> "tap an ingredient" message (separate text)
///  - Cut / Cooking stage -> the current step from the active CookingPrepListUI
/// Tapping anywhere on the panel closes it.
/// </summary>
public class InactivityHintController : MonoBehaviour
{
    [Header("References")]
    public CookingPrepPanelController prepController;   // to know which stage is active
    public GameObject inactiveMessagePanel;             // InActiveMessagePanel (root)
    public TMP_Text messageText;                        // the Text (TMP) inside the border

    [Header("Timing")]
    public float idleSeconds = 8f;

    [Header("Messages")]
    [TextArea]
    public string ingredientStageMessage =
        "Tap an ingredient on the list\nto place it on the table!";
    [TextArea] public string stepMessageFormat = "Next step:\n{0}";
    [TextArea] public string fallbackMessage = "Keep going! Follow the recipe steps.";

    private float idleTimer;

    private void Awake()
    {
        // Make the whole panel a "tap anywhere to close" button.
        if (inactiveMessagePanel != null)
        {
            var btn = inactiveMessagePanel.GetComponent<Button>();
            if (btn == null) btn = inactiveMessagePanel.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(Hide);
            inactiveMessagePanel.SetActive(false);
        }
    }

    private void OnEnable() => idleTimer = 0f;

    private void Update()
    {
        // Only count idle time while a recipe is actually being cooked
        if (prepController == null || prepController.currentRecipe == null) return;

        // Panel is open: wait for the player to tap it
        if (inactiveMessagePanel != null && inactiveMessagePanel.activeSelf) return;

        if (PlayerIsInteracting())
        {
            idleTimer = 0f;
            return;
        }

        idleTimer += Time.unscaledDeltaTime;
        if (idleTimer >= idleSeconds)
            Show();
    }

    private bool PlayerIsInteracting()
    {
        // Legacy Input. Covers touch and mouse (drag included).
        return Input.touchCount > 0 || Input.GetMouseButton(0);
    }

    private void Show()
    {
        if (inactiveMessagePanel == null) return;

        messageText.text = BuildMessage();
        inactiveMessagePanel.SetActive(true);
        inactiveMessagePanel.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        if (inactiveMessagePanel != null) inactiveMessagePanel.SetActive(false);
        idleTimer = 0f;
    }

    private string BuildMessage()
    {
        // Stage 1: ingredient list buttons
        if (prepController.ingredientPrepPanel != null && prepController.ingredientPrepPanel.activeInHierarchy)
            return ingredientStageMessage;

        // Stage 2 / 3: whichever CookingPrepListUI is on the active panel
        var list = CookingPrepListUI.Instance;
        if (list != null)
        {
            string step = list.CurrentStepText;
            if (!string.IsNullOrEmpty(step))
                return string.Format(stepMessageFormat, step);
        }

        return fallbackMessage;
    }
}