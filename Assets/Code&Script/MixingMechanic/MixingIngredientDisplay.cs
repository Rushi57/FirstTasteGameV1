using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MixingIngredientDisplay : MonoBehaviour
{
    [Header("Source (stove pan)")]
    [SerializeField] private Transform ingredientAnchorSource;   // IngredientAnchoringPoint under Pan

    [Header("Target (mixing panel)")]
    [SerializeField] private RectTransform displayContainer;     // WhatInsideOfAchorPointandLiquid
    [SerializeField] private bool hideContainerBackground = true;

    [Header("Mix sizes (by IngredientData.size)")]
    [SerializeField] private Vector2 smallMixSize = new Vector2(130, 70);
    [SerializeField] private Vector2 largeMixSize = new Vector2(200, 145);
    [SerializeField] private bool useIconScale = true;           // lets garlic etc. be smaller via iconScale
    [SerializeField] private Vector2 defaultSize = new Vector2(130, 70);  // fallback if no IngredientData found

    [Header("Auto-fit (for dishes with many ingredients)")]
    [SerializeField, Range(0.3f, 1f)] private float minScale = 0.55f;
    [SerializeField, Range(0.8f, 0.95f)] private float shrinkStep = 0.9f;

    [Header("Safe play area (inside the pan)")]
    [SerializeField] private Vector2 playAreaSize = new Vector2(300, 180);
    [SerializeField] private int placementAttempts = 40;

    [Header("Liquid (from the stove pan)")]
    [SerializeField] private Transform liquidSource;                  // LiquidEffect under Pan
    [SerializeField] private Vector2 liquidSize = new Vector2(260, 150);
    [SerializeField] private bool weightByFill = true;   // more liquid poured = stronger color in the mix

    private readonly List<GameObject> spawned = new();

    private struct Item
    {
        public string name;
        public Sprite sprite;
        public Color tint;
        public Vector2 size;
    }

    private void OnEnable() => Refresh();

    public void Refresh()
    {
        Clear();
        if (ingredientAnchorSource == null || displayContainer == null) return;

        if (hideContainerBackground && displayContainer.TryGetComponent(out Image bg))
            bg.color = new Color(1, 1, 1, 0);

        SpawnLiquid();

        // 1. Collect whatever is on the pan right now
        var items = new List<Item>();
        foreach (Transform child in ingredientAnchorSource)
        {
            if (!child.gameObject.activeInHierarchy) continue;

            Sprite sprite = null;
            Color tint = Color.white;
            if (child.TryGetComponent(out Image img)) { sprite = img.sprite; tint = img.color; }
            else if (child.TryGetComponent(out SpriteRenderer sr)) { sprite = sr.sprite; tint = sr.color; }
            if (sprite == null) continue;

            Vector2 size = defaultSize;
            if (child.TryGetComponent(out TutorialInteractable ti) && ti.sourceData is IngredientData d)
            {
                size = d.size == ItemSize.Small ? smallMixSize : largeMixSize;
                if (useIconScale && d.iconScale > 0f) size *= d.iconScale;
            }

            items.Add(new Item { name = child.name, sprite = sprite, tint = tint, size = size });
        }

        if (items.Count == 0) return;

        // 2. Biggest first so small ones fill the gaps
        items.Sort((a, b) => (b.size.x * b.size.y).CompareTo(a.size.x * a.size.y));

        // 3. Spawn, all centered at (0,0). Biggest first = drawn first = sits at the back
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var go = new GameObject(item.name + "_Mix", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(displayContainer, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = FitToPlayArea(item.size);
            rt.anchoredPosition = Vector2.zero;       // exact center
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;

            var newImg = go.GetComponent<Image>();
            newImg.sprite = item.sprite;
            newImg.color = item.tint;
            newImg.preserveAspect = true;
            newImg.raycastTarget = false;

            spawned.Add(go);
        }

       
    }

    // Never let a single image be bigger than the play area
    private Vector2 FitToPlayArea(Vector2 size)
    {
        float k = Mathf.Min(1f, playAreaSize.x / size.x, playAreaSize.y / size.y);
        return size * k;
    }

    public void Clear()
    {
        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
    }

    // Green box in the Scene view (select MixingDashBoard) showing the safe play area
    private void OnDrawGizmosSelected()
    {
        if (displayContainer == null) return;
        Gizmos.matrix = displayContainer.localToWorldMatrix;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(playAreaSize.x, playAreaSize.y, 0));
    }
    private void SpawnLiquid()
    {
        if (liquidSource == null) return;

        Color sum = Color.clear;     // weighted rgb
        float totalWeight = 0f;
        float maxAlpha = 0f;
        Sprite sprite = null;
        float bestWeight = -1f;

        foreach (Transform child in liquidSource)
        {
            if (!child.gameObject.activeInHierarchy) continue;
            if (!child.TryGetComponent(out Image src) || src.sprite == null) continue;
            if (src.color.a <= 0.01f) continue;

            // How much of this liquid is there? Filled images use their fill amount.
            float weight = 1f;
            if (weightByFill && src.type == Image.Type.Filled)
                weight = src.fillAmount;
            if (weight <= 0.01f) continue;

            sum += new Color(src.color.r, src.color.g, src.color.b, 0f) * weight;
            totalWeight += weight;
            maxAlpha = Mathf.Max(maxAlpha, src.color.a);

            // Use the sprite of the most plentiful liquid for the shape
            if (weight > bestWeight) { bestWeight = weight; sprite = src.sprite; }
        }

        if (totalWeight <= 0f || sprite == null) return;

        Color blended = sum / totalWeight;
        blended.a = maxAlpha;

        var go = new GameObject("Liquid_Mix", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(displayContainer, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = liquidSize;
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = blended;
        img.raycastTarget = false;

        spawned.Add(go);
    }
}