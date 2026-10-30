using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanLiquidFill : MonoBehaviour
{
    [Header("Gradual Pour")]
    [SerializeField] private bool animateFillAmount = true;

    private Coroutine pourRoutine;
    private bool gradualBegun;
    private float pouringTotal, pouringAdded;
  

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
        StopEvaporate();
        if(pourRoutine != null) { StopCoroutine(pourRoutine); pourRoutine = null; }
        gradualBegun = false;
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
    private Coroutine evaporateRoutine;

    /// <summary>Slowly removes up to 'ml' of liquid 'id' over 'seconds'.</summary>
    public void StartEvaporate(string id, float ml, float seconds)
    {
        if (blob == null || !amounts.ContainsKey(id)) return;
        StopEvaporate();
        evaporateRoutine = StartCoroutine(EvaporateRoutine(id, ml, seconds));
    }

    public void StopEvaporate()
    {
        if (evaporateRoutine != null) StopCoroutine(evaporateRoutine);
        evaporateRoutine = null;
    }

    private IEnumerator EvaporateRoutine(string id, float ml, float seconds)
    {
        // Stop any blend animation so we control the visuals directly
        if (routine != null) { StopCoroutine(routine); routine = null; }

        float rate = ml / Mathf.Max(0.01f, seconds);
        float removed = 0f;

        while (removed < ml && amounts.TryGetValue(id, out float current) && current > 0f)
        {
            float step = Mathf.Min(rate * Time.deltaTime, ml - removed, current);
            removed += step;

            float left = current - step;
            if (left <= 0.01f) amounts.Remove(id);
            else amounts[id] = left;

            // Apply directly each frame
            blob.color = GetMix();
            blob.transform.localScale = baseScale * GetTargetScale();

            yield return null;
        }
        evaporateRoutine = null;
    }

    public void BeginGradualAdd(string id, float ml, float delay, float duration)
    {
        if (blob == null) return;
        if (pourRoutine != null) StopCoroutine(pourRoutine);
        StopEvaporate();
        gradualBegun = true;
        pouringTotal = Mathf.Max(ml, 0.1f);
        pouringAdded = 0f;
        pourRoutine = StartCoroutine(GradualAddRoutine(id, pouringTotal, delay, Mathf.Max(0.05f, duration)));
    }
    public void FinishGradualAdd(string id, float ml)
    {
        if (blob == null) return;

        if (!gradualBegun)
        {
            AddLiquidById(id, ml);      // fallback if the gradual fill never started
            return;
        }

        if (pourRoutine != null)
        {
            StopCoroutine(pourRoutine);
            pourRoutine = null;

            float remaining = Mathf.Max(0f, pouringTotal - pouringAdded);
            if (remaining > 0f)
            {
                amounts.TryGetValue(id, out float old);
                amounts[id] = old + remaining;
            }
            ApplyInstant(1f);
        }
        gradualBegun = false;
    }

    private void ApplyInstant(float grow)
    {
        blob.color = GetMix();
        Color c = blob.color; c.a *= grow; blob.color = c;   // fade in
        blob.transform.localScale = Vector3.Lerp(baseScale * 0.5f, baseScale * GetTargetScale(), grow);
        if (animateFillAmount) blob.fillAmount = grow;
    }

    private IEnumerator GradualAddRoutine(string id, float ml, float delay, float duration)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if(routine != null) { StopCoroutine(routine); routine = null; }

        bool first = blob.color.a < 0.01f;   // pan was empty before this pour
        float rate = ml / duration;

        while (pouringAdded < ml)
        {
            float step = Mathf.Min(rate * Time.deltaTime, ml - pouringAdded);
            pouringAdded += step;

            amounts.TryGetValue(id, out float old);
            amounts[id] = old + step;

            float grow = first ? Mathf.Clamp01(pouringAdded / ml) : 1f;
            ApplyInstant(grow);
            yield return null;
        }

        gradualBegun = false;
        pourRoutine = null;
    }
}