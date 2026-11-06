using System.Collections.Generic;
using UnityEngine;


public class TableItemSlotManager : MonoBehaviour
{
    [Header("Slots")]
    [Tooltip("Every ItemContainerDropZone slot on the table, in whatever order you want them filled.")]
    public List<RectTransform> slots = new List<RectTransform>();

    [Header("Recipe Validation")]
    [Tooltip("The RecipeIngredientListUI tracking the current recipe's checklist.")]
    public RecipeIngredientListUI ingredientList;

    [Tooltip("The recipe currently being cooked.")]
    public RecipeData currentRecipe;

    [Header("Always accepted")]
    [Tooltip("Items that still tick the checklist.")]
    public List<IngredientData> alwaysAcceptedItems = new List<IngredientData>();

    private readonly Dictionary<RectTransform, GameObject> occupancy = new Dictionary<RectTransform, GameObject>();

   
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
    public bool HandleDrop(RectTransform targetSlot, GameObject droppedObj)
    {
        if (droppedObj == null) return false;

        TestDrag drag = droppedObj.GetComponent<TestDrag>();
        TutorialInteractable interactable = droppedObj.GetComponent<TutorialInteractable>();
        if (drag == null) return false;

        IngredientData droppedIngredient = interactable != null ? interactable.sourceData as IngredientData : null;
        if (droppedIngredient == null)
        {
            Debug.LogWarning($"[TableItemSlotManager] '{droppedObj.name}' has no IngredientData.");
            return false;
        }

        TableSlotOccupant occupantComp = droppedObj.GetComponent<TableSlotOccupant>();

        if (IsSlotTakenByOther(targetSlot, droppedObj))
        {
            Debug.Log($"[TableItemSlotManager] Slot '{targetSlot.name}' is already occupied - rejecting drop.");
            return false;
        }

        bool needed = TryCollect(droppedIngredient);
        if (!needed && !IsAlwaysAccepted(droppedIngredient))
            return false;

        if (occupantComp != null && occupantComp.slot != null && occupantComp.slot != targetSlot)
            FreeSlot(occupantComp.slot);

        drag.SnapTo(targetSlot);
        OccupySlot(targetSlot, droppedObj);

        if (occupantComp != null)
        {
            occupantComp.slotManager = this;
            occupantComp.slot = targetSlot;
        }
        return true;
    }

    private bool IsAlwaysAccepted(IngredientData ingredient)
    {
        return alwaysAcceptedItems != null && alwaysAcceptedItems.Contains(ingredient);
    }

    public bool TryCollect(IngredientData ingredient)
    {
        if (!RecipeNeeds(ingredient)) return false;
        ingredientList?.MarkCollected(ingredient);
        return true;
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
    private bool IsSlotTakenByOther(RectTransform slot, GameObject dropped)
    {
        if (!occupancy.TryGetValue(slot, out var existing) || existing == null) return false;
        if (existing == dropped) return false;

        
        if (!existing.activeInHierarchy || existing.transform.parent != slot)
        {
            occupancy.Remove(slot);
            return false;
        }
        return true;
    }
}