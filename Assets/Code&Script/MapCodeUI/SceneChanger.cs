using UnityEngine;

public class SceneChanger : MonoBehaviour
{

    [Tooltip("Optional ")]
    public TutorialInteractable tutorialTagLevel1;
    [Tooltip("Optional ")]
    public TutorialInteractable tutorialTagPlay;
    // Generic method to load any scene by name through the LoadingScene
    public void LoadScene(string sceneName)
    {
        tutorialTagPlay?.ReportTap();
        LoadingManager.LoadNextScene(sceneName);
    }

    public void TapLevel1()
    {
        tutorialTagLevel1?.ReportTap();
        Debug.Log("Level 1 is Tap!!!!");
    }

    public void QuitGame()
    {
        Debug.Log("Exit Game");
        Application.Quit();
    }
}