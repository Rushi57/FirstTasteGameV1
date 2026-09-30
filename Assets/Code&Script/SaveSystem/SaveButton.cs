using UnityEngine;
using UnityEngine.UI;

// Attach to SaveBtn in MapScene.
[RequireComponent(typeof(Button))]
public class SaveButton : MonoBehaviour
{
    [SerializeField] private GameObject savedMessage; // optional "Game Saved!" popup

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnSaveClicked);
    }

    public void OnSaveClicked()
    {
        // Reuse the current session data so levelScores (the stars) are kept
        SaveData data = GameSession.GetOrCreateData();

        // TODO: replace these with YOUR real game values
        data.coins = 0;
        data.currentLevel = 1;
        if (!data.unlockedLevels.Contains(1)) data.unlockedLevels.Add(1);   // avoids duplicates on every save
        data.tutorialDone = true;

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