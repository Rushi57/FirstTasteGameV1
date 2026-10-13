using UnityEngine;

public class MobilePauseHandler : MonoBehaviour
{
    private static MobilePauseHandler instance;
    private float previousTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null; // needed if domain reload is disabled

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }
        else
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = false;
        }
    }
}