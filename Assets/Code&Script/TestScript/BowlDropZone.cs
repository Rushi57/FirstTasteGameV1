using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Spawned onto the Bowl by IngredientSpawner (or put on a bowl manually).
/// Drag a cooked ingredient out of the pan and drop it on the bowl to "set it aside".
/// Completes the prep-list step "{stepPrefix}{ingredientId}" (e.g. SetAside:ChickenLegs).
/// The ingredient can be dragged out again (e.g. to return it to the pan later).
/// </summary>
public class BowlDropZone : MonoBehaviour, IDropHandler
{
    [Header("Step")]
    [Tooltip("Step id = prefix + ingredient id, e.g. 'SetAside:ChickenLegs'. Must match your prep list step.")]
    public string stepPrefix = "SetAside:";

    [Header("What can be set aside")]
    [Tooltip("IngredientData.id values accepted by the bowl. Leave empty to accept any ingredient from the pan.")]
    public List<string> acceptedIngredientIds = new List<string> { "ChickenLegs" };

    [Header("Requirements")]
    [Tooltip("Only accept ingredients that were actually added to the pan first.")]
    public bool requireInPan = true;
    public PanDropZone panZone;

    [Header("Cooking")]
    [Tooltip("Optional. Stops the ingredient from browning further once it is in the bowl.")]
    public PanCookingEffect cookingEffect;
    [Tooltip("Reject the drop if the ingredient is less cooked than this (0 = no check, 1 = fully cooked).")]
    [Range(0f, 1f)] public float minDoneness = 0f;

    [Header("Sound")]
    [Tooltip("Stop the pan's frying loop when the ingredient is taken out of the pan.")]
    public bool stopFryingSound = true;

    [Header("Placement")]
    [Tooltip("Where accepted ingredients land. Leave empty to use the bowl's own RectTransform.")]
    public RectTransform ingredientAnchor;
    public float scaleInBowl = 0.8f;
    [Tooltip("Random wiggle around the landing spot, so pieces don't stack exactly.")]
    public float scatter = 15f;
    [Tooltip("Where the ingredient lands, measured from the bowl's centre (or Ingredient Anchor). X = left/right, Y = up/down.")]
    public Vector2 dropOffset = Vector2.zero;
    [Tooltip("ON = the ingredient stays where the player let go (kept inside the bowl). OFF = it goes to Drop Offset.")]
    public bool placeAtDropPoint = false;
    [Tooltip("When Place At Drop Point is ON, keeps the ingredient this far from the bowl's edge.")]
    public float edgePadding = 20f;
    [Tooltip("If ON the ingredient can't be dragged out of the bowl again. Keep OFF if a later step returns it to the pan.")]
    public bool lockAfterDrop = false;

    public event System.Action<IngredientData> OnSetAside;

    private struct Aside { public Transform t; public Vector3 scale; }

    private readonly HashSet<string> inPan = new HashSet<string>();
    private readonly Dictionary<string, Aside> aside = new Dictionary<string, Aside>();
    private TutorialInteractable tutorialTag;

    private void Awake()
    {
        tutorialTag = GetComponent<TutorialInteractable>();
    }

    private void OnEnable()
    {
        if (panZone != null)
        {
            panZone.OnIngredientAdded -= HandleAddedToPan;
            panZone.OnIngredientAdded += HandleAddedToPan;
            SeedFromPan();   // catch up on what was added while this object was inactive (e.g. closed cabinet)
        }
    }

    private void OnDisable()
    {
        if (panZone != null) panZone.OnIngredientAdded -= HandleAddedToPan;
    }

    /// <summary>Used by IngredientSpawner when the bowl is spawned at runtime.</summary>
    public void Setup(PanDropZone pan, PanCookingEffect cooking,
                      List<string> acceptedIds = null, string prefix = null, bool? lockAfter = null)
    {
        if (panZone != null) panZone.OnIngredientAdded -= HandleAddedToPan;

        panZone = pan;
        cookingEffect = cooking;

        if (acceptedIds != null && acceptedIds.Count > 0)
            acceptedIngredientIds = new List<string>(acceptedIds);
        if (!string.IsNullOrEmpty(prefix))
            stepPrefix = prefix;
        if (lockAfter.HasValue)
            lockAfterDrop = lockAfter.Value;

        if (panZone == null) return;
        panZone.OnIngredientAdded += HandleAddedToPan;
        SeedFromPan();
    }

    /// <summary>Remember ingredients that are already sitting in the pan.</summary>
    private void SeedFromPan()
    {
        if (panZone == null) return;
        RectTransform a = panZone.ingredientAnchor != null ? panZone.ingredientAnchor : panZone.transform as RectTransform;
        if (a == null) return;

        foreach (Transform child in a)
        {
            var ti = child.GetComponent<TutorialInteractable>();
            var d = ti != null ? ti.sourceData as IngredientData : null;
            if (d != null) inPan.Add(d.id);
        }
    }

    /// <summary>Used by IngredientSpawner to pass the placement settings.</summary>
    public void ConfigurePlacement(Vector2 offset, float scatterAmount, float scale, bool atDropPoint)
    {
        dropOffset = offset;
        scatter = scatterAmount;
        scaleInBowl = scale;
        placeAtDropPoint = atDropPoint;
    }

    // Fires whenever something is added to the pan (including coming BACK from the bowl)
    private void HandleAddedToPan(IngredientData data)
    {
        if (data == null) return;
        inPan.Add(data.id);

        if (aside.TryGetValue(data.id, out Aside a))
        {
            if (a.t != null)
            {
                a.t.localScale = a.scale;                       // back to normal size
                TestDrag d = a.t.GetComponent<TestDrag>();
                if (d != null) d.enabled = true;
            }
            aside.Remove(data.id);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null || dropped == gameObject) return;

        TestDrag drag = dropped.GetComponent<TestDrag>();
        if (drag == null || drag.IsLocked) return;

        var interactable = dropped.GetComponent<TutorialInteractable>();
        IngredientData data = interactable != null ? interactable.sourceData as IngredientData : null;
        if (data == null) return;   // utensils (pan, spatula, lid...) bounce back via TestDrag

        if (acceptedIngredientIds != null && acceptedIngredientIds.Count > 0
            && !acceptedIngredientIds.Contains(data.id))
            return;

        // Already in the bowl (dragged out and dropped back on it): just let it snap back, no penalty
        if (aside.ContainsKey(data.id)) return;

        if (requireInPan && panZone != null && !inPan.Contains(data.id))
        {
            Debug.Log($"[BowlDrop] '{data.id}' was never added to the pan - ignoring.");
            return;
        }

        Image img = dropped.GetComponent<Image>();
        if (img == null) img = dropped.GetComponentInChildren<Image>();

        if (cookingEffect != null && minDoneness > 0f && img != null
            && cookingEffect.GetDoneness(img) < minDoneness)
        {
            Debug.Log($"[BowlDrop] '{data.id}' is not cooked enough yet - ignoring.");
            return;
        }

        string stepId = stepPrefix + data.id;

        // Only works when the prep list says this step is next
        if (CookingPrepListUI.Instance != null && !CookingPrepListUI.Instance.TryAccept(stepId))
            return;

        Debug.Log($"[BowlDrop] '{data.id}' set aside -> completing '{stepId}'");

        if (cookingEffect != null) cookingEffect.Untrack(img);

        Vector3 originalScale = dropped.transform.localScale;

        RectTransform target = ingredientAnchor != null ? ingredientAnchor : transform as RectTransform;
        drag.SnapTo(target);

        RectTransform rt = dropped.transform as RectTransform;
        if (rt != null)
        {
            Vector2 pos;
            if (placeAtDropPoint && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    target, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                Rect r = target.rect;
                float minX = Mathf.Min(r.xMin + edgePadding, r.center.x);
                float maxX = Mathf.Max(r.xMax - edgePadding, r.center.x);
                float minY = Mathf.Min(r.yMin + edgePadding, r.center.y);
                float maxY = Mathf.Max(r.yMax - edgePadding, r.center.y);
                pos = new Vector2(Mathf.Clamp(local.x, minX, maxX), Mathf.Clamp(local.y, minY, maxY));
            }
            else
            {
                pos = dropOffset + new Vector2(Random.Range(-scatter, scatter), Random.Range(-scatter, scatter));
            }
            rt.anchoredPosition = pos;
        }
        dropped.transform.localScale = Vector3.one * scaleInBowl;

        aside[data.id] = new Aside { t = dropped.transform, scale = originalScale };
        inPan.Remove(data.id);
        if (lockAfterDrop) drag.enabled = false;

        CookingPrepListUI.Instance?.CompleteStep(stepId);

        if (stopFryingSound)
        {
            if (panZone != null) panZone.ResetCookingSound();   // pan state -> None + StopLoop()
            else SFXManager.Instance?.StopLoop();
        }
        panZone?.NotifyIngredientRemoved(data);

        OnSetAside?.Invoke(data);
        tutorialTag?.ReportDrop();
    }
}