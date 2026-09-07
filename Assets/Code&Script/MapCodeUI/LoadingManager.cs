using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public static string targetScene;

    [Header("UI Components")]
    [SerializeField] private Slider progressBar;

    [Header("Speed Settings")]
    [Tooltip("How fast the slider fills per second (e.g., 0.5 = 2 seconds for a full bar)")]
    [SerializeField] private float fillSpeed = 0.5f;

    private void Start()
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = "MainMenu";
        }

        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);

        // Prevent the scene from switching automatically as soon as loading finishes
        operation.allowSceneActivation = false;

        float targetProgress = 0f;

        while (!operation.isDone)
        {
            // Normalize progress (Unity reports 0.0 to 0.9 during load)
            targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            if (progressBar != null)
            {
                // Gradually move current value toward targetProgress based on fillSpeed
                progressBar.value = Mathf.MoveTowards(progressBar.value, targetProgress, fillSpeed * Time.deltaTime);
            }

            // Wait until both the scene is fully loaded AND the UI bar is visually full
            if (operation.progress >= 0.9f && progressBar != null && progressBar.value >= 1f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    public static void LoadNextScene(string sceneName)
    {
        targetScene = sceneName;
        SceneManager.LoadScene("LoadingScene");
    }
}