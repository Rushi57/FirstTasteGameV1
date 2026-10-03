using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [Header("Buttons")]
    public Button replayButton;
    public Button quitButton;

    [Header("Quit")]
    [Tooltip("Scene to load when the player tap Quit ")]
    public string quitSceneName = "Map Scene";

    private void Start()
    {
        if (replayButton != null) replayButton.onClick.AddListener(OnReplay);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
    }
    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnGameOver += HandleGameOver;
    }
    private void HandleGameOver()
    {
        TutorialManager.Instance?.AbortTutorial();
    }
    private void OnReplay()
    {
        Time.timeScale = 1f;   // reset before reloading
        var tutorial = TutorialManager.Instance;

        if (tutorial != null && !tutorial.HasCompletedTutorial())
            tutorial.ResetProgress();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    private void OnQuit()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(quitSceneName);
    }
}
