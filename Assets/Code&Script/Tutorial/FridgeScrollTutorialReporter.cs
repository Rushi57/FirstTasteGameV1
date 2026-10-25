using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class FridgeScrollTutorialReporter : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private TutorialInteractable interactable;

    [SerializeField, Range(0f, 1f)] private float completeThreshold = 0.9f;

    [Tooltip("ON = target section is at the bottom (normalized y = 0, player swipes up). OFF = target section is at the top (y = 1).")]
    [SerializeField] private bool targetAtBottom = true;

    [Tooltip("Speed (px/s) below which the scroll counts as settled.")]
    [SerializeField] private float settledSpeed = 30f;

    private ScrollRect scrollRect;
    private bool reported;
    private bool dragging;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (interactable == null) interactable = GetComponent<TutorialInteractable>();
    }

    private void OnEnable() { reported = false; dragging = false; }

    public void OnBeginDrag(PointerEventData e) => dragging = true;
    public void OnEndDrag(PointerEventData e) => dragging = false;

    private void Update()
    {
        // Unity: verticalNormalizedPosition 1 = top, 0 = bottom
        float y = scrollRect.verticalNormalizedPosition;
        float progress = targetAtBottom ? 1f - y : y;

        if (reported && progress < completeThreshold - 0.2f) { reported = false; return; }
        if (reported) return;

        bool settled = !dragging && scrollRect.velocity.sqrMagnitude < settledSpeed * settledSpeed;
        if (progress >= completeThreshold && settled)
        {
            reported = true;
            if (interactable != null) interactable.ReportScroll();
        }
    }
}