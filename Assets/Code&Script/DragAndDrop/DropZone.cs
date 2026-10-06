using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [System.Serializable]
    public class DropEvent : UnityEvent<GameObject> { }

    [Header("Drop Settings")]
    [Tooltip("If true, snaps the dropped object to the center of this zone.")]
    [SerializeField] private bool snapToCenter = true;

    [Tooltip("If true, allows only one item in this slot at a time.")]
    [SerializeField] private bool singleItemOnly = true;

    [Tooltip("If set, limits dropping to objects with this specific tag. Leave empty to accept all.")]
    [SerializeField] private string requiredTag = "";

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private CanvasGroup highlightOverlay;
    [SerializeField] private float hoverAlpha = 0.5f;

    [Header("Events")]
    [Tooltip("Fires when a valid object is dropped here. Passes the dropped GameObject.")]
    public DropEvent OnItemDropped;

    public void OnDrop(PointerEventData eventData)
    {
        // 1. Validate that an object was actually dragged
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        // 2. Validate Tag (if configured)
        if (!string.IsNullOrEmpty(requiredTag) && !draggedObject.CompareTag(requiredTag))
        {
            Debug.LogWarning($"[DropZone] {draggedObject.name} rejected: Missing tag '{requiredTag}'.");
            return;
        }

        // 3. Validate Space (if singleItemOnly is true)
        if (singleItemOnly && transform.childCount > 0)
        {
            Debug.LogWarning($"[DropZone] {gameObject.name} already occupied!");
            return;
        }

        // 4. Handle Parent Re-assignment & Snapping
        draggedObject.transform.SetParent(transform);

        if (snapToCenter)
        {
            RectTransform draggedRect = draggedObject.GetComponent<RectTransform>();
            if (draggedRect != null)
            {
                draggedRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                draggedObject.transform.localPosition = Vector3.zero;
            }
        }

        // 5. Reset Highlight Visuals
        SetHighlight(false);

        // 6. Log & Trigger Custom Events
        Debug.Log($"{draggedObject.name} dropped onto {gameObject.name}");
        OnItemDropped?.Invoke(draggedObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show hover feedback if a valid item is currently being dragged
        if (eventData.pointerDrag != null)
        {
            if (string.IsNullOrEmpty(requiredTag) || eventData.pointerDrag.CompareTag(requiredTag))
            {
                SetHighlight(true);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(false);
    }

    private void SetHighlight(bool active)
    {
        if (highlightOverlay != null)
        {
            highlightOverlay.alpha = active ? hoverAlpha : 0f;
        }
    }
}