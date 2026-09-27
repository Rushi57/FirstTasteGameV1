using UnityEngine;

/// <summary>
/// Listens for RecipeIngredientListUI.OnAllIngredientsCollected and, when it
/// fires, hides IngredientPrepScrollingObj and shows CookingPrepScrollingObj
/// populated with the current recipe's instruction steps.
/// </summary>
public class CookingPrepPanelController : MonoBehaviour
{
    [Header("Source")]
    public RecipeIngredientListUI ingredientList;

    [Header("Panels to swap")]
    public GameObject ingredientPrepPanel; // IngredientPrepScrollingObj
    public GameObject cookingPrepPanel;    // CookingPrepScrollingObj

    [Header("Cooking Prep content")]
    public CookingPrepListUI cookingPrepListUI;

    [Tooltip("The recipe currently being cooked - set this alongside RecipeIngredientListUI.DisplayRecipe() and TableItemSlotManager.currentRecipe.")]
    public RecipeData currentRecipe;

    private void OnEnable()
    {
        if (ingredientList != null)
            ingredientList.OnAllIngredientsCollected += HandleAllCollected;
    }

    private void OnDisable()
    {
        if (ingredientList != null)
            ingredientList.OnAllIngredientsCollected -= HandleAllCollected;
    }

    private void HandleAllCollected()
    {
        Debug.Log("[CookingPrepPanelController] All ingredients collected - switching to Cooking Prep panel.");

        if (ingredientPrepPanel != null) ingredientPrepPanel.SetActive(false);
        if (cookingPrepPanel != null) cookingPrepPanel.SetActive(true);

        cookingPrepListUI?.DisplaySteps(currentRecipe);
    }
}