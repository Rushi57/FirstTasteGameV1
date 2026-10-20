using UnityEngine;

public class MobilePerformanceSetup : MonoBehaviour
{
    [SerializeField] int targetFPS = 60;
    [SerializeField] int renderWidth = 1280; // render width; height follows the device aspect

    void Awake()
    {
        // Mobile defaults to 30 FPS and vSync can fight with the frame cap
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFPS;

        // Render at ~720p wide instead of the phone's native resolution
        float aspect = (float)Screen.height / Screen.width;
        int h = Mathf.RoundToInt(renderWidth * aspect);
        if (Screen.width > renderWidth)
            Screen.SetResolution(renderWidth, h, true);

        // Stop the screen from dimming and keep the device awake while playing
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}