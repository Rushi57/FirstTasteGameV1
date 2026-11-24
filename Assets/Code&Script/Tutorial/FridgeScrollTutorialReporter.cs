using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class FridgeScrollTutorialReporter : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private TutorialInteractable interactable;

    [Tooltip("Drag the 'Content' object here (it needs a CanvasGroup).")]
    [SerializeField] private CanvasGroup contentGroup;

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

    private void OnDisable()
    {
      
        if (contentGroup != null) contentGroup.blocksRaycasts = true;
    }

    public void OnBeginDrag(PointerEventData e) => dragging = true;
    public void OnEndDrag(PointerEventData e) => dragging = false;

    private void Update()
    {
        UpdateItemLock();

        float y = scrollRect.verticalNormalizedPosition;
        float progress = targetAtBottom ? 1f - y : y;

        bool atEnd = progress >= completeThreshold;

        if (reported && progress < completeThreshold - 0.2f) { reported = false; return; }
        if (reported) return;

        bool settled = !dragging &&
                       (atEnd || scrollRect.velocity.sqrMagnitude < settledSpeed * settledSpeed);

        if (atEnd && settled)
        {
            reported = true;
            if (interactable != null) interactable.ReportScroll();
        }
    }

    private void UpdateItemLock()
    {
        if (contentGroup == null || interactable == null) return;

        var m = TutorialManager.Instance;
        bool scrollStepActive = m != null && m.IsActive &&
                                m.IsExpected(interactable.ResolvedId, TutorialActionType.Scroll);

        // Items can't be tapped while the player is supposed to be scrolling
        contentGroup.blocksRaycasts = !scrollStepActive;
    }
}