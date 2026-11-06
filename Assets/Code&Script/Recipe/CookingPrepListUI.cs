using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One instance lives on IngCutPrepScrollObj (chopping steps) and one on
/// CookingPrepScrollingObj (stove/oil/etc). Only the instance on the ACTIVE
/// panel is CookingPrepListUI.Instance, so existing mechanics that call
/// Instance.TryAccept / CompleteStep keep working in each stage.
///
/// Repeated step ids (e.g. several "Pan:PanLidSimmer" rows) are supported:
/// each row gets an internal unique key ("id", "id#2", "id#3", ...), and
/// CompleteStep(id) ticks the first occurrence that isn't done yet.
/// </summary>
public class CookingPrepListUI : MonoBehaviour
{
    public static CookingPrepListUI Instance { get; private set; }

    [Tooltip("The ScrollView's Content transform - rows get spawned as children of this.")]
    public Transform contentContainer;
    public GameObject stepRowPrefab;
    public ScrollRect scrollRect;

    [Header("Auto Scroll")]
    public float scrollDuration = 0.3f;

    private Coroutine scrollRoutine;
    private float scrollTargetNorm = 1f;

    [Header("Auto-complete chopping steps")]
    [Tooltip("Assign this on the CUT PREP list only. Leave empty on the Cooking Prep list.")]
    public CuttingMechanic cuttingMechanic;


    [Header("Step order rules")]
    [Tooltip("If true, steps must be done in list order. Wrong/skipped actions are rejected.")]
    public bool strictOrder = true;
    public string wrongStepMessage = "Wrong Step!\nMinus 1 heart";

    // unique key -> row
    private readonly Dictionary<string, CookingPrepStepRowUI> rows = new Dictionary<string, CookingPrepStepRowUI>();
    // unique keys that are done
    private readonly HashSet<string> completed = new HashSet<string>();
    // unique keys in list order
    private readonly List<string> orderedIds = new List<string>();
    // base id -> unique keys (in list order)
    private readonly Dictionary<string, List<string>> occurrences = new Dictionary<string, List<string>>();
    // unique key -> base id
    private readonly Dictionary<string, string> keyToBase = new Dictionary<string, string>();

    public string wrongMeasurementMessage = "Wrong Measurement!\nMinus 1 heart";

    // unique key -> instruction text
    private readonly Dictionary<string, string> keyToText = new Dictionary<string, string>();
    public bool HasStep(string id) => occurrences.ContainsKey(id);

    /// <summary>Fires once every auto-completable step in THIS list is done.</summary>
    public event System.Action OnAllStepsComplete;

    public static event System.Action<string> StepCompleted;

    private void OnEnable()
    {
        Instance = this;
        if (cuttingMechanic != null) cuttingMechanic.OnStateChanged += HandleCutStateChanged;
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        if (cuttingMechanic != null) cuttingMechanic.OnStateChanged -= HandleCutStateChanged;
    }

    /// <summary>Shows every instruction (old behaviour).</summary>
    public void DisplaySteps(RecipeData recipe)
    {
        if (recipe == null) { Clear(); return; }

        var all = new List<int>();
        for (int i = 0; i < recipe.cookingInstructions.Count; i++) all.Add(i);
        DisplaySteps(recipe, all);
    }

    /// <summary>Shows only the instructions at the given indices (cut steps OR cooking steps).</summary>
    public void DisplaySteps(RecipeData recipe, List<int> stepIndices)
    {
        Debug.Log($"[PrepList] DisplaySteps called on {name}. Existing children: {(contentContainer != null ? contentContainer.childCount : 0)}", this);
        Clear();
        completed.Clear();
        if (recipe == null || stepIndices == null || contentContainer == null || stepRowPrefab == null) return;

        foreach (int i in stepIndices)
        {
            if (i < 0 || i >= recipe.cookingInstructions.Count) continue;

            GameObject rowGO = Instantiate(stepRowPrefab, contentContainer);
            CookingPrepStepRowUI row = rowGO.GetComponent<CookingPrepStepRowUI>();
            if (row == null)
            {
                Debug.LogWarning("[CookingPrepListUI] stepRowPrefab is missing a CookingPrepStepRowUI component.");
                continue;
            }

            row.SetText(recipe.cookingInstructions[i]);

            // Id is built from the ORIGINAL index, so stove order etc. stays correct.
            string id = recipe.GetStepId(i);
            Debug.Log($"[PrepList] row {i} -> id = '{id}'");
            if (!string.IsNullOrEmpty(id))
            {
                // Repeated ids get a unique internal key so rows don't overwrite each other.
                if (!occurrences.TryGetValue(id, out var list))
                    occurrences[id] = list = new List<string>();

                string key = list.Count == 0 ? id : $"{id}#{list.Count + 1}";
                list.Add(key);

                rows[key] = row;
                keyToBase[key] = id;
                keyToText[key] = recipe.cookingInstructions[i];
                orderedIds.Add(key);
            }
            row.SetCompleted(false);
        }

        ResetScrollToTop();
    }

    // ---------------- Step gate ----------------

    /// <summary>Unique key of the first step (in list order) that isn't completed yet. Null if all done.</summary>
    private string CurrentKey
    {
        get
        {
            foreach (string key in orderedIds)
                if (!completed.Contains(key)) return key;
            return null;
        }
    }

    /// <summary>Base id of the first step (in list order) that isn't completed yet. Null if all done.</summary>
    public string CurrentStepId => CurrentKey != null ? keyToBase[CurrentKey] : null;
    /// <summary>Instruction text of the first step that isn't completed yet. Null if all done.</summary>
    public string CurrentStepText =>
        CurrentKey != null && keyToText.TryGetValue(CurrentKey, out var t) ? t : null;
    public bool IsExpected(string id)
    {
        if (!strictOrder) return true;
        if (orderedIds.Count == 0) return true;
        string current = CurrentStepId;
        if (current == null) return true;
        return current == id;
    }

    public bool TryAccept(string id)
    {
        if (orderedIds.Count > 0 && !occurrences.ContainsKey(id))
        {
            Debug.Log($"[PrepList] REJECTED '{id}' - not in this panel's step list");
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
        if (!strictOrder || orderedIds.Count == 0) return true;

        string current = CurrentStepId;
        if (current == null) return true;

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
        string id = $"{data.id}:{state}";
        if (!occurrences.ContainsKey(id)) return;   // intermediate state (e.g. Sliced on the way to Minced)
        CompleteStep(id);
    }

    /// <summary>Call from ANY mechanic when it finishes.</summary>
    public void CompleteStep(string stepId)
    {
        if (!occurrences.TryGetValue(stepId, out var keys))
        {
            Debug.LogWarning($"[PrepList] no row for '{stepId}'. Registered ids: {string.Join(", ", occurrences.Keys)}");
            return;
        }

        // First not-yet-completed occurrence of this id.
        string key = keys.Find(k => !completed.Contains(k));
        if (key == null)
        {
            Debug.Log($"[PrepList] '{stepId}' was already completed - skipping.");
            return;
        }
        if (!IsExpected(stepId))
        {
            Debug.Log($"[PrepList] '{stepId}' finished out of order (current = '{CurrentStepId}') - not registered.");
            return;
        }

        var row = rows[key];
        completed.Add(key);
        row.SetCompleted(true);
        StepCompleted?.Invoke(stepId);

        string nextKey = CurrentKey;
        if (nextKey != null && rows.TryGetValue(nextKey, out var nextRow))
        {
            float step = Mathf.Abs(
                ((RectTransform)nextRow.transform).anchoredPosition.y -
                ((RectTransform)row.transform).anchoredPosition.y);
            ScrollBy(step);
        }

        if (completed.Count >= rows.Count)
            OnAllStepsComplete?.Invoke();
    }

    public void ResetProgress()
    {
        completed.Clear();
        ResetScrollToTop();
    }

    // ---------------- Scrolling ----------------

    private void ResetScrollToTop()
    {
        if (scrollRoutine != null) { StopCoroutine(scrollRoutine); scrollRoutine = null; }
        if (scrollRect == null) return;

        Canvas.ForceUpdateCanvases();
        scrollRect.velocity = Vector2.zero;
        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.horizontalNormalizedPosition = 0f;
        scrollTargetNorm = 1f;
    }

    private void ScrollBy(float distance)
    {
        if (scrollRect == null || !isActiveAndEnabled) return;

        if (scrollRoutine == null)
            scrollTargetNorm = scrollRect.verticalNormalizedPosition;
        else
            StopCoroutine(scrollRoutine);

        scrollRoutine = StartCoroutine(ScrollRoutine(distance));
    }

    private IEnumerator ScrollRoutine(float distance)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport != null
            ? scrollRect.viewport
            : (RectTransform)scrollRect.transform;

        float scrollable = content.rect.height - viewport.rect.height;
        if (scrollable <= 0f) { scrollRoutine = null; yield break; }

        scrollRect.velocity = Vector2.zero;

        float start = scrollRect.verticalNormalizedPosition;
        scrollTargetNorm = Mathf.Clamp01(scrollTargetNorm - distance / scrollable);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, scrollDuration);
            scrollRect.verticalNormalizedPosition =
                Mathf.Lerp(start, scrollTargetNorm, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        scrollRect.verticalNormalizedPosition = scrollTargetNorm;
        scrollRoutine = null;
    }

    public bool IsMixStepCurrent()
    {
        string current = CurrentStepId;
        return current != null && current.StartsWith("Mix:", System.StringComparison.Ordinal);
    }

    private void Clear()
    {
        if (scrollRoutine != null) { StopCoroutine(scrollRoutine); scrollRoutine = null; }
        rows.Clear();
        orderedIds.Clear();
        occurrences.Clear();
        keyToText.Clear();
        keyToBase.Clear();
        if (contentContainer == null) return;

        for (int i = contentContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = contentContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
    }
    public bool TryAcceptMeasurement(MeasurementOption chosen)
    {
        string text = CurrentStepText;
        if (!strictOrder || string.IsNullOrEmpty(text)) return true;

        if (!TryGetExpectedMeasurement(text, out MeasurementOption expected))
            return true; // current step isn't a measuring step

        if (expected == chosen) return true;

        Debug.Log($"[PrepList] REJECTED measurement {chosen} - step expects {expected}");
        ScoreManager.Instance?.ReportMistake(wrongMeasurementMessage);
        return false;
    }
    private static bool TryGetExpectedMeasurement(string text, out MeasurementOption result)
    {
        foreach (MeasurementOption o in System.Enum.GetValues(typeof(MeasurementOption)))
        {
            string label = MeasurementTable.Label(o);   // e.g. "1/2 tsp"
                                                        // (?<![\d/.]) stops "1 cup" matching inside "11 cup" or "1/1 cup"
            string pattern = @"(?<![\d/.])" + Regex.Escape(label) + @"\b";
            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
            {
                result = o;
                return true;
            }
        }
        result = default;
        return false;
    }
}