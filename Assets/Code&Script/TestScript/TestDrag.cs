using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to any draggable UI item (e.g. ItemDrag). Handles the actual
/// dragging motion. Reporting to the tutorial system happens in TestDrop,
/// once a valid drop is confirmed - this script only moves things around.
/// </summary>
public class TestDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Identifier for this item, checked by TestDrop to see if it's the correct item for that zone.")]
    public string itemId;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector2 originalAnchoredPosition;
    private Transform originalParent;
    private bool wasDroppedSuccessfully;


    private void Awake()
    {
        rectTransform = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        
        wasDroppedSuccessfully = false;
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        // Reparent to the canvas root so it renders above everything while dragging.
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        // Let raycasts pass through this item while dragging, so OnDrop on
        // the zone underneath can actually detect the pointer/drop.
        canvasGroup.blocksRaycasts = false;
        Debug.Log("BeginDrag");
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        Debug.Log("Dragging");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // If TestDrop.OnDrop() never called SnapTo() (wrong zone, or dropped
        // on nothing), send it back to where it started.
        if (!wasDroppedSuccessfully)
            ReturnToOrigin();


        Debug.Log("EndDrag");
    }

    public void ReturnToOrigin()
    {
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalAnchoredPosition;
    }

    /// <summary>Called by TestDrop when this item is dropped on the correct zone.</summary>
    public void SnapTo(RectTransform target)
    {
        wasDroppedSuccessfully = true;
        transform.SetParent(target, true);
        rectTransform.anchoredPosition = Vector2.zero;
    }
}