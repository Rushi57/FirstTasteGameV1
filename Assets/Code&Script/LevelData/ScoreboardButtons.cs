using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScoreboardButtons : MonoBehaviour
{
    public string mapSceneName = "MapScene";
    public Button rePlayBtn;

    private void HandleGameOver()
    {
        TutorialManager.Instance?.AbortTutorial();
    }
    public void GoToMap()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.SkipTutorial();

        LoadingManager.LoadNextScene(mapSceneName);
    }

    public void rePlayBt()
    {
        Time.timeScale = 1f;   // reset before reloading
        var tutorial = TutorialManager.Instance;

        if (tutorial != null && !tutorial.HasCompletedTutorial())
            tutorial.ResetProgress();
    }
    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnGameOver += HandleGameOver;
    }
}
