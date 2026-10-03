using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreboardButtons : MonoBehaviour
{
    public string mapSceneName = "MapScene";
    
    public void GoToMap()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.SkipTutorial();

        LoadingManager.LoadNextScene(mapSceneName);
    }
}
