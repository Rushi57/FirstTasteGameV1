using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Populates the scrolling "Ingredients :" list (your DishInformation panel)
/// from a RecipeData, and tracks which ones have been spawned and/or placed
/// on the table so far. Put this on the Content object of your ScrollView,
/// or anywhere convenient, and assign contentContainer + ingredientRowPrefab.
/// </summary>
public class RecipeIngredientListUI : MonoBehaviour
{
    [Tooltip("The ScrollView's Content transform - rows get spawned as children of this.")]
    public Transform contentContainer;

    [Tooltip("Prefab with a RecipeIngredientRowUI component.")]
    public GameObject ingredientRowPrefab;

    [Tooltip("Optional: shows the current recipe's icon (e.g. next to 'Title Dish').")]
    public Image recipeImageDisplay;

    private RecipeData currentRecipe;
    private readonly List<RecipeIngredientRowUI> activeRows = new List<RecipeIngredientRowUI>();
    private readonly HashSet<IngredientData> collectedIds = new HashSet<IngredientData>();
    private readonly HashSet<IngredientData> spawnedIds = new HashSet<IngredientData>();
    private bool hasFiredAllCollected = false;

    /// <summary>Fires exactly once, the moment every ingredient in the current recipe has been collected.</summary>
    public System.Action OnAllIngredientsCollected;

    /// <summary>Call this when the player selects/opens a recipe ("SELECT A RECIPE AND LET'S COOK!").</summary>
    public void DisplayRecipe(RecipeData recipe)
    {
        currentRecipe = recipe;
        Clear();

        if (recipeImageDisplay != null)
        {
            Sprite sprite = recipe != null ? recipe.recipeIcon : null;
            recipeImageDisplay.sprite = sprite;
            recipeImageDisplay.enabled = sprite != null;
        }

        if (recipe == null || contentContainer == null || ingredientRowPrefab == null) return;

        foreach (var entry in recipe.ingredients)
        {
            GameObject rowGO = Instantiate(ingredientRowPrefab, contentContainer);
            RecipeIngredientRowUI row = rowGO.GetComponent<RecipeIngredientRowUI>();
            if (row == null)
            {
                Debug.LogWarning("[RecipeIngredientListUI] ingredientRowPrefab is missing a RecipeIngredientRowUI component.");
                continue;
            }
            row.SetData(entry);
            activeRows.Add(row);
        }
    }

    /// <summary>
    /// Grays out the row for this ingredient to show it's been spawned/pulled
    /// out already. Call this right after IngredientSpawner spawns it - NOT
    /// the same as MarkCollected, which is for "correctly placed on the table".
    /// </summary>
    public void MarkSpawned(IngredientData ingredient)
    {
        if (ingredient == null) return;
        spawnedIds.Add(ingredient);

        foreach (var row in activeRows)
        {
            if (row.Matches(ingredient))
            {
                row.SetSpawned(true);
                return;
            }
        }
    }

    /// <summary>Marks the row for this ingredient as collected (e.g. strikethrough/checkmark). Call this once it's correctly placed on the table.</summary>
    public void MarkCollected(IngredientData ingredient)
    {
        if (ingredient == null) return;
        collectedIds.Add(ingredient);

        foreach (var row in activeRows)
        {
            if (row.Matches(ingredient))
            {
                row.SetCollected(true);
                break;
            }
        }

        if (!hasFiredAllCollected && AllCollected())
        {
            hasFiredAllCollected = true;
            OnAllIngredientsCollected?.Invoke();
        }
    }

    /// <summary>True once every ingredient in the current recipe has been marked collected.</summary>
    public bool AllCollected()
    {
        return currentRecipe != null && collectedIds.Count >= currentRecipe.ingredients.Count;
    }

    private void Clear()
    {
        // Destroy EVERY child, not just ones we tracked ourselves - protects
        // against a leftover template/prefab-source object accidentally left
        // in the scene under contentContainer.
        if (contentContainer != null)
        {
            for (int i = contentContainer.childCount - 1; i >= 0; i--)
                Destroy(contentContainer.GetChild(i).gameObject);
        }

        activeRows.Clear();
        collectedIds.Clear();
        spawnedIds.Clear();
        hasFiredAllCollected = false;
    }
}