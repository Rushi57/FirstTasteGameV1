using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    // Stores the target scene name globally across scene loads
    public static string targetScene;

    [Header("UI Components")]
    [SerializeField] private Slider progressBar;

    private void Start()
    {
        // Default fallback if no scene was specified
        if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = "MainMenu";
        }

        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            // operation.progress goes from 0.0 to 0.9 while loading
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            // Once fully loaded ( progress == 1.0 ), allow the scene to activate
            if (operation.progress >= 0.9f)
            {
                // Optional brief delay so the bar fills completely before switching
                yield return new WaitForSeconds(0.2f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    // Call this method from any script or button to switch scenes via LoadingScene
    public static void LoadNextScene(string sceneName)
    {
        targetScene = sceneName;
        SceneManager.LoadScene("LoadingScene");
    }
}