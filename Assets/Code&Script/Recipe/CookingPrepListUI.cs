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

    [Header("Step order rules")]                                         // NEW
    [Tooltip("If true, steps must be done in list order. Wrong/skipped actions are rejected.")]
    public bool strictOrder = true;                                      // NEW
    public string wrongStepMessage = "Wrong Step!\nMinus 1 heart";       // NEW

    private readonly Dictionary<string, CookingPrepStepRowUI> rows = new Dictionary<string, CookingPrepStepRowUI>();
    private readonly HashSet<string> completed = new HashSet<string>();
    private readonly List<string> orderedIds = new List<string>();       // NEW

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
                if (!orderedIds.Contains(id)) orderedIds.Add(id);        // NEW
                row.SetCompleted(completed.Contains(id));
            }
            else
            {
                row.SetCompleted(false);
            }
        }

        ResetScrollToTop();
    }

    // ---------------- NEW: step gate ----------------

    /// <summary>The first step (in list order) that isn't completed yet. Null if all done.</summary>
    public string CurrentStepId
    {
        get
        {
            foreach (string id in orderedIds)
                if (!completed.Contains(id)) return id;
            return null;
        }
    }

    /// <summary>True if this id is allowed to happen right now.</summary>
    public bool IsExpected(string id)
    {
        if (!strictOrder) return true;
        if (orderedIds.Count == 0) return true;   // list not shown yet, don't gate
        string current = CurrentStepId;
        if (current == null) return true;         // everything already done
        return current == id;
    }

    /// <summary>
    /// Call BEFORE a mini-game applies its result. If it returns false, the action
    /// must be cancelled (return early, snap the item back, etc). The warning popup
    /// and heart loss are already handled here.
    /// </summary>
    public bool TryAccept(string id)
    {
        if(orderedIds.Count > 0 && !rows.ContainsKey(id))
        {
            Debug.Log($"[PrepList] REJECTED '{id}' - not in this recipe's prep list");
            ScoreManager.Instance?.ReportMistake("Wrong Ingredient!\nMinus 1 heart");
            return false;
        }

        if (IsExpected(id)) return true;

        Debug.Log($"[PrepList] REJECTED '{id}' - current step is '{CurrentStepId}'");
        ScoreManager.Instance?.ReportMistake(wrongStepMessage);
        return false;
    }

    public bool TryAcceptIngredientForCutting(string ingredientId)
    {
        if(!strictOrder || orderedIds.Count == 0) return true;

        string current = CurrentStepId;
        if(current == null) return true;

        bool isCutStep = current.EndsWith(":" + IngredientPrepState.Sliced, System.StringComparison.Ordinal)
                   || current.EndsWith(":" + IngredientPrepState.Minced, System.StringComparison.Ordinal);
        bool sameIngredient = current.StartsWith(ingredientId + ":", System.StringComparison.Ordinal);

        if (isCutStep && sameIngredient) return true;

        Debug.Log($"[PrepList] REJECTED ingredient '{ingredientId}' on chopping board - current step is '{current}'");
        ScoreManager.Instance?.ReportMistake(wrongStepMessage);
        return false;

    }

    // ------------------------------------------------

    private void HandleCutStateChanged(IngredientData data, IngredientPrepState state)
    {
        Debug.Log($"[PrepList] cut event received: {(data != null ? data.id : "null")}:{state}");
        if (data == null || state == IngredientPrepState.Whole) return;
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
        if (completed.Contains(stepId))
        {
            Debug.Log($"[PrepList] '{stepId}' was already completed - skipping.");
            return;
        }
        if (!IsExpected(stepId))                                         // NEW: safety net
        {
            Debug.Log($"[PrepList] '{stepId}' finished out of order (current = '{CurrentStepId}') - not registered.");
            return;
        }

        completed.Add(stepId);
        Debug.Log($"[PrepList] row object = {row.name}, sibling index = {row.transform.GetSiblingIndex()}");
        row.SetCompleted(true);

        if (completed.Count >= rows.Count)
            OnAllStepsComplete?.Invoke();
    }

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
        orderedIds.Clear();                                              // NEW
        if (contentContainer == null) return;

        for (int i = contentContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = contentContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
    }
}
