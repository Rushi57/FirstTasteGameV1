using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Attach to LoadGameBtn in MainMenu.
[RequireComponent(typeof(Button))]
public class LoadGameButton : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "MapScene";
    [Header("No Save Message")]
    [SerializeField] private GameObject noSavePanel;      // LoadGameMainMenuPanel (the inactive parent)
    [SerializeField] private Animator noSaveAnimator;     // Animator on LoadMainMenuBorder
    [SerializeField] private string openTrigger = "MainMenuAnimOpen";
    [SerializeField] private string closeTrigger = "MainMenuAnimClose";

    // Wire these in the Inspector via each Button's OnClick() list.
    public void OnLoadClicked()
    {
        SaveData data = SaveSystem.Load();

        if (data == null)
        {
            Debug.Log("No save file found.");
            ShowNoSaveMessage();
            return;
        }

        GameSession.Data = data;                       // static, so it survives LoadingScene
        LoadingManager.LoadNextScene(mapSceneName);    // LoadingScene -> MapScene
    }

    public void OnCloseClicked()
    {
        if (noSaveAnimator != null)
        {
            noSaveAnimator.ResetTrigger(openTrigger);
            noSaveAnimator.SetTrigger(closeTrigger);
        }
    }

    private void ShowNoSaveMessage()
    {
        // The Animator can't run while its parent is inactive, so turn the panel on first
        if (noSavePanel != null && !noSavePanel.activeSelf)
            noSavePanel.SetActive(true);

        if (noSaveAnimator != null)
        {
            noSaveAnimator.ResetTrigger(openTrigger);
            noSaveAnimator.SetTrigger(openTrigger);
        }
    }
}