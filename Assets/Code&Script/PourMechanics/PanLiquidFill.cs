using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanLiquidFill : MonoBehaviour
{

  

    [SerializeField] private Image blob;
   

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

    [SerializeField] private DishLiquidProfile profile;
    private float reduction;

    public void SetProfile(DishLiquidProfile p) { profile = p; Clear(); }
    public void SetReduction(float t)
    {
        reduction = Mathf.Clamp01(t);
        if (blob == null || amounts.Count == 0) return;
        if (routine != null) { StopCoroutine(routine); routine = null; }
        blob.color = GetMix();   // applied directly, safe to call every frame
    }

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

    public void Refresh()
    {
        if(blob == null || amounts.Count == 0) return;
        if(routine != null) StopCoroutine(routine);
        routine = StartCoroutine(AnimateTo(GetMix(), GetTargetScale()));
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
        reduction = 0f;          // new
        if (blob == null) return;
        Color c = blob.color; c.a = 0f; blob.color = c;
        blob.transform.localScale = baseScale * 0.5f;
    }

    private Color GetMix()
    {
        if (profile == null || !profile.showLiquid) return Color.clear;

        float total = 0f, weightSum = 0f, opacityW = 0f;
        Vector3 rgb = Vector3.zero;

        foreach (var kv in amounts)
        {
            var s = profile.Get(kv.Key);
            if (s == null) continue;                       // this dish ignores that liquid
            float w = kv.Value * s.strength;
            total += kv.Value;
            weightSum += w;
            rgb += new Vector3(s.color.r, s.color.g, s.color.b) * w;
            opacityW += s.opacity * w;
        }
        if (total <= 0f || weightSum <= 0f) return Color.clear;

        Color tint = new Color(rgb.x / weightSum, rgb.y / weightSum, rgb.z / weightSum);
        float tintOpacity = opacityW / weightSum;

        // average strength per ml: lots of water lowers it, so the base shows through
        float influence = 1f - Mathf.Exp(-profile.sensitivity * (weightSum / total));

        Color result = Color.Lerp(profile.baseColor, tint, influence);
        float alpha = Mathf.Lerp(profile.baseOpacity, tintOpacity, influence);

        result = Color.Lerp(result, profile.reducedColor, reduction * 0.6f);
        result.a = Mathf.Clamp01(alpha + reduction * 0.2f) * maxAlpha;
        return result;
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