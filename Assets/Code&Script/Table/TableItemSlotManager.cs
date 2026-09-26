using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks which of the table's ItemContainerDropZone slots are currently
/// occupied, AND validates/accepts ingredients dropped into any of them
/// against the current recipe - absorbing what TableZoneIngredientReceiver
/// used to do, now centralized here since the slots themselves are the
/// drop targets. Each slot gets a thin TableSlotDropHandler that forwards
/// its drop event here.
/// </summary>
public class TableItemSlotManager : MonoBehaviour
{
    [Header("Slots")]
    [Tooltip("Every ItemContainerDropZone slot on the table, in whatever order you want them filled.")]
    public List<RectTransform> slots = new List<RectTransform>();

    [Header("Recipe Validation")]
    [Tooltip("The RecipeIngredientListUI tracking the current recipe's checklist.")]
    public RecipeIngredientListUI ingredientList;

    [Tooltip("The recipe currently being cooked. Set this from wherever you assign/select the recipe (e.g. right after RecipeIngredientListUI.DisplayRecipe()).")]
    public RecipeData currentRecipe;

    private readonly Dictionary<RectTransform, GameObject> occupancy = new Dictionary<RectTransform, GameObject>();

    /// <summary>Returns the first empty slot, or null if the table is completely full.</summary>
    public RectTransform GetEmptySlot()
    {
        foreach (var slot in slots)
        {
            if (slot == null) continue;
            if (!occupancy.TryGetValue(slot, out var occupant) || occupant == null)
                return slot;
        }
        return null;
    }

    public void OccupySlot(RectTransform slot, GameObject item)
    {
        if (slot != null) occupancy[slot] = item;
    }

    public void FreeSlot(RectTransform slot)
    {
        if (slot != null) occupancy.Remove(slot);
    }

    public bool IsFull()
    {
        return GetEmptySlot() == null;
    }

    /// <summary>
    /// Called by TableSlotDropHandler when something is dropped onto one of
    /// the slots. Validates against the recipe and either accepts (snaps
    /// into place, marks collected, updates occupancy) or does nothing - if
    /// nothing is done, TestDrag.OnEndDrag handles snapping the item back to
    /// where it came from automatically (no extra code needed here for that).
    /// </summary>
    public void HandleDrop(RectTransform targetSlot, GameObject droppedObj)
    {
        if (droppedObj == null) return;

        TestDrag drag = droppedObj.GetComponent<TestDrag>();
        TutorialInteractable interactable = droppedObj.GetComponent<TutorialInteractable>();
        if (drag == null) return;

        IngredientData droppedIngredient = interactable != null ? interactable.sourceData as IngredientData : null;
        if (droppedIngredient == null)
        {
            Debug.LogWarning($"[TableItemSlotManager] Dropped object '{droppedObj.name}' has no IngredientData (sourceData) to identify it - can't validate against the recipe.");
            return;
        }

        TableSlotOccupant occupantComp = droppedObj.GetComponent<TableSlotOccupant>();

        // Reject if the target slot is already occupied by something else.
        if (occupancy.TryGetValue(targetSlot, out var existing) && existing != null && existing != droppedObj)
        {
            Debug.Log($"[TableItemSlotManager] Slot '{targetSlot.name}' is already occupied - rejecting drop.");
            return; // TestDrag.OnEndDrag snaps it back automatically
        }

        if (currentRecipe == null || !RecipeNeeds(droppedIngredient))
        {
            Debug.Log($"[TableItemSlotManager] '{droppedIngredient.displayName}' is not needed right now - rejecting drop.");
            return; // TestDrag.OnEndDrag snaps it back automatically
        }

        // Free whatever slot this item previously occupied (if any) before moving it.
        if (occupantComp != null && occupantComp.slot != null && occupantComp.slot != targetSlot)
            FreeSlot(occupantComp.slot);

        Debug.Log($"[TableItemSlotManager] '{droppedIngredient.displayName}' accepted into '{targetSlot.name}' and marked collected.");
        ingredientList?.MarkCollected(droppedIngredient);

        drag.SnapTo(targetSlot);
        OccupySlot(targetSlot, droppedObj);

        if (occupantComp != null)
        {
            occupantComp.slotManager = this;
            occupantComp.slot = targetSlot;
        }
    }

    private bool RecipeNeeds(IngredientData ingredient)
    {
        if (currentRecipe == null) return false;
        foreach (var entry in currentRecipe.ingredients)
        {
            if (entry.ingredient == ingredient)
                return true;
        }
        return false;
    }
}