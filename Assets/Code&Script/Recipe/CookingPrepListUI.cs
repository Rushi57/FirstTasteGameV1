using UnityEngine;
using UnityEngine.UI;

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

    [Tooltip("The ScrollRect for this list (usually on CookingPrepScrollingObj itself). If assigned, the view is reset to the top every time DisplaySteps runs.")]
    public ScrollRect scrollRect;

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

        ResetScrollToTop();
    }

    /// <summary>
    /// Forces the ScrollRect back to the top. Needed because populating a
    /// ScrollView while its panel is inactive (or right as it becomes active)
    /// doesn't recompute layout/scroll position on its own - without this,
    /// the view can appear scrolled to the middle/bottom on first show.
    /// </summary>
    private void ResetScrollToTop()
    {
        if (scrollRect == null) return;

        // Layout (Grid/Vertical Layout Group + ContentSizeFitter) needs to
        // run first so Content's height is correct before we set position,
        // otherwise this can be overridden a frame later.
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    private void Clear()
    {
        if (contentContainer == null) return;
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
            Destroy(contentContainer.GetChild(i).gameObject);
    }
}