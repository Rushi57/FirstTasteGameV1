using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InactivityHintController : MonoBehaviour
{
    [Header("References")]
    public CookingPrepPanelController prepController;
    public GameObject inactiveMessagePanel;
    public TMP_Text messageText;

    [Header("Don't show while these are active")]
    public GameObject[] blockingPanels;

    [Header("Timing")]
    public float idleSeconds = 8f;

    [Header("Debug")]
    public bool debugLogs = true;

    [Header("Messages")]
    [TextArea]
    public string ingredientStageMessage =
        "Tap an ingredient on the list\nto place it on the table!";
    [TextArea] public string stepMessageFormat = "Next step:\n{0}";
    [TextArea] public string fallbackMessage = "Keep going! Follow the recipe steps.";

    private float idleTimer;
    private string lastReason = "";

    public Animator settingsAnimator;                          // Animator on GameSettingsPanel
    public string settingsClosedState = "IngSettingCloseIdle"; // resting "closed" state
    private void Awake()
    {
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
        if (prepController == null) { Report("prepController is NOT assigned"); return; }
        if (prepController.currentRecipe == null) { Report("currentRecipe is NULL (recipe not set yet)"); return; }

        GameObject blocker = GetBlocker();
        if (blocker != null)
        {
            Report($"Blocked by active object: {blocker.name}");
            if (inactiveMessagePanel != null && inactiveMessagePanel.activeSelf)
                inactiveMessagePanel.SetActive(false);
            idleTimer = 0f;
            return;
        }

        if (inactiveMessagePanel != null && inactiveMessagePanel.activeSelf)
        {
            Report("Hint is open, waiting for tap");
            return;
        }

        if (PlayerIsInteracting())
        {
            Report("Player is touching/clicking (timer reset)");
            idleTimer = 0f;
            return;
        }

        Report($"Counting idle time...");
        idleTimer += Time.unscaledDeltaTime;
        if (idleTimer >= idleSeconds)
            Show();
    }

    private bool PlayerIsInteracting()
    {
        return Input.touchCount > 0 || Input.GetMouseButton(0);
    }

    private void Show()
    {
        if (inactiveMessagePanel == null) return;
        if (messageText != null) messageText.text = BuildMessage();
        inactiveMessagePanel.SetActive(true);
        inactiveMessagePanel.transform.SetAsLastSibling();
        if (debugLogs) Debug.Log("[InactivityHint] SHOWN");
    }

    public void Hide()
    {
        if (inactiveMessagePanel != null) inactiveMessagePanel.SetActive(false);
        idleTimer = 0f;
    }

    private string BuildMessage()
    {
        if (prepController.ingredientPrepPanel != null && prepController.ingredientPrepPanel.activeInHierarchy)
            return ingredientStageMessage;

        var list = CookingPrepListUI.Instance;
        if (list != null)
        {
            string step = list.CurrentStepText;
            if (!string.IsNullOrEmpty(step))
                return string.Format(stepMessageFormat, step);
        }
        return fallbackMessage;
    }

    private GameObject GetBlocker()
    {
        if (IsSettingsOpen()) return settingsAnimator.gameObject;
        foreach (var go in blockingPanels)
            if (go != null && go.activeInHierarchy) return go;
        return null;
    }

    // Logs only when the reason CHANGES, so the Console doesn't flood.
    private void Report(string reason)
    {
        if (!debugLogs || reason == lastReason) return;
        lastReason = reason;
        Debug.Log($"[InactivityHint] {reason}");
    }

    // True whenever the settings window is anything other than fully closed
    private bool IsSettingsOpen()
    {
        if (settingsAnimator == null) return false;
        // Panel is "hidden" when scaled to (almost) zero
        return settingsAnimator.transform.localScale.x > 0.01f;
    }
}