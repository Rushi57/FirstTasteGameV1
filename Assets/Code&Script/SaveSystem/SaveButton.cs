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
        SaveData data = new SaveData();

        // TODO: replace these with YOUR real game values
        data.coins = 0;               // e.g. PlayerStats.Instance.coins
        data.currentLevel = 1;        // e.g. LevelManager.Instance.currentLevel
        data.unlockedLevels.Add(1);   // e.g. copy your unlocked list here
        data.tutorialDone = true;     // e.g. TutorialManager.IsFinished

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