using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Unity.VisualScripting;
using System.Data;

public class PourMechanic : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum MeasurementUnit { USTablespoon, MetricTablespoon}
    public enum PourResult { Perfect, Good ,Overflow, TooLittle}

    [Header("Recipe Settings")]
    [Tooltip("How many tablespoon the recipe calls for")]
    public float targetTablespoons = 2f;
    public MeasurementUnit unit = MeasurementUnit.USTablespoon;

    [Header("Zone Tolerance (as % target, 1.0 = 100%")]
    [Range(0f, 1f)] public float greenZoneWidth = 0.10f;
    [Range(0f, 1f)] public float yellowZoneWidth = 0.25f;
    [Tooltip("Multiplier of target that represents the very top of the meter (max overflow)")]

    public float meterTopMultiplyer = 1.6f;

    [Header("Pouring")]
    [Tooltip("Tablespoons poured per second while holding the button")]
    public float pourRatePerSecond = 0.6f;

    [Header("References")]
    public ParticleSystem pourParticles;
    public RectTransform indicator;
    public RectTransform meterTrack;
    public Image spoonFillImage;

    public TMP_Text amountLabel;
    public TMP_Text resultLabel;

    private float currentTablespoons = 0f;
    private bool isPouring = false;
    private float trackHalfHeight;

    private const float ML_PER_US_TBSP = 14.7868f;
    private const float ML_PER_METRIC_TBSP = 15f;

    void Start()
    {
        trackHalfHeight = meterTrack.rect.height / 2f;
        UpdateVisuals();
    }

    void Update()
    {
        if (!isPouring) return;

        currentTablespoons += pourRatePerSecond * Time.deltaTime;

        float maxAmount = targetTablespoons * meterTopMultiplyer;
        currentTablespoons = Mathf.Clamp(currentTablespoons, 0f, maxAmount);

        UpdateVisuals();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        isPouring = true;
        if (pourParticles != null) pourParticles.Play();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPouring=false;
        if (pourParticles != null) pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        EvaluateResult();
    }

    private void UpdateVisuals()
    {
        float percentOfTarget = currentTablespoons / targetTablespoons;

        float maxPercent = meterTopMultiplyer;
        float t = Mathf.InverseLerp(0f, maxPercent, percentOfTarget);

        float yPos = Mathf.Lerp(-trackHalfHeight, trackHalfHeight, t);

        Vector2 pos = indicator.anchoredPosition;
        pos.y = yPos;
        indicator.anchoredPosition = pos;

        if(spoonFillImage != null)
            spoonFillImage.fillAmount = Mathf.Clamp01(percentOfTarget);

        if(amountLabel != null)
        {
            string unitLabel = unit == MeasurementUnit.USTablespoon ? "tbsp (US)" : "tbsp (Metric)";
            amountLabel.text = $"{currentTablespoons:0.0} / {targetTablespoons:0.0} {unitLabel}";
        }
    }

    private void EvaluateResult()
    {
        float percentOfTarget = currentTablespoons / targetTablespoons;
        float lowerGreen = 1f - greenZoneWidth;
        float upperGreen = 1f + greenZoneWidth;
        float lowerYellow = 1f - yellowZoneWidth;
        float upperYellow = 1f + yellowZoneWidth;

        PourResult result;

        if (percentOfTarget >= lowerGreen && percentOfTarget <= upperGreen)
            result = PourResult.Perfect;
        else if (percentOfTarget > upperGreen && percentOfTarget <= upperYellow)
            result = PourResult.Good;
        else if (percentOfTarget > upperYellow)
            result = PourResult.Overflow;
        else
            result = PourResult.TooLittle;


        if(resultLabel != null)
        {
            switch(result)
            {
                case PourResult.Perfect: resultLabel.text = "Perfect!"; break;
                case PourResult.Good: resultLabel.text = "Good!"; break;
                case PourResult.Overflow: resultLabel.text = "To much! Overflow"; break;
                case PourResult.TooLittle: resultLabel.text = "Not Enough!"; break;
            }
        }
        Debug.Log($"Pour result: {result} ({currentTablespoons:0.00} tbsp poured, target {targetTablespoons} tbsp)");
    }

    public float ToMilliliters(float tablespoons)
    {
        return unit == MeasurementUnit.USTablespoon
            ? tablespoons * ML_PER_US_TBSP
            : tablespoons * ML_PER_METRIC_TBSP;
    }

    // Call this to reset the meter (e.g., when a new recipe step starts)
    public void ResetPour()
    {
        currentTablespoons = 0f;
        UpdateVisuals();
        if (resultLabel != null) resultLabel.text = "";
    }
}
