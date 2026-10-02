using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to any draggable UI item. Handles the dragging motion.
/// Also supports LockAndReturn(): send the item back to its origin and hide it
/// (used while the pour animation plays), then SetLocked(false) to bring it back.
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
    private bool isLocked;

    public bool IsLocked => isLocked;

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
        if (isLocked) return;

        wasDroppedSuccessfully = false;
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        // Reparent to the canvas root so it renders above everything while dragging.
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        // Let raycasts pass through while dragging so OnDrop on the zone underneath works.
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Stay non-blocking if the item was locked during the drop.
        canvasGroup.blocksRaycasts = !isLocked;

        if (!wasDroppedSuccessfully)
            ReturnToOrigin();
    }

    public void ReturnToOrigin()
    {
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalAnchoredPosition;
    }

    /// <summary>Called by a drop zone when this item is dropped on the correct zone.</summary>
    public void SnapTo(RectTransform target)
    {
        wasDroppedSuccessfully = true;
        transform.SetParent(target, true);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Sends the item straight back to its table slot and hides/locks it
    /// until SetLocked(false) is called (e.g. when the pour animation ends).
    /// </summary>
    public void LockAndReturn()
    {
        ReturnToOrigin();
        wasDroppedSuccessfully = true; // OnEndDrag must not move it again
        SetLocked(true);
    }

    /// <summary>Hides + blocks the item (true) or shows + enables it again (false).</summary>
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        canvasGroup.alpha = locked ? 0f : 1f;
        canvasGroup.blocksRaycasts = !locked;
        canvasGroup.interactable = !locked;
    }
}