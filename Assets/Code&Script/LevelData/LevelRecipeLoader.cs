using UnityEngine;
using TMPro;

public class LevelRecipeLoader : MonoBehaviour
{
    public RecipeIngredientListUI ingredientList;
    public TableItemSlotManager tableSlotManager;
    public CookingPrepPanelController cookingPrepPanel;

    [Header("Stove")]
    [Tooltip("Drag StoveDropZone here (the object with StoveHeatController).")]
    public StoveHeatController stoveController;

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

        if (tableSlotManager != null)
            tableSlotManager.currentRecipe = selected.recipe;

        if (cookingPrepPanel != null)
            cookingPrepPanel.currentRecipe = selected.recipe;

        // Fresh progress for this level, then give the stove its recipe
        CookingPrepListUI.Instance?.ResetProgress();

        if (stoveController != null)
            stoveController.SetRecipe(selected.recipe);
        else
            Debug.LogWarning("[LevelRecipeLoader] stoveController is not assigned - stove steps won't complete.");

        if (dishNameLabel != null)
            dishNameLabel.text = selected.levelName;
    }
}