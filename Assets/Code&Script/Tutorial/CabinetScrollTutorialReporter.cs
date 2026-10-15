using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the same GameObject as the ScrollRect ("Scrolling").
/// When the player has scrolled far enough to reveal the utensils and the
/// scroll has settled, it calls ReportScroll() on the TutorialInteractable.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class CabinetScrollTutorialReporter : MonoBehaviour
{
    [Tooltip("The TutorialInteractable with id ScrollContentCabinet. Auto-found on this object if left empty.")]
    [SerializeField] private TutorialInteractable interactable;

    [Tooltip("How far (0-1) the player must scroll before it counts as done.")]
    [SerializeField, Range(0f, 1f)] private float completeThreshold = 0.9f;

    [Tooltip("ON = utensils are at the right end (normalized x = 1, player swipes left). OFF = utensils at the left end (x = 0).")]
    [SerializeField] private bool utensilsAtEnd = true;

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
        float x = scrollRect.horizontalNormalizedPosition;
        float progress = utensilsAtEnd ? x : 1f - x;   // 0 = start, 1 = utensils fully shown

        // Player moved back away from the end -> allow reporting again
        if (reported && progress < completeThreshold - 0.2f)
        {
            reported = false;
            return;
        }

        if (reported) return;

        // Reached the end and the scroll has settled (finger up, inertia done)
        bool settled = scrollRect.velocity.sqrMagnitude < 1f;
        if (progress >= completeThreshold && settled)
        {
            reported = true;
            interactable.ReportScroll();
        }
    }
}