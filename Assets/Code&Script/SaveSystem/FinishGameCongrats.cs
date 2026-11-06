using UnityEngine;
using UnityEngine.UI;

// Attach to a controller in MapScene that is ALWAYS ACTIVE (e.g. CodeForUIGameObject).
// CongratsFinishLevelGameObject can stay inactive.
public class FinishGameCongrats : MonoBehaviour
{
    [SerializeField] private GameObject congratsPanel;     // CongratsFinishLevelGameObject
    [SerializeField] private Button continueButton;        // Continue button inside the panel
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private int totalLevels = 10;
    [SerializeField] private int requiredStars = 3;

    private void Awake()
    {
        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
    }

    private void Start()
    {
        // Runs every time the map loads (e.g. after finishing a level)
        CheckAndShow();
    }

    // Can also be called right after a level result is saved.
    public void CheckAndShow()
    {
        SaveData data = GameSession.Data;
        if (data == null)
        {
            Debug.Log("[FinishCongrats] No GameSession.Data (new game / started MapScene directly) - not showing.");
            return;
        }
        if (data.congratsShown)
        {
            Debug.Log("[FinishCongrats] congratsShown is already true in this save - not showing.");
            return;
        }

        if (AllLevelsFullStars(data))
        {
            Debug.Log("[FinishCongrats] All levels 3 stars - showing panel.");
            congratsPanel.SetActive(true);
        }
    }

    private bool AllLevelsFullStars(SaveData data)
    {
        bool all = true;
        for (int level = 1; level <= totalLevels; level++)
        {
            int stars = data.GetLevelStars(level);
            if (stars < requiredStars)
            {
                Debug.Log($"[FinishCongrats] Level {level} has {stars} star(s) in SaveData (need {requiredStars}).");
                all = false;
            }
        }
        return all;
    }

    // Right-click the component header in the Inspector -> "Test Show Panel"
    [ContextMenu("Test Show Panel")]
    private void TestShowPanel() => congratsPanel.SetActive(true);

    // Right-click -> "Reset Congrats Flag" to test again on the same save
    [ContextMenu("Reset Congrats Flag")]
    private void ResetFlag()
    {
        if (GameSession.Data != null) GameSession.Data.congratsShown = false;
    }

    private void OnContinue()
    {
        SaveData data = GameSession.GetOrCreateData();
        data.congratsShown = true;

        int slot = SaveSystem.PickAutoSaveSlot();
        SaveSystem.Save(data, slot);
        GameSession.CurrentSlot = slot;
        GameSession.PendingSavedSlotMessage = slot;   // MainMenu reads this

        congratsPanel.SetActive(false);
        LoadingManager.LoadNextScene(mainMenuSceneName);
    }
}