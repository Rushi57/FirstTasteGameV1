using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Populates the scrolling "Ingredients :" list from a RecipeData and tracks
/// which ingredients have been spawned (grayed out) and/or collected (placed
/// on the table).
/// </summary>
public class RecipeIngredientListUI : MonoBehaviour
{
    [Tooltip("The ScrollView's Content transform - rows get spawned as children of this.")]
    public Transform contentContainer;

    [Tooltip("Prefab with a RecipeIngredientRowUI component.")]
    public GameObject ingredientRowPrefab;

    [Tooltip("Optional: shows the current recipe's icon.")]
    public Image recipeImageDisplay;

    private RecipeData currentRecipe;
    private readonly List<RecipeIngredientRowUI> activeRows = new List<RecipeIngredientRowUI>();
    private readonly HashSet<IngredientData> collectedIds = new HashSet<IngredientData>();
    private readonly HashSet<IngredientData> spawnedIds = new HashSet<IngredientData>();
    private bool hasFiredAllCollected = false;
    private bool hasFiredAllSpawned = false;

    /// <summary>Fires once, when every ingredient has been placed on the table.</summary>
    public System.Action OnAllIngredientsCollected;

    /// <summary>Fires once, when every ingredient row has been grayed out (spawned).</summary>
    public System.Action OnAllIngredientsSpawned;

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

    /// <summary>Grays out the row for this ingredient. Call right after IngredientSpawner spawns it.</summary>
    public void MarkSpawned(IngredientData ingredient)
    {
        if (ingredient == null) return;
        spawnedIds.Add(ingredient);

        foreach (var row in activeRows)
        {
            if (row.Matches(ingredient))
            {
                row.SetSpawned(true);
                break;
            }
        }

        if (!hasFiredAllSpawned && AllSpawned())
        {
            hasFiredAllSpawned = true;
            OnAllIngredientsSpawned?.Invoke();
        }
    }

    /// <summary>Marks the row as collected. Call once it's correctly placed on the table.</summary>
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

    public bool AllSpawned()
    {
        return currentRecipe != null && spawnedIds.Count >= currentRecipe.ingredients.Count;
    }

    public bool AllCollected()
    {
        return currentRecipe != null && collectedIds.Count >= currentRecipe.ingredients.Count;
    }

    private void Clear()
    {
        if (contentContainer != null)
        {
            for (int i = contentContainer.childCount - 1; i >= 0; i--)
                Destroy(contentContainer.GetChild(i).gameObject);
        }

        activeRows.Clear();
        collectedIds.Clear();
        spawnedIds.Clear();
        hasFiredAllCollected = false;
        hasFiredAllSpawned = false;
    }
}