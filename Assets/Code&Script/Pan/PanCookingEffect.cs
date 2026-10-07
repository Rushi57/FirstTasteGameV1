using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanCookingEffect : MonoBehaviour
{
    [Header("Heat")]
    public StoveHeatController stove;
    [Tooltip("Cooking speed per heat level, indexed by (int)StoveHeat. Index 0 = Off.")]
    public float[] heatSpeed = { 0f, 0.5f, 1f, 1.5f };

    [Header("Timing (seconds at speed 1)")]
    public float secondsToCook = 20f;
    public bool canBurn = true;
    public float secondsToBurn = 25f;   // extra time after fully cooked

    [Header("Tints (multiplied with the sprite)")]
    public Color cookedTint = new Color(0.78f, 0.50f, 0.30f, 1f);
    public Color burntTint = new Color(0.15f, 0.10f, 0.08f, 1f);

    private class Item { public Image img; public Color raw; public float time; }
    private readonly List<Item> items = new List<Item>();

    private void Awake()
    {
        if (stove == null) stove = GetComponentInParent<StoveHeatController>();
    }

    public void Track(Image img)
    {
        if (img == null) return;
        foreach (var i in items) if (i.img == img) return;   // already tracked
        items.Add(new Item { img = img, raw = img.color });
    }

    private float Speed()
    {
        if (stove == null) return 0f;
        int h = Mathf.Clamp((int)stove.CurrentHeat, 0, heatSpeed.Length - 1);
        return heatSpeed[h];
    }

    private void Update()
    {
        float speed = Speed();
        if (speed <= 0f) return;

        float maxTime = secondsToCook + (canBurn ? secondsToBurn : 0f);

        for (int n = items.Count - 1; n >= 0; n--)
        {
            Item it = items[n];
            if (it.img == null) { items.RemoveAt(n); continue; }

            it.time = Mathf.Min(it.time + Time.deltaTime * speed, maxTime);

            it.img.color = it.time <= secondsToCook
                ? Color.Lerp(it.raw, cookedTint, it.time / secondsToCook)
                : Color.Lerp(cookedTint, burntTint, (it.time - secondsToCook) / secondsToBurn);
        }
    }

    /// <summary>0 = raw, 1 = perfectly cooked, above 1 = overcooked.</summary>
    public float GetDoneness(Image img)
    {
        foreach (var i in items)
            if (i.img == img) return i.time / secondsToCook;
        return 0f;
    }

    /// <summary>Call on Retry / new dish.</summary>
    public void ResetCooking()
    {
        foreach (var i in items)
            if (i.img != null) i.img.color = i.raw;
        items.Clear();
    }
}