using System.Collections;
using UnityEngine;

public class PanelCloseOnStepComplete : MonoBehaviour
{
    [Tooltip("The Cooking Prep list (same one assigned as 'Cooking Prep List UI' in CookingPrepPanelController).")]
    [SerializeField] private CookingPrepListUI cookingPrepList;

    [Tooltip("TopUtensils.")]
    [SerializeField] private GameObject panelToClose;

    [Tooltip("Ingredient names of the seasonings on this shelf, e.g. Salt, Pepper, Sugar, Vinegar, Soy, Cooking Oil.")]
    [SerializeField] private string[] shelfIngredientIds;

    [SerializeField] private float closeDelay = 0.15f;

    private void OnEnable()
    {
        CookingPrepListUI.StepCompleted += HandleStepCompleted;
        CookingPrepListUI.IngredientAccepted += HandleIngredientAccepted;
    }
    private void OnDisable()
    {
        CookingPrepListUI.StepCompleted -= HandleStepCompleted;
        CookingPrepListUI.IngredientAccepted -= HandleIngredientAccepted;
    }

    private void HandleStepCompleted(string stepId)
    {
        if (cookingPrepList == null || panelToClose == null) return;
        if (!panelToClose.activeInHierarchy) return;

  
        if (cookingPrepList.LastCompletedStepId != stepId) return;

        string text = cookingPrepList.LastCompletedText;
        if (string.IsNullOrEmpty(text)) return;

        string normText = Normalize(text);
        foreach (string id in shelfIngredientIds)
        {
            if (!string.IsNullOrEmpty(id) && normText.Contains(Normalize(id)))
            {
                StartCoroutine(CloseRoutine());
                return;
            }
        }
    }

    private void HandleIngredientAccepted(CookingPrepListUI list)
    {
        if (list != cookingPrepList || panelToClose == null) return;
        if (!panelToClose.activeInHierarchy) return;

        string text = list.CurrentStepText;   
        if (string.IsNullOrEmpty(text)) return;

        string normText = Normalize(text);
        foreach (string id in shelfIngredientIds)
        {
            if (!string.IsNullOrEmpty(id) && normText.Contains(Normalize(id)))
            {
                StartCoroutine(CloseRoutine());
                return;
            }
        }
    }

    private IEnumerator CloseRoutine()
    {
        if (closeDelay > 0f) yield return new WaitForSecondsRealtime(closeDelay);
        if (panelToClose.activeInHierarchy)
            PanelExclusivityManager.Instance.Close(panelToClose);
    }

    private static string Normalize(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char c in s)
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }
}