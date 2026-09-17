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
    [Tooltip("Must match the dragged item's Item Id (on TestDrag) for this drop to count as correct.")]
    public string acceptedItemId;

    private TutorialInteractable tutorialTag;

    private void Awake()
    {
        tutorialTag = GetComponent<TutorialInteractable>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        TestDrag drag = droppedObj.GetComponent<TestDrag>();
        if (drag == null) return;

        if (!string.IsNullOrEmpty(acceptedItemId) && drag.itemId != acceptedItemId)
        {
            // Wrong item for this zone - do nothing, TestDrag.OnEndDrag will
            // see wasDroppedSuccessfully == false and snap it back on its own.
            return;
        }

        drag.SnapTo(transform as RectTransform);
        tutorialTag?.ReportDrop();
    }
}