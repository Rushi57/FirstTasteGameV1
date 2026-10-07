using UnityEngine;

public class MobilePauseHandler : MonoBehaviour
{
    private static MobilePauseHandler instance;

    void Awake()
    {
        // Check if an instance of this handler already exists
        if (instance != null && instance != this)
        {
            // Destroy this duplicate because one is already running
            Destroy(gameObject);
            return;
        }

        // Set this object as the permanent instance
        instance = this;

        // Tell Unity NOT to destroy this object when loading new scenes
        DontDestroyOnLoad(gameObject);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // App minimized: freeze game logic and mute sound
            Time.timeScale = 0f;
            AudioListener.pause = true;

            // Clean up memory to help the OS keep the game's snapshot clean
            System.GC.Collect();
        }
        else
        {
            // App resumed: unfreeze game logic and restore sound
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
