using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    public System.Action OnAllIngredientsCollected;

  
    public System.Action OnAllIngredientsSpawned;

    private ScrollRect scrollRect;

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
        if (isActiveAndEnabled)
            StartCoroutine(ScrollToTop());
    }

    private IEnumerator ScrollToTop()
    {
     
        yield return new WaitForEndOfFrame();

        if (contentContainer == null) yield break;

        if (scrollRect == null)
            scrollRect = contentContainer.GetComponentInParent<ScrollRect>();
        if (scrollRect == null) yield break;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f; // 1 = top
    }


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

    public bool IsInRecipe(IngredientData ingredient)
    {
        if(ingredient == null || currentRecipe == null) return false;

        foreach(var row in activeRows)
        {
            if(row.Matches(ingredient))
            { 
                return true; 
            }

        }
        return false;
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