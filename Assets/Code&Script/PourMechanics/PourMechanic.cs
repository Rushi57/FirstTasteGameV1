using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Put this on PourButton. Hold to pour, release to grade.
/// The target amount is set by PourSeasoningController from the
/// measurement the player picked.
/// </summary>
public class PourMechanic : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum PourResult { Perfect, Good, Overflow, TooLittle }

    [Header("Zone Tolerance (as % of target, 0.10 = 10%)")]
    [Range(0f, 1f)] public float greenZoneWidth = 0.10f;
    [Range(0f, 1f)] public float yellowZoneWidth = 0.25f;
    [Tooltip("Multiplier of target that represents the very top of the meter (max overflow)")]
    public float meterTopMultiplyer = 1.6f;

    [Header("Pouring")]
    [Tooltip("Holding the button this many seconds fills exactly to the target.")]
    public float secondsToReachTarget = 4f;

    [Header("References")]
    public ParticleSystem pourParticles;
    public RectTransform indicator;
    public RectTransform meterTrack;
    public Image spoonFillImage;
    public TMP_Text amountLabel;
    public TMP_Text resultLabel;

    [Header("Meter zones (children of MeterImage; RedZone stays as the full background)")]
    public RectTransform greenZone;
    public RectTransform yellowZone;
    [Tooltip("Smallest half-height of a zone as a fraction of the meter, so tiny targets stay hittable.")]
    [Range(0f, 0.1f)] public float minZoneHalfHeight = 0.02f;


    private float meterMaxMl;   // biggest measurement of the chosen tool
    private float TopMl => (meterMaxMl > 0f ? meterMaxMl : targetMl) * meterTopMultiplyer;
    private float GreenHalfMl => Mathf.Max(targetMl * greenZoneWidth, TopMl * minZoneHalfHeight);
    private float YellowHalfMl => Mathf.Max(targetMl * yellowZoneWidth, TopMl * minZoneHalfHeight);
    /// <summary>Raised on release: meter result and the amount poured in ml.</summary>
    public event Action<PourResult, float> OnPourFinished;

    private float targetMl;
    private float currentMl;
    private bool isPouring;
    private bool hasResult;
    private float trackHalfHeight;

    void Start()
    {
        if (meterTrack != null) trackHalfHeight = meterTrack.rect.height / 2f;
        UpdateVisuals();
    }

    /// <summary>Called when the player picks a measurement.</summary>
    public void SetTarget(float ml)
    {
        targetMl = ml;
        ResetPour();
        LayoutZones();
        Debug.Log($"[PourMeter] Target set to {ml:0.##} ml");
    }

    public void ClearTarget()
    {
        targetMl = 0f;
        ResetPour();
        LayoutZones();
    }

    void Update()
    {
        if (!isPouring || targetMl <= 0f) return;

        currentMl += (targetMl / Mathf.Max(0.1f, secondsToReachTarget)) * Time.deltaTime;
        currentMl = Mathf.Clamp(currentMl, 0f, TopMl);
        UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetMl <= 0f)
        {
            Debug.Log("[PourMeter] Pick a measurement first.");
            return;
        }

        if (hasResult) ResetPour();   // a new attempt starts from empty

        isPouring = true;
        if (pourParticles != null) pourParticles.Play();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPouring) return;

        isPouring = false;
        if (pourParticles != null) pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        EvaluateResult();
    }

    private void UpdateVisuals()
    {
        float percent = targetMl > 0f ? currentMl / targetMl : 0f;
        float t = TopMl > 0f ? Mathf.Clamp01(currentMl / TopMl) : 0f;

        if (indicator != null)
        {
            Vector2 pos = indicator.anchoredPosition;
            pos.y = Mathf.Lerp(-trackHalfHeight, trackHalfHeight, t);
            indicator.anchoredPosition = pos;
        }

        if (spoonFillImage != null)
            spoonFillImage.fillAmount = Mathf.Clamp01(percent);

        if (amountLabel != null)
            amountLabel.text = targetMl > 0f ? $"{currentMl:0.#} / {targetMl:0.#} ml" : "";
    }

    private void EvaluateResult()
    {
        float diff = Mathf.Abs(currentMl - targetMl);

        PourResult result;
        if (diff <= GreenHalfMl)
            result = PourResult.Perfect;
        else if (diff <= YellowHalfMl)
            result = PourResult.Good;
        else if (currentMl > targetMl)
            result = PourResult.Overflow;
        else
            result = PourResult.TooLittle;

        hasResult = true;

        if (resultLabel != null)
        {
            resultLabel.text = result switch
            {
                PourResult.Perfect => "Perfect!",
                PourResult.Good => "Good!",
                PourResult.Overflow => "Too much! Overflow",
                _ => "Not enough!"
            };
        }

        Debug.Log($"[PourMeter] {result} ({currentMl:0.##} ml poured, target {targetMl:0.##} ml)");
        OnPourFinished?.Invoke(result, currentMl);
    }

    public void ResetPour()
    {
        currentMl = 0f;
        isPouring = false;
        hasResult = false;
        if (resultLabel != null) resultLabel.text = "";
        if (meterTrack != null) trackHalfHeight = meterTrack.rect.height / 2f;
        UpdateVisuals();
    }
    public void SetParticleColor(Color color)
    {
        if (pourParticles == null) return;
        var main = pourParticles.main;
        main.startColor = color;
    }
    public void SetMeterMax(float maxMl)
    {
        meterMaxMl = maxMl;
        LayoutZones();
        UpdateVisuals();
    }

    private void LayoutZones()
    {
        bool has = targetMl > 0f;
        if (greenZone != null) greenZone.gameObject.SetActive(has);
        if (yellowZone != null) yellowZone.gameObject.SetActive(has);
        if (!has) return;

        float top = TopMl;
        SetBand(yellowZone, (targetMl - YellowHalfMl) / top, (targetMl + YellowHalfMl) / top);
        SetBand(greenZone, (targetMl - GreenHalfMl) / top, (targetMl + GreenHalfMl) / top);
    }

    private void SetBand(RectTransform zone, float lo, float hi)
    {
        if (zone == null) return;
        zone.anchorMin = new Vector2(0f, Mathf.Clamp01(lo));
        zone.anchorMax = new Vector2(1f, Mathf.Clamp01(hi));
        zone.offsetMin = new Vector2(zone.offsetMin.x, 0f);
        zone.offsetMax = new Vector2(zone.offsetMax.x, 0f);
    }
}