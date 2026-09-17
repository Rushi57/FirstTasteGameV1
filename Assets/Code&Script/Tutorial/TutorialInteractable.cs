using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drop this on any button or drag-target you want the tutorial to be able to
/// reference. Call ReportTap() / ReportDrop() from your existing button OnClick
/// or drag-and-drop scripts (see examples at the bottom of this file).
///
/// The TutorialManager listens to these events and checks the "id" against the
/// current step's targetId — so your gameplay code doesn't need to know
/// anything about the tutorial at all.
/// </summary>
public class TutorialInteractable : MonoBehaviour
{
    [Tooltip("Must match TutorialStep.targetId for the step that references this object.")]
    public string interactableId;

    public RectTransform RectTransform => transform as RectTransform;

    /// <summary>Call this when the object is tapped/clicked.</summary>
    public void ReportTap()
    {
        TutorialManager.Instance?.NotifyAction(interactableId, TutorialActionType.Tap);
    }

    /// <summary>Call this when the object is successfully dropped on its target.</summary>
    public void ReportDrop()
    {
        TutorialManager.Instance?.NotifyAction(interactableId, TutorialActionType.Drag);
    }
}

/*
========================== USAGE EXAMPLES ==========================

1) On a UI Button (tap example):
   Add TutorialInteractable to the button, set interactableId = "StartButton".
   In the Button's OnClick() list (Inspector), drag the same GameObject and
   pick TutorialInteractable -> ReportTap.

   Or from code, after your normal click logic runs:

   public class MyButton : MonoBehaviour, IPointerClickHandler
   {
       private TutorialInteractable tutorialTag;
       void Awake() => tutorialTag = GetComponent<TutorialInteractable>();

       public void OnPointerClick(PointerEventData eventData)
       {
           // ... your existing click logic ...
           tutorialTag?.ReportTap();
       }
   }

2) On a drag-and-drop item, call ReportDrop() once it lands on the CORRECT
   target (i.e. where your game logic says the drop was valid):

   public class DraggableItem : MonoBehaviour, IEndDragHandler
   {
       private TutorialInteractable tutorialTag;
       void Awake() => tutorialTag = GetComponent<TutorialInteractable>();

       public void OnEndDrag(PointerEventData eventData)
       {
           bool droppedCorrectly = CheckDropTarget(eventData);
           // ... your existing drop logic ...
           if (droppedCorrectly)
               tutorialTag?.ReportDrop();
       }
   }
=======================================================================
*/