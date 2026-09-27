using UnityEngine;

/// <summary>
/// Populates the Cooking Prep scroll list (CookingPrepScrollingObj) from a
/// RecipeData's plain-text cookingInstructions. Put this on that object's
/// Content, or anywhere convenient, and assign contentContainer + stepRowPrefab.
/// </summary>
public class CookingPrepListUI : MonoBehaviour
{
    [Tooltip("The ScrollView's Content transform - rows get spawned as children of this.")]
    public Transform contentContainer;

    [Tooltip("Prefab with a CookingPrepStepRowUI component (e.g. ListCookingPrepPrefab).")]
    public GameObject stepRowPrefab;

    public void DisplaySteps(RecipeData recipe)
    {
        Clear();

        if (recipe == null || contentContainer == null || stepRowPrefab == null) return;

        foreach (string line in recipe.cookingInstructions)
        {
            GameObject rowGO = Instantiate(stepRowPrefab, contentContainer);
            CookingPrepStepRowUI row = rowGO.GetComponent<CookingPrepStepRowUI>();
            if (row == null)
            {
                Debug.LogWarning("[CookingPrepListUI] stepRowPrefab is missing a CookingPrepStepRowUI component.");
                continue;
            }
            row.SetText(line);
        }
    }

    private void Clear()
    {
        if (contentContainer == null) return;
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
            Destroy(contentContainer.GetChild(i).gameObject);
    }
}