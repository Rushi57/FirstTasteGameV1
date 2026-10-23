using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the same GameObject as the fridge ScrollRect ("ScrollingUp"),
/// next to the TutorialInteractable (id ScrollFridgeContent).
/// When the player has scrolled far enough to reveal the other part of the
/// fridge and the scroll has settled, it calls ReportScroll() on the
/// TutorialInteractable.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class FridgeScrollTutorialReporter : MonoBehaviour
{
    [Tooltip("The TutorialInteractable with id ScrollFridgeContent. Auto-found on this object if left empty.")]
    [SerializeField] private TutorialInteractable interactable;

    [Tooltip("How far (0-1) the player must scroll before it counts as done.")]
    [SerializeField, Range(0f, 1f)] private float completeThreshold = 0.9f;

    [Tooltip("ON = target section is at the bottom (normalized y = 0, player swipes up). OFF = target section is at the top (y = 1).")]
    [SerializeField] private bool targetAtBottom = true;

    private ScrollRect scrollRect;
    private bool reported;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (interactable == null) interactable = GetComponent<TutorialInteractable>();
    }

    private void OnEnable() => reported = false;

    private void Update()
    {
        // Unity: verticalNormalizedPosition 1 = top, 0 = bottom
        float y = scrollRect.verticalNormalizedPosition;
        float progress = targetAtBottom ? 1f - y : y;   // 0 = start, 1 = target fully shown

        // Player moved back away from the target -> allow reporting again
        if (reported && progress < completeThreshold - 0.2f)
        {
            reported = false;
            return;
        }

        if (reported) return;

        // Reached the target and the scroll has settled (finger up, inertia done)
        bool settled = scrollRect.velocity.sqrMagnitude < 1f;
        if (progress >= completeThreshold && settled)
        {
            reported = true;
            if (interactable != null) interactable.ReportScroll();
        }
    }
}