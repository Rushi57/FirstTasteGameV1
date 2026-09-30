using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreboardButtons : MonoBehaviour
{
    public string mapSceneName = "MapScene";
    
    public void GoToMap()
    {
        LoadingManager.LoadNextScene(mapSceneName);
    }
}
