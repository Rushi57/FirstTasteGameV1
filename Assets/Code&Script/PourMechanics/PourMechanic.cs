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
        Debug.Log($"[PourMeter] Target set to {ml:0.##} ml");
    }

    public void ClearTarget()
    {
        targetMl = 0f;
        ResetPour();
    }

    void Update()
    {
        if (!isPouring || targetMl <= 0f) return;

        currentMl += (targetMl / Mathf.Max(0.1f, secondsToReachTarget)) * Time.deltaTime;
        currentMl = Mathf.Clamp(currentMl, 0f, targetMl * meterTopMultiplyer);
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
        float t = Mathf.InverseLerp(0f, meterTopMultiplyer, percent);

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
        float percent = currentMl / targetMl;

        PourResult result;
        if (Mathf.Abs(percent - 1f) <= greenZoneWidth)
            result = PourResult.Perfect;
        else if (Mathf.Abs(percent - 1f) <= yellowZoneWidth)
            result = PourResult.Good;
        else if (percent > 1f)
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
}