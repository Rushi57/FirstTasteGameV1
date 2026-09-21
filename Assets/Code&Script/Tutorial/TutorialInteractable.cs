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
    [Tooltip("Used if Source Data is not assigned. Must match TutorialStep.targetId for the step that references this object.")]
    public string interactableId;

    [Tooltip("OPTIONAL: assign the IngredientData/UtensilData (or any ScriptableObject implementing ITutorialIdentifiable) this object represents. If set, the id is pulled from there automatically instead of typed by hand here, so it can never drift out of sync with your TutorialStep assets.")]
    public ScriptableObject sourceData;

    /// <summary>The id actually used for matching - from sourceData if assigned, otherwise the manual interactableId field. Trimmed so a stray leading/trailing space typed in the Inspector can't silently break matching.</summary>
    public string ResolvedId => ((sourceData is ITutorialIdentifiable identifiable) ? identifiable.TutorialId : interactableId)?.Trim();

    public RectTransform RectTransform => transform as RectTransform;

    private void OnEnable()
    {
        // Self-register regardless of when/how this object was created -
        // handles both scene-placed objects AND ones spawned at runtime
        // (e.g. from an ingredient/utensil prefab pool).
        TutorialManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        TutorialManager.Instance?.Unregister(this);
    }

    /// <summary>Call this when the object is tapped/clicked.</summary>
    public void ReportTap()
    {
        TutorialManager.Instance?.NotifyAction(ResolvedId, TutorialActionType.Tap);
    }

    /// <summary>Call this when the object is successfully dropped on its target.</summary>
    public void ReportDrop()
    {
        TutorialManager.Instance?.NotifyAction(ResolvedId, TutorialActionType.Drag);
    }
}

/// <summary>
/// Implement this on any ScriptableObject (IngredientData, UtensilData, etc.)
/// that should be referenceable by the tutorial system as a single source
/// of truth for its id - instead of retyping the same string in multiple places.
/// </summary>
public interface ITutorialIdentifiable
{
    string TutorialId { get; }
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