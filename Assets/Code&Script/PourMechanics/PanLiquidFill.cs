using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanLiquidFill : MonoBehaviour
{
    [System.Serializable]
    public class LiquidStyle
    {
        [Tooltip("Must match: Soy, Vinegar, CookingOil, Water")]
        public string id = "Soy";
        public Color color = Color.white;
        [Tooltip("Higher = this liquid dominates the mix. Lower = it only tints slightly.")]
        public float tintStrength = 1f;
        [Range(0f, 1f), Tooltip("How see-through this liquid is on its own (water should be low).")]
        public float opacity = 1f;
    }

    [SerializeField] private Image blob;
    [SerializeField] private List<LiquidStyle> styles = new List<LiquidStyle>();

    [Header("Look")]
    [Range(0f, 1f), Tooltip("Overall cap on the blob's alpha. Lower = more transparent.")]
    [SerializeField] private float maxAlpha = 0.6f;

    [Header("Animation")]
    [SerializeField] private float fadeInTime = 0.4f;
    [SerializeField] private float blendTime = 0.6f;
    [SerializeField] private float pulseScale = 1.08f;
    [SerializeField] private float growPerMl = 0.004f;

    private readonly Dictionary<string, float> amounts = new Dictionary<string, float>();
    private Coroutine routine;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (blob != null) baseScale = blob.transform.localScale;
        Clear();
    }

    // Called from PourSeasoningController (no change needed there)
    public void AddLiquid(SeasoningType type, Color unused, float ml)
    {
        AddLiquidById(type.ToString(), ml);
    }

    // Call this wherever the player adds water
    public void AddWater(float ml)
    {
        AddLiquidById("Water", ml);
    }

    public void AddLiquidById(string id, float ml)
    {
        if (blob == null) return;

        amounts.TryGetValue(id, out float old);
        amounts[id] = old + Mathf.Max(ml, 0.1f);

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(AnimateTo(GetMix(), GetTargetScale()));
    }

    public void Clear()
    {
        if (routine != null) StopCoroutine(routine);
        amounts.Clear();
        if (blob == null) return;
        Color c = blob.color; c.a = 0f; blob.color = c;
        blob.transform.localScale = baseScale * 0.5f;
    }

    private LiquidStyle GetStyle(string id)
    {
        foreach (var s in styles) if (s.id == id) return s;
        return null;
    }

    private Color GetMix()
    {
        Vector3 rgb = Vector3.zero;
        float alpha = 0f, total = 0f;

        foreach (var kv in amounts)
        {
            var s = GetStyle(kv.Key);
            Color c = s != null ? s.color : Color.white;
            float strength = s != null ? s.tintStrength : 1f;
            float opacity = s != null ? s.opacity : 1f;

            float w = kv.Value * strength;          // amount x strength
            rgb += new Vector3(c.r, c.g, c.b) * w;
            alpha += opacity * w;
            total += w;
        }
        if (total <= 0f) return Color.clear;

        rgb /= total;
        return new Color(rgb.x, rgb.y, rgb.z, (alpha / total) * maxAlpha);
    }

    private float GetTargetScale()
    {
        float total = 0f;
        foreach (var kv in amounts) total += kv.Value;
        return Mathf.Clamp(1f + total * growPerMl, 1f, 1.4f);
    }

    private IEnumerator AnimateTo(Color target, float targetScale)
    {
        Color start = blob.color;
        bool first = start.a < 0.01f;
        if (first) start = new Color(target.r, target.g, target.b, 0f);

        Vector3 startScale = blob.transform.localScale;
        Vector3 endScale = baseScale * targetScale;
        float duration = first ? fadeInTime : blendTime;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float e = Mathf.SmoothStep(0f, 1f, t);
            blob.color = Color.Lerp(start, target, e);
            float pulse = first ? 1f : 1f + Mathf.Sin(e * Mathf.PI) * (pulseScale - 1f);
            blob.transform.localScale = Vector3.Lerp(startScale, endScale, e) * pulse;
            yield return null;
        }
        blob.color = target;
        blob.transform.localScale = endScale;
    }
}