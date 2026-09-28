using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Put this on StoveDropObj. It needs an Image with Raycast Target ON
/// (alpha can be 0) so it can receive drops.
/// Accepts only the pan. Once the pan is on the stove, it completes the
/// "Put the Pan in the Stove" prep step.
/// </summary>
public class StoveDropZone : MonoBehaviour, IDropHandler
{
    [Header("Pan identification")]
    [Tooltip("The Id field on the pan's IngredientData asset, e.g. 'Pan'.")]
    public string panId = "Pan";

    [Header("Cooking Prep")]
    [Tooltip("Must match the Cooking Step Ids entry for the pan row in your RecipeData.")]
    public string panStepId = "Pan:OnStove";

    public GameObject CurrentPan { get; private set; }
    public bool HasPan => CurrentPan != null;

    public event System.Action<GameObject> OnPanPlaced;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        TestDrag drag = dropped.GetComponent<TestDrag>();
        if (drag == null) return;

        // Utensils have no TutorialInteractable, so identify by TestDrag.itemId
        string droppedId = drag.itemId;

        if (droppedId != panId)
        {
            Debug.Log($"[StoveDrop] '{droppedId}' dropped on stove - only the pan ('{panId}') is accepted. Bouncing back.");
            return; // never call SnapTo, so it returns to its origin
        }

        if (HasPan)
        {
            Debug.Log("[StoveDrop] The pan is already on the stove.");
            return;
        }

        drag.SnapTo(transform as RectTransform);
        CurrentPan = dropped;

        Debug.Log($"[StoveDrop] Pan placed on stove: {dropped.name}");

        OnPanPlaced?.Invoke(dropped);
        CookingPrepListUI.Instance?.CompleteStep(panStepId);
    }

    /// <summary>Call on Retry / new dish.</summary>
    public void ResetStove()
    {
        CurrentPan = null;
    }
}