using UnityEngine;

public class StopLoopOnEnable : MonoBehaviour
{
    void OnEnable()
    {
        SFXManager.Instance?.StopLoop();
    }
}