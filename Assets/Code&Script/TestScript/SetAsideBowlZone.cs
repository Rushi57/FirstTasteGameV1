using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Put on BowlDropZone. Accepts any IngredientData (up to maxItems) and arranges
/// them inside the bowl:
///   1 item  -> center
///   2 items -> first slides to the right, second goes on the left
///   3 items -> right, left, top-center
///   4 items -> right, left, top-right, top-left
/// Each drop is validated against the Cooking Prep list (strict order),
/// and ticks the step when it is correct.
/// </summary>
public class SetAsideBowlZone : MonoBehaviour, IDropHandler
{
    [Header("Placement")]
    [Tooltip("Where items are parented. Leave empty to use this object's RectTransform.")]
    public RectTransform itemAnchor;
    public int maxItems = 4;
    [Header("Item Scale (Android-friendly)")]
    [Tooltip("Scale when 1 item is in the bowl.")]
    public float scaleOneItem = 1.2f;
    [Tooltip("Scale when 2 items are in the bowl.")]
    public float scaleTwoItems = 1.1f;
    [Tooltip("Scale when 3-4 items are in the bowl.")]
    public float scaleCrowded = 1.0f;
    [Tooltip("How fast items grow/shrink when the count changes.")]
    public float scaleSpeed = 14f;

    [Header("Layout (in anchor local units)")]
    public float sideOffsetX = 35f;   // left/right distance from center
    public float topOffsetY = 18f;    // raised row for the 3rd/4th item
    public float moveSpeed = 14f;     // slide speed when re-arranging

    [Header("Step matching")]
    [Tooltip("Step id = prefix + IngredientData.id. Check the console line " +
             "'[PrepList] row N -> id = ...' to see the exact id of your bowl step.")]
    public string stepIdPrefix = "Bowl:";

    [Header("Messages")]
    public string bowlFullMessage = "The bowl is full!";

    public event System.Action<IngredientData> OnIngredientAdded;

    private TutorialInteractable tutorialTag;

    private class Entry
    {
        public RectTransform rt;
        public IngredientData data;
        public Vector3 originalScale;
    }

    private readonly List<Entry> items = new List<Entry>();

    private RectTransform Anchor => itemAnchor != null ? itemAnchor : (RectTransform)transform;

    private void Awake()
    {
        tutorialTag = GetComponent<TutorialInteractable>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null || dropped == gameObject) return;

        TestDrag drag = dropped.GetComponent<TestDrag>();
        if (drag == null || drag.IsLocked) return;

        var interactable = dropped.GetComponent<TutorialInteractable>();
        IngredientData data = interactable != null ? interactable.sourceData as IngredientData : null;
        if (data == null)
        {
            Debug.LogWarning($"[Bowl] {dropped.name} has no IngredientData - ignoring.");
            return;
        }

        if (items.Count >= maxItems)
        {
            ScoreManager.Instance?.ReportMistake(bowlFullMessage);
            return;   // TestDrag sends it back to where it came from
        }

        // Cooking Prep gate: wrong item / wrong order is rejected here (and costs a heart)
        string stepId = stepIdPrefix + data.id;
        var prep = CookingPrepListUI.Instance;
        if (prep != null && !prep.TryAccept(stepId)) return;

        // Accept
        RectTransform rt = (RectTransform)dropped.transform;
        var entry = new Entry { rt = rt, data = data, originalScale = rt.localScale };

        drag.SnapTo(Anchor);                 // lands in the center...
        
        items.Add(entry);                    // ...then slides to its slot in LateUpdate

        prep?.CompleteStep(stepId);
        OnIngredientAdded?.Invoke(data);
        tutorialTag?.ReportDrop();
        Debug.Log($"[Bowl] Added '{data.id}' ({items.Count}/{maxItems}) -> completed '{stepId}'");
    }

    private void LateUpdate()
    {
        // Drop anything the player dragged back out of the bowl
        for (int i = items.Count - 1; i >= 0; i--)
        {
            Entry e = items[i];
            if (e.rt == null) { items.RemoveAt(i); continue; }
            if (e.rt.parent != Anchor)
            {
                e.rt.localScale = e.originalScale;
                items.RemoveAt(i);
            }
        }

        float t = 1f - Mathf.Exp(-moveSpeed * Time.unscaledDeltaTime);
        float ts = 1f - Mathf.Exp(-scaleSpeed * Time.unscaledDeltaTime);
        float scaleMul = GetScaleMultiplier(items.Count);

        for (int i = 0; i < items.Count; i++)
        {
            Vector2 target = GetSlot(i, items.Count);
            items[i].rt.anchoredPosition = Vector2.Lerp(items[i].rt.anchoredPosition, target, t);
            items[i].rt.localScale = Vector3.Lerp(
                items[i].rt.localScale, items[i].originalScale * scaleMul, ts);
        }
    }

    private Vector2 GetSlot(int index, int count)
    {
        float x = sideOffsetX, y = topOffsetY;

        switch (count)
        {
            case 1:
                return Vector2.zero;                                  // center
            case 2:
                return index == 0 ? new Vector2(x, 0) : new Vector2(-x, 0);
            case 3:
                if (index == 0) return new Vector2(x, 0);
                if (index == 1) return new Vector2(-x, 0);
                return new Vector2(0, y);
            default:
                if (index == 0) return new Vector2(x, 0);
                if (index == 1) return new Vector2(-x, 0);
                if (index == 2) return new Vector2(x * 0.5f, y);
                return new Vector2(-x * 0.5f, y);
        }
    }

    /// <summary>Call when a recipe resets / restarts.</summary>
    public void ClearAll()
    {
        foreach (var e in items)
            if (e.rt != null) Destroy(e.rt.gameObject);
        items.Clear();
    }
    private float GetScaleMultiplier(int count)
    {
        if (count <= 1) return scaleOneItem;
        if (count == 2) return scaleTwoItems;
        return scaleCrowded;
    }
}