using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// Put this on the PourButton (it needs a raycast-able Image).
/// The target amount now comes from the measurement the player picked (in ml).
public class PourMechanic : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum PourResult { Perfect, Good, Overflow, TooLittle }

    [Header("Zone Tolerance (as % of target, 0.1 = 10%)")]
    [Range(0f, 1f)] public float greenZoneWidth = 0.10f;
    [Range(0f, 1f)] public float yellowZoneWidth = 0.25f;
    [Tooltip("Top of the meter as a multiplier of the target (max overflow)")]
    public float meterTopMultiplier = 1.6f;

    [Header("Pouring")]
    [Tooltip("Seconds of holding needed to reach 100% of the target, so big and small measures feel the same")]
    public float secondsToFillTarget = 3f;

    [Header("References")]
    public ParticleSystem pourParticles;
    public RectTransform indicator;      // the black bar on the meter
    public RectTransform meterTrack;     // MeterImage
    public Image spoonFillImage;         // optional
    public TMP_Text amountLabel;         // optional
    public TMP_Text resultLabel;         // optional

    /// Fired when the player lets go. (result, poured ml, target ml)
    public event Action<PourResult, float, float> OnPourFinished;

    private float targetMl;
    private float currentMl;
    private string measureLabel = "";
    private bool isPouring;
    private bool canPour;

    void Start()
    {
        UpdateVisuals();
    }

    /// Called by SeasoningPourController when the player taps a measurement button.
    public void SetTarget(float ml, string label)
    {
        targetMl = ml;
        measureLabel = label;
        canPour = ml > 0f;
        ResetPour();
    }

    public void ResetPour()
    {
        isPouring = false;
        currentMl = 0f;
        if (pourParticles != null) pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (resultLabel != null) resultLabel.text = "";
        UpdateVisuals();
    }

    void Update()
    {
        if (!isPouring || !canPour) return;

        float ratePerSecond = targetMl / Mathf.Max(0.1f, secondsToFillTarget);
        currentMl += ratePerSecond * Time.deltaTime;
        currentMl = Mathf.Clamp(currentMl, 0f, targetMl * meterTopMultiplier);
        UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!canPour) return;
        isPouring = true;
        if (pourParticles != null) pourParticles.Play();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!canPour || !isPouring) return;
        isPouring = false;
        if (pourParticles != null) pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        EvaluateResult();
    }

    private void UpdateVisuals()
    {
        if (meterTrack == null || indicator == null) return;

        float percent = targetMl > 0f ? currentMl / targetMl : 0f;
        float t = Mathf.InverseLerp(0f, meterTopMultiplier, percent);

        // Works with any pivot: use the track's rect edges
        float half = meterTrack.rect.height / 2f;
        Vector2 pos = indicator.anchoredPosition;
        pos.y = Mathf.Lerp(-half, half, t);
        indicator.anchoredPosition = pos;

        if (spoonFillImage != null) spoonFillImage.fillAmount = Mathf.Clamp01(percent);

        if (amountLabel != null)
            amountLabel.text = $"{currentMl:0.0} / {targetMl:0.0} ml  ({measureLabel})";
    }

    private void EvaluateResult()
    {
        float percent = currentMl / targetMl;
        PourResult result;

        if (percent >= 1f - greenZoneWidth && percent <= 1f + greenZoneWidth) result = PourResult.Perfect;
        else if (percent > 1f + greenZoneWidth && percent <= 1f + yellowZoneWidth) result = PourResult.Good;
        else if (percent > 1f + yellowZoneWidth) result = PourResult.Overflow;
        else result = PourResult.TooLittle;

        if (resultLabel != null)
        {
            switch (result)
            {
                case PourResult.Perfect: resultLabel.text = "Perfect!"; break;
                case PourResult.Good: resultLabel.text = "Good!"; break;
                case PourResult.Overflow: resultLabel.text = "Too much! Overflow"; break;
                case PourResult.TooLittle: resultLabel.text = "Not enough!"; break;
            }
        }

        Debug.Log($"Pour result: {result} ({currentMl:0.00} ml poured, target {targetMl} ml)");
        OnPourFinished?.Invoke(result, currentMl, targetMl);
    }
}