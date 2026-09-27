using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Put this on TableZone (the object tagged "TableZoneTag") alongside an
/// Image with Raycast Target ON. Accepts any dragged item (TestDrag) and
/// checks whether its ingredient is something the current recipe still
/// needs - if so, marks it collected in the ingredients list and (optionally)
/// snaps it into place; otherwise the item just bounces back like a wrong
/// drop anywhere else.
///
/// Unlike TestDrop (which is hardcoded to accept ONE specific ingredient),
/// this accepts WHATEVER the active recipe currently needs - since the
/// table has to receive many different ingredients over the course of prep.
/// </summary>
public class TableZoneIngredientReceiver : MonoBehaviour, IDropHandler
{
    [Tooltip("The RecipeIngredientListUI tracking the current recipe's checklist.")]
    public RecipeIngredientListUI ingredientList;

    [Tooltip("The recipe currently being cooked. Set this from wherever you assign/select the recipe (e.g. right after RecipeIngredientListUI.DisplayRecipe()).")]
    public RecipeData currentRecipe;

    [Tooltip("Optional: if true, a correctly-placed item is snapped into this zone. If false, it's simply destroyed/consumed once collected (useful if you don't want prepped ingredients visually cluttering the table).")]
    public bool snapAcceptedItemIntoZone = true;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        TestDrag drag = droppedObj.GetComponent<TestDrag>();
        TutorialInteractable interactable = droppedObj.GetComponent<TutorialInteractable>();
        if (drag == null) return;

        IngredientData droppedIngredient = interactable != null ? interactable.sourceData as IngredientData : null;
        if (droppedIngredient == null)
        {
            Debug.LogWarning($"[TableZoneIngredientReceiver] Dropped object '{droppedObj.name}' has no IngredientData (sourceData) to identify it - can't validate against the recipe.");
            return;
        }

        if (currentRecipe == null || !RecipeNeeds(droppedIngredient))
        {
            Debug.Log($"[TableZoneIngredientReceiver] '{droppedIngredient.displayName}' is not needed right now - rejecting drop.");
            return; // TestDrag.OnEndDrag will snap it back since we never call SnapTo
        }

        Debug.Log($"[TableZoneIngredientReceiver] '{droppedIngredient.displayName}' accepted and marked collected.");
        ingredientList?.MarkCollected(droppedIngredient);

        // This item permanently leaves its table slot now - free it so a new ingredient can spawn there.
        droppedObj.GetComponent<TableSlotOccupant>()?.FreeMySlot();

        if (snapAcceptedItemIntoZone)
        {
            drag.SnapTo(transform as RectTransform);
        }
        else
        {
            drag.SnapTo(transform as RectTransform); // still needs to register as "successfully dropped" so it doesn't bounce back
            Destroy(droppedObj);
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