using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Put this on EACH ItemContainerDropZone slot (alongside an Image with
/// Raycast Target ON). Just forwards its own drop event to the shared
/// TableItemSlotManager, which does the actual recipe validation/snapping -
/// this keeps every slot's script identical and dumb, with all the real
/// logic centralized in one place.
/// </summary>
public class TableSlotDropHandler : MonoBehaviour, IDropHandler
{
    [Tooltip("The shared manager for all table slots.")]
    public TableItemSlotManager slotManager;

    public void OnDrop(PointerEventData eventData)
    {
        slotManager?.HandleDrop(transform as RectTransform, eventData.pointerDrag);
        Debug.Log("The item is dropped in here");
    }
}