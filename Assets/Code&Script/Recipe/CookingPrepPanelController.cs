using UnityEngine;

/// <summary>
/// Drives the three prep stages:
///   1. Ingredient Prep     (IngredientPrepScrollingObj) - until every row is grayed out
///   2. Ingredient Cut Prep (IngCutPrepScrollObj)        - chop/slice/mince steps
///   3. Cooking Prep        (CookingPrepScrollingObj)    - stove, oil, pan, etc.
/// </summary>
public class CookingPrepPanelController : MonoBehaviour
{
    [Header("Source")]
    public RecipeIngredientListUI ingredientList;

    [Header("Panels to swap")]
    public GameObject ingredientPrepPanel;    // IngredientPrepScrollingObj
    public GameObject ingredientCutPrepPanel; // IngCutPrepScrollObj
    public GameObject cookingPrepPanel;       // CookingPrepScrollingObj

    [Header("List UIs (one per panel)")]
    public CookingPrepListUI cutPrepListUI;     // on IngCutPrepScrollObj
    public CookingPrepListUI cookingPrepListUI; // on CookingPrepScrollingObj

    [Tooltip("The recipe currently being cooked.")]
    public RecipeData currentRecipe;

    private void OnEnable()
    {
        if (ingredientList != null)
            ingredientList.OnAllIngredientsSpawned += HandleAllIngredientsSpawned;
        if (cutPrepListUI != null)
            cutPrepListUI.OnAllStepsComplete += HandleAllCutsDone;
    }

    private void OnDisable()
    {
        if (ingredientList != null)
            ingredientList.OnAllIngredientsSpawned -= HandleAllIngredientsSpawned;
        if (cutPrepListUI != null)
            cutPrepListUI.OnAllStepsComplete -= HandleAllCutsDone;
    }

    /// <summary>Call when a recipe starts so the player begins on Ingredient Prep.</summary>
    public void ResetToIngredientStage()
    {
        SetPanels(true, false, false);
        CookingPhaseGate.Instance?.SetPhase(CookingPhase.GatherIngredients);
    }

    // Stage 1 -> 2
    private void HandleAllIngredientsSpawned()
    {
        SetPanels(false, true, false);
        CookingPhaseGate.Instance?.SetPhase(CookingPhase.IngredientPrep);
        if (currentRecipe == null) return;

        var prepIndices = currentRecipe.GetPrepStepIndices();
        if (prepIndices.Count == 0)
        {
            HandleAllCutsDone(); // nothing to cut
            return;
        }

        cutPrepListUI?.DisplaySteps(currentRecipe, prepIndices);
    }

    // Stage 2 -> 3
    private void HandleAllCutsDone()
    {
        SetPanels(false, false, true);
        CookingPhaseGate.Instance?.SetPhase(CookingPhase.CookingPrep);
        if (currentRecipe != null)
            cookingPrepListUI?.DisplaySteps(currentRecipe, currentRecipe.GetCookingStepIndices());
    }

    private void SetPanels(bool ingredient, bool cut, bool cooking)
    {
        if (ingredientPrepPanel != null) ingredientPrepPanel.SetActive(ingredient);
        if (ingredientCutPrepPanel != null) ingredientCutPrepPanel.SetActive(cut);
        if (cookingPrepPanel != null) cookingPrepPanel.SetActive(cooking);
    }
}