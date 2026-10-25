using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class CabinetScrollTutorialReporter : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private TutorialInteractable interactable;
    [SerializeField, Range(0f, 1f)] private float completeThreshold = 0.9f;
    [SerializeField] private bool utensilsAtEnd = true;
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
        float x = scrollRect.horizontalNormalizedPosition;
        float progress = utensilsAtEnd ? x : 1f - x;

        if (reported && progress < completeThreshold - 0.2f) { reported = false; return; }
        if (reported) return;

        bool settled = !dragging && scrollRect.velocity.sqrMagnitude < settledSpeed * settledSpeed;
        if (progress >= completeThreshold && settled)
        {
            reported = true;
            interactable.ReportScroll();
        }
    }
}