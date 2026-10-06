using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Generic drag-and-drop component for ANY tool or ingredient (onion, knife, pan
/// lid, spatula, water bottle, etc). Dropping it on a DropZone snaps it into place;
/// the DropZone itself decides whether that drop should trigger a mechanic panel
/// (see MechanicRule on DropZone). This replaces the old DraggableIngredient script -
/// use this one for every draggable item going forward.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Must match this item's TutorialStep.targetId if used in a tutorial step (e.g. 'Onion', 'Knife', 'PanLid', 'Spatula', 'WaterBottle').")]
    [SerializeField] private string itemId;

    [SerializeField] private Canvas canvas;
    [SerializeField] private DropZone validDropZone;

    private RectTransform rt;
    private CanvasGroup canvasGroup;
    private Vector2 originalAnchoredPosition;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        originalAnchoredPosition = rt.anchoredPosition;
        canvasGroup.blocksRaycasts = false; // let raycasts reach the drop zone underneath
    }

    public void OnDrag(PointerEventData e)
    {
        rt.anchoredPosition += e.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData e)
    {
        canvasGroup.blocksRaycasts = true;

        bool droppedOnZone = validDropZone != null &&
            RectTransformUtility.RectangleContainsScreenPoint(validDropZone.RectTransform, e.position, e.pressEventCamera);

        if (droppedOnZone)
        {
            rt.position = validDropZone.RectTransform.position; // snap into place
            validDropZone.NotifyItemDropped(itemId);

            // Report directly too, in case this exact drop (not a downstream mechanic
            // it triggers) is itself the tutorial step being waited on - e.g.
            // "drag the onion onto the board" is its own step before "drag the knife" is.
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.ReportActionCompleted(itemId);
        }
        else
        {
            rt.anchoredPosition = originalAnchoredPosition; // snap back, missed drop
        }
    }
}