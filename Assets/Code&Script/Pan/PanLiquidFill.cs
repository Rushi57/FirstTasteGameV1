using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanLiquidFill : MonoBehaviour
{
    [System.Serializable]
    public class LiquidStyle
    {
        public SeasoningType type;
        [Tooltip("How strongly this liquid tints the mix (soy should be high).")]
        public float tintStrength = 1f;
    }

    [SerializeField] private Image blob;
    [SerializeField] private List<LiquidStyle> styles = new List<LiquidStyle>();
    [SerializeField] private float fadeInTime = 0.4f;
    [SerializeField] private float blendTime = 0.6f;
    [SerializeField] private float pulseScale = 1.08f;
    [Tooltip("Blob size grows slightly with total amount poured (0 = fixed size).")]
    [SerializeField] private float growPerMl = 0.004f;

    private readonly Dictionary<SeasoningType, float> amounts = new Dictionary<SeasoningType, float>();
    private readonly Dictionary<SeasoningType, Color> colors = new Dictionary<SeasoningType, Color>();
    private Coroutine routine;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (blob != null) baseScale = blob.transform.localScale;
        Clear();
    }

    /// <summary>Call after a correct pour finishes its animation.</summary>
    public void AddLiquid(SeasoningType type, Color color, float ml)
    {
        if (blob == null) return;

        amounts.TryGetValue(type, out float old);
        amounts[type] = old + Mathf.Max(ml, 0.1f);
        colors[type] = color;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(AnimateTo(GetMix(), GetTargetScale()));
    }

    public void Clear()
    {
        if (routine != null) StopCoroutine(routine);
        amounts.Clear();
        colors.Clear();
        if (blob == null) return;
        Color c = blob.color; c.a = 0f; blob.color = c;
        blob.transform.localScale = baseScale * 0.5f;
    }

    private Color GetMix()
    {
        Color sum = Color.black;
        float total = 0f;

        foreach (var kv in amounts)
        {
            float w = kv.Value * GetStrength(kv.Key);
            sum += colors[kv.Key] * w;
            total += w;
        }
        if (total <= 0f) return Color.clear;

        Color mix = sum / total;
        mix.a = 1f;
        return mix;
    }

    private float GetStrength(SeasoningType t)
    {
        foreach (var s in styles) if (s.type == t) return s.tintStrength;
        return 1f;
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