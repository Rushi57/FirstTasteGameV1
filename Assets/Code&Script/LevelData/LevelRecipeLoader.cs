using UnityEngine;
using TMPro;

/// <summary>
/// Put this anywhere in LevelMapScene (e.g. on the same object as your
/// RecipeIngredientListUI, or a dedicated manager object). On Start(), reads
/// LevelSelectionManager.SelectedLevel and wires its recipe into the
/// ingredients list and TableZone automatically - so the player sees the
/// right dish's ingredients the moment the level loads.
/// </summary>
public class LevelRecipeLoader : MonoBehaviour
{
    public RecipeIngredientListUI ingredientList;
    public TableZoneIngredientReceiver tableZoneReceiver;

    [Tooltip("Optional: shows the dish's name, e.g. on a 'Now Cooking: Adobo' label.")]
    public TMP_Text dishNameLabel;

    void Start()
    {
        LevelData selected = LevelSelectionManager.SelectedLevel;

        if (selected == null)
        {
            Debug.LogWarning("[LevelRecipeLoader] No level was selected before entering this scene - LevelSelectionManager.SelectedLevel is null. Falling back to whatever is already assigned in the Inspector, if anything.");
            return;
        }

        if (selected.recipe == null)
        {
            Debug.LogWarning($"[LevelRecipeLoader] Level '{selected.levelName}' has no recipe assigned.");
            return;
        }

        Debug.Log($"[LevelRecipeLoader] Loading recipe for Level {selected.levelNumber}: {selected.levelName}");

        ingredientList?.DisplayRecipe(selected.recipe);

        if (tableZoneReceiver != null)
            tableZoneReceiver.currentRecipe = selected.recipe;

        if (dishNameLabel != null)
            dishNameLabel.text = selected.levelName;
    }
}