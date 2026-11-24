using UnityEngine;

public class MobilePerformanceSetup : MonoBehaviour
{
    [SerializeField] int targetFPS = 60;
    [SerializeField] int renderWidth = 1280;

    void Awake()
    {
     
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFPS;

       
        float aspect = (float)Screen.height / Screen.width;
        int h = Mathf.RoundToInt(renderWidth * aspect);
        if (Screen.width > renderWidth)
            Screen.SetResolution(renderWidth, h, true);

        
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}