using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to a drop zone (e.g. DropZone1, DropZone2). Only reports to the
/// tutorial system (via TutorialInteractable.ReportDrop) when the dragged
/// item's itemId matches acceptedItemId - so dropping the wrong ingredient
/// in the wrong zone does NOT advance the tutorial.
/// </summary>
[RequireComponent(typeof(TutorialInteractable))]
public class TestDrop : MonoBehaviour, IDropHandler
{
    [Tooltip("OPTIONAL: assign the IngredientData this zone should accept. If set, its id is used automatically instead of typing Accepted Item Id by hand - so it can never drift out of sync with the ingredient's actual id.")]
    public ScriptableObject acceptedData;

    [Tooltip("Used only if Accepted Data is not assigned. Must match the dragged item's Item Id (on TestDrag) for this drop to count as correct.")]
    public string acceptedItemId;

    /// <summary>The id actually used for matching - from acceptedData if assigned, otherwise the manual acceptedItemId field.</summary>
    public string ResolvedAcceptedId => (acceptedData is ITutorialIdentifiable identifiable) ? identifiable.TutorialId : acceptedItemId;

    private TutorialInteractable tutorialTag;

    private void Awake()
    {
        tutorialTag = GetComponent<TutorialInteractable>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("[TestDrop] OnDrop called on " + gameObject.name);

        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null)
        {
            Debug.Log("[TestDrop] eventData.pointerDrag was null - nothing was being dragged according to the event system.");
            return;
        }
        Debug.Log("[TestDrop] Dropped object: " + droppedObj.name);

        TestDrag drag = droppedObj.GetComponent<TestDrag>();
        if (drag == null)
        {
            Debug.Log("[TestDrop] " + droppedObj.name + " has no TestDrag component - can't check its itemId.");
            return;
        }

        Debug.Log("[TestDrop] Dragged item's Item Id = '" + drag.itemId + "', this zone's Accepted Item Id = '" + ResolvedAcceptedId + "'");

        if (!string.IsNullOrEmpty(ResolvedAcceptedId) && drag.itemId != ResolvedAcceptedId)
        {
            Debug.Log("[TestDrop] Item Id mismatch - rejecting drop, item will snap back.");
            // Wrong item for this zone - do nothing, TestDrag.OnEndDrag will
            // see wasDroppedSuccessfully == false and snap it back on its own.
            return;
        }

        Debug.Log("[TestDrop] Accepted! Snapping item into zone and reporting to tutorial.");
        drag.SnapTo(transform as RectTransform);
        tutorialTag?.ReportDrop();
    }
}