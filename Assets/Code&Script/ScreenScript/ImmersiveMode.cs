using System.Collections;
using UnityEngine;

public class ImmersiveMode : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(KeepHidden());
    }

    IEnumerator KeepHidden()
    {
        var wait = new WaitForSeconds(1f);
        while (true)
        {
            HideSystemBars();
            yield return wait;
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) HideSystemBars();
    }

    void HideSystemBars()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                    using (var decorView = window.Call<AndroidJavaObject>("getDecorView"))
                    {
                        int flags = 0x00000100 | 0x00000200 | 0x00000400
                                  | 0x00000002 | 0x00000004 | 0x00001000;
                        decorView.Call("setSystemUiVisibility", flags);
                    }
                }));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Immersive mode failed: " + e.Message);
        }
#endif
    }
}