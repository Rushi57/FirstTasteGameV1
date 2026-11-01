using UnityEngine;

/// <summary>
/// Hides a UI group (e.g. UIPanelButton) while the tutorial is running,
/// and shows it again when the tutorial is not active.
/// Put this on a DIFFERENT, always-active object (e.g. CodeForUIGameObject),
/// NOT on UIPanelButton itself.
/// </summary>
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
            target.SetActive(!tutorialPlaying);   // hide while playing, show otherwise
    }
}