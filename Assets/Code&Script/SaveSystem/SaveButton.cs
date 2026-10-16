using UnityEngine;
using UnityEngine.UI;

// Attach to SaveBtn in MapScene.
[RequireComponent(typeof(Button))]
public class SaveButton : MonoBehaviour
{
    [SerializeField] private GameObject savedMessage; // optional "Game Saved!" popup

    SaveData data = GameSession.GetOrCreateData();

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnSaveClicked);
    }

    public void OnSaveClicked()
    {
        // Reuse the current session data so levelScores (the stars) are kept
        SaveData data = GameSession.GetOrCreateData();

        // data.currentLevel = 1;   <- remove, this resets progress on every save
        if (!data.unlockedLevels.Contains(1)) data.unlockedLevels.Add(1);

        TutorialManager.WriteCompletedTutorialsTo(data);
        data.tutorialDone = data.IsTutorialCompleted("main_tutorial_MAP");

        SaveSystem.Save(data);

        if (savedMessage != null)
        {
            savedMessage.SetActive(true);
            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), 1.5f);
        }
    }

    private void HideMessage() => savedMessage.SetActive(false);
}