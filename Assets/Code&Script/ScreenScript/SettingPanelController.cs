using System.Collections;
using UnityEngine;

public class SettingPanelController : MonoBehaviour
{
    [SerializeField] Animator animator;   // SettingPanel's Animator
    [SerializeField] GameObject panel;

    // Must match your state names exactly (check in the Animator window)
    const string OpenIdle = "SettingAnimationOPENIdle";
    const string CloseIdle = "SettingAnimationCLOSEIdle";

    bool isOpen, isBusy;

    void Awake()
    {
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    public void Open()
    {
        if (isBusy || isOpen) return;     // ignore spam taps
        panel.SetActive(true);
        StartCoroutine(Play("OpenSetting", "CloseSetting", OpenIdle, true));
    }

    public void Close()
    {
        if (isBusy || !isOpen) return;    // ignore Exit while still opening
        StartCoroutine(Play("CloseSetting", "OpenSetting", CloseIdle, false));
      
    }

    IEnumerator Play(string set, string clear, string targetState, bool opening)
    {
        isBusy = true;
        animator.ResetTrigger(clear);
        animator.ResetTrigger(set);
        animator.SetTrigger(set);

        // Wait until the Animator has actually reached the idle state
        float timeout = 3f;
        while (timeout > 0f)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(targetState) && !animator.IsInTransition(0)) break;
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        isOpen = opening;
        if (!opening) panel.SetActive(false);
        isBusy = false;
    }
    public void ForceClosed()
    {
        animator.ResetTrigger("OpenSetting");
        animator.ResetTrigger("CloseSetting");
        animator.Play(CloseIdle, 0, 0f);
        isOpen = false;
        isBusy = false;
    }
}