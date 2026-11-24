using UnityEngine;

public class HideWhileTutorial : MonoBehaviour
{
    [Tooltip("Drag UIPanelButton here.")]
    public GameObject target;

    private bool lastState = false;
    private bool initialized = false;

    private void Update()
    {
        bool tutorialPlaying = TutorialManager.Instance != null
                            && TutorialManager.Instance.IsActive;

        if (initialized && tutorialPlaying == lastState) return;

        initialized = true;
        lastState = tutorialPlaying;

        if (target != null)
            target.SetActive(!tutorialPlaying); 
    }
}