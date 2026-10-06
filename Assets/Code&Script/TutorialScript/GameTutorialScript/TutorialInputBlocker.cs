using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Put this on a full-screen Image (Raycast Target = ON, transparent color is fine)
/// at the TOP of your tutorial canvas hierarchy (highest sibling index / sort order
/// among interactive UI). It intercepts every pointer event in the scene and only
/// forwards events to the real UI underneath if they originate on the currently
/// allowed target. Everything else is silently swallowed.
/// </summary>
[RequireComponent(typeof(Image))]
public class TutorialInputBlocker : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private GraphicRaycaster raycaster; // the root Canvas's GraphicRaycaster

    private RectTransform allowedTarget;
    private bool dragOriginatedOnAllowedTarget;

    public void SetAllowedTarget(RectTransform target) => allowedTarget = target;

    bool IsOverAllowedTarget(PointerEventData e) =>
        allowedTarget != null &&
        RectTransformUtility.RectangleContainsScreenPoint(allowedTarget, e.position, e.pressEventCamera);

    void ForwardToUnderlying<T>(PointerEventData e, ExecuteEvents.EventFunction<T> handler)
        where T : IEventSystemHandler
    {
        var results = new List<RaycastResult>();
        raycaster.Raycast(e, results);

        foreach (var result in results)
        {
            if (result.gameObject == gameObject) continue; // skip the blocker itself

            GameObject handlerObj = ExecuteEvents.GetEventHandler<T>(result.gameObject);
            if (handlerObj != null)
            {
                ExecuteEvents.Execute(handlerObj, e, handler);
                return;
            }
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (IsOverAllowedTarget(e))
            ForwardToUnderlying(e, ExecuteEvents.pointerDownHandler);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (IsOverAllowedTarget(e) || dragOriginatedOnAllowedTarget)
            ForwardToUnderlying(e, ExecuteEvents.pointerUpHandler);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (IsOverAllowedTarget(e))
            ForwardToUnderlying(e, ExecuteEvents.pointerClickHandler);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        // Latch on begin so the drag keeps working even after the pointer
        // travels outside the target's original rect (e.g. dragging the onion
        // across the screen toward the chopping board).
        dragOriginatedOnAllowedTarget = IsOverAllowedTarget(e);
        if (dragOriginatedOnAllowedTarget)
            ForwardToUnderlying(e, ExecuteEvents.beginDragHandler);
    }

    public void OnDrag(PointerEventData e)
    {
        if (dragOriginatedOnAllowedTarget)
            ForwardToUnderlying(e, ExecuteEvents.dragHandler);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (dragOriginatedOnAllowedTarget)
            ForwardToUnderlying(e, ExecuteEvents.endDragHandler);
        dragOriginatedOnAllowedTarget = false;
    }
}