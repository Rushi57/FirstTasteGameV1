using UnityEngine;

public class PlaySfxOnEnable : MonoBehaviour
{
    void OnEnable()
    {
        if(SFXManager.Instance != null)
            SFXManager.Instance.Play(SFXManager.Instance.scorebordePanel);
    }
}
