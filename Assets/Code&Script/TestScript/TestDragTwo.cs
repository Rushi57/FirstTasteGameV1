using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// DEBUG ONLY. Does nothing except log when each pointer/drag event fires.
/// No movement, no reparenting, no tutorial integration - purely to isolate
/// whether Unity's event system is calling these methods on this object.
/// Attach this to ItemDrag temporarily (alongside or instead of TestDrag).
/// </summary>
public class TestDragTwo : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[TestDragTwo] OnPointerDown");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("[TestDragTwo] OnPointerUp");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("[TestDragTwo] OnBeginDrag");
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("[TestDragTwo] OnDrag - delta: " + eventData.delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("[TestDragTwo] OnEndDrag");
    }
}