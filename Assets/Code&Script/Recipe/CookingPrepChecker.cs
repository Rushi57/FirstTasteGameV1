using UnityEngine;
using System.Collections.Generic;

public class CookingPrepChecker : MonoBehaviour
{
    public static CookingPrepChecker Instance { get; private set; }

    [System.Serializable]
    public class PrepStep
    {
        [Tooltip("Chopping steps use IngredientId:State, e.g. PorkItem:Sliced. Other steps use any name you like, e.g. Stove:HighHeat")]
        public string stepId;
        public string instruction;
    }
    [Header("References")]
    public CuttingMechanic cuttingMechanic;
    public Transform listContainer;          // the parent that holds the rows
    public CookingPrepListUI itemPrefab;     // ListCookingPrepPrefab

    [Header("Steps (in display order)")]
    public List<PrepStep> steps = new List<PrepStep>();

    private readonly Dictionary<string, CookingPrepListUI> rows = new Dictionary<string, CookingPrepItemUI>();
    private readonly HashSet<string> completed = new HashSet<string>();

    private void Awake()
    {
        if(Instance != null && Instance != this) { Destroy(gameObject); return; }
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
    private void Start() => BuildList();

    public void BuildList()
    {
        foreach (Transform child in listContainer) Destroy(child.gameObject);
        rows.Clear();
        completed.Clear();

        foreach (var step in steps)
        {
            var row = Instantiate(itemPrefab, listContainer);
            row.Setup(step.instruction);
            rows[step.stepId] = row;
        }
    }
    // Chopping: fired by CuttingMechanic after each cut
    private void HandleCutStateChanged(IngredientData data, IngredientPrepState state)
    {
        if (data == null || state == IngredientPrepState.Whole) return; // ignore the reset
        CompleteStep(ChopStepId(data, state));
    }

    public static string ChopStepId(IngredientData data, IngredientPrepState state)
        => $"{data.id}:{state}";

    /// <summary>Call from ANY mechanic when it finishes.</summary>
    public void CompleteStep(string stepId)
    {
        if (!rows.TryGetValue(stepId, out var row)) return; // not part of this recipe
        if (!completed.Add(stepId)) return;                 // already done

        row.SetCompleted(true);

        if (completed.Count >= rows.Count)
            OnAllStepsComplete?.Invoke();
    }

    public bool IsStepComplete(string stepId) => completed.Contains(stepId);

    /// <summary>Call on Retry / new dish.</summary>
    public void ResetAll() => BuildList();
}
