using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CookingPrepListUI : MonoBehaviour
{
    public static CookingPrepListUI Instance { get; private set; }

    [Tooltip("The ScrollView's Content transform - rows get spawned as children of this.")]
    public Transform contentContainer;
    public GameObject stepRowPrefab;
    public ScrollRect scrollRect;

    [Header("Auto-complete chopping steps")]
    public CuttingMechanic cuttingMechanic;

    private readonly Dictionary<string, CookingPrepStepRowUI> rows = new Dictionary<string, CookingPrepStepRowUI>();
    private readonly HashSet<string> completed = new HashSet<string>();

    public event System.Action OnAllStepsComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (cuttingMechanic != null) cuttingMechanic.OnStateChanged += HandleCutStateChanged;
    }

    private void OnDisable()
    {
        if (cuttingMechanic != null) cuttingMechanic.OnStateChanged -= HandleCutStateChanged;
    }

    public void DisplaySteps(RecipeData recipe)
    {
        Debug.Log($"[PrepList] DisplaySteps called. Existing children: {contentContainer.childCount}", this);
        Clear();
        if (recipe == null || contentContainer == null || stepRowPrefab == null) return;

        for (int i = 0; i < recipe.cookingInstructions.Count; i++)
        {
            GameObject rowGO = Instantiate(stepRowPrefab, contentContainer);
            CookingPrepStepRowUI row = rowGO.GetComponent<CookingPrepStepRowUI>();
            if (row == null)
            {
                Debug.LogWarning("[CookingPrepListUI] stepRowPrefab is missing a CookingPrepStepRowUI component.");
                continue;
            }

            row.SetText(recipe.cookingInstructions[i]);

            string id = recipe.GetStepId(i);
            Debug.Log($"[PrepList] row {i} -> id = '{id}'");
            if (!string.IsNullOrEmpty(id))
            {
                rows[id] = row;
                row.SetCompleted(completed.Contains(id)); // re-apply if the list is rebuilt
            }
            else
            {
                row.SetCompleted(false);
            }
        }

        ResetScrollToTop();
    }

    private void HandleCutStateChanged(IngredientData data, IngredientPrepState state)
    {
        Debug.Log($"[PrepList] cut event received: {(data != null ? data.id : "null")}:{state}");
        if (data == null || state == IngredientPrepState.Whole) return; // ignore the reset
        CompleteStep($"{data.id}:{state}");
    }

    /// <summary>Call from ANY mechanic when it finishes.</summary>
    public void CompleteStep(string stepId)
    {
        if (!rows.TryGetValue(stepId, out var row))
        {
            Debug.LogWarning($"[PrepList] no row for '{stepId}'. Registered ids: {string.Join(", ", rows.Keys)}");
            return;
        }
        if (!completed.Add(stepId))
        {
            Debug.Log($"[PrepList] '{stepId}' was already completed - skipping.");
            return;
        }

        Debug.Log($"[PrepList] row object = {row.name}, sibling index = {row.transform.GetSiblingIndex()}");
        row.SetCompleted(true);

        if (completed.Count >= rows.Count)
            OnAllStepsComplete?.Invoke();
    }

    /// <summary>Call on Retry / new dish, then call DisplaySteps again.</summary>
    public void ResetProgress()
    {
        completed.Clear();
    }

    private void ResetScrollToTop()
    {
        if (scrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    private void Clear()
    {
        rows.Clear();
        if (contentContainer == null) return;

        for (int i = contentContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = contentContainer.GetChild(i);
            child.SetParent(null);          // removes it from the layout immediately
            Destroy(child.gameObject);
        }
    }

}