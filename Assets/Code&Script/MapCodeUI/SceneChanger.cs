using UnityEngine;

public class SceneChanger : MonoBehaviour
{
    // Generic method to load any scene by name through the LoadingScene
    public void LoadScene(string sceneName)
    {
        LoadingManager.LoadNextScene(sceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Exit Game");
        Application.Quit();
    }
}