using System.Collections;
using UnityEngine;

public class SceneChanger : MonoBehaviour
{

    [Tooltip("Optional ")]
    public TutorialInteractable tutorialTagLevel1;
    [Tooltip("Optional ")]
    public TutorialInteractable tutorialTagLevelPlay;
    [Tooltip("Seconds to wait after reporting the tutorial tap before loading the next scene, so the player has time to see the tutorial react (dialogue update, pulse stopping) before the scene changes.")]
    public float tutorialTapDelay = 1.5f;
    // Generic method to load any scene by name through the LoadingScene
    public void NewGame(string sceneName)
    {
        TutorialManager.ResetAllTutorials();
        LoadingManager.LoadNextScene(sceneName);
    }

    public void HomeGame(string sceneName)
    {
       
        LoadingManager.LoadNextScene(sceneName);
    }

    public void QuitInGame(string sceneName)
    {
        TutorialManager.Instance?.SkipTutorial();
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

    public void OnClickPlay(string sceneName)
    {
        tutorialTagLevelPlay?.ReportTap();
        Debug.Log("Play Button!!!!");
        StartCoroutine(LoadSceneAfterDelay(sceneName, tutorialTapDelay));

    }

    private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadingManager.LoadNextScene(sceneName);
    }
}