using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Salt/pepper seasoning mini-game:
///  - Each tap on the shaker adds one fixed "shake" (a small fraction of a teaspoon)
///  - The indicator moves up the meter in discrete steps, one per tap - no continuous flow
///  - Tapping "Done" locks in the current amount and scores it against the target
///  - Handles fractional targets (like 1/2 tsp) naturally, since taps are discrete units
/// </summary>
public class SaltPepperShakeMechanic : MonoBehaviour
{
    public enum SeasonResult { Perfect, Good, Bad }
    public enum MeasurementUnit { USTeaspoon, MetricTeaspoon }

    [Header("Recipe Target")]
    [Tooltip("How many teaspoons the recipe calls for, e.g. 0.5 for half a teaspoon")]
    public float targetTeaspoons = 0.5f;
    public MeasurementUnit unit = MeasurementUnit.USTeaspoon;
    [Tooltip("How much one tap adds, in teaspoons - smaller = finer control, more taps needed")]
    public float tapIncrementTsp = 0.125f; // 1/8 tsp per shake

    [Header("Tolerance (in number of taps away from the exact target)")]
    [Tooltip("Within this many taps of the target still counts as Perfect")]
    public int perfectTapTolerance = 0;
    [Tooltip("Within this many taps of the target still counts as Good. Beyond this is Bad")]
    public int goodTapTolerance = 1;
    [Tooltip("Taps beyond target + this many auto-locks the result as Bad (oversalted)")]
    public int overflowTapBuffer = 3;

    [Header("References")]
    public Button shakeButton;
    public Button doneButton;
    public RectTransform shakerImage;    // the salt/pepper shaker sprite - gets a little "punch" per tap
    public ParticleSystem shakeParticles; // optional: small grain burst per tap
    public RectTransform indicator;
    public RectTransform meterTrack;
    public TMP_Text amountLabel;
    public TMP_Text resultLabel;

    [Header("Shake Animation")]
    public float punchScale = 1.15f;
    public float punchDuration = 0.12f;

    private int currentTaps;
    private bool isLocked;
    private float trackHalfHeight;
    private Vector3 shakerOriginalScale;

    private int TargetTaps => Mathf.Max(1, Mathf.RoundToInt(targetTeaspoons / tapIncrementTsp));
    private int MaxTaps => TargetTaps + overflowTapBuffer;

    private const float ML_PER_US_TSP = 4.9289f;
    private const float ML_PER_METRIC_TSP = 5f;

    void Awake()
    {
        trackHalfHeight = meterTrack.rect.height / 2f;
        if (shakerImage != null)
            shakerOriginalScale = shakerImage.localScale;

        shakeButton.onClick.AddListener(OnShakeTapped);
        doneButton.onClick.AddListener(OnDoneTapped);
    }

    void OnEnable()
    {
        ResetSeasoning();
    }

    public void ResetSeasoning()
    {
        currentTaps = 0;
        isLocked = false;

        if (shakeButton != null)
        {
            shakeButton.gameObject.SetActive(true);
            shakeButton.image.raycastTarget = true;
        }
        if (resultLabel != null) resultLabel.text = "";

        UpdateVisuals();
    }

    private void OnShakeTapped()
    {
        if (isLocked) return;

        currentTaps++;
        currentTaps = Mathf.Min(currentTaps, MaxTaps);

        if (shakeParticles != null) shakeParticles.Emit(6);
        if (shakerImage != null) StartCoroutine(PunchShaker());

        UpdateVisuals();

        // Auto-lock if they've badly oversalted past the overflow buffer
        if (currentTaps >= MaxTaps)
            LockAndEvaluate();
    }

    private void OnDoneTapped()
    {
        if (isLocked) return;
        LockAndEvaluate();
    }

    private void LockAndEvaluate()
    {
        isLocked = true;

        SeasonResult result = EvaluateResult();

        if (resultLabel != null)
        {
            resultLabel.text = result switch
            {
                SeasonResult.Perfect => "Perfectly Seasoned!",
                SeasonResult.Good => "Good",
                _ => "Too Much Salt!"
            };
        }

        if (shakeButton != null)
            shakeButton.image.raycastTarget = false; // stop accepting more taps

        Debug.Log($"Seasoning finished. {currentTaps}/{TargetTaps} taps -> {result}");
    }

    private SeasonResult EvaluateResult()
    {
        int diff = Mathf.Abs(currentTaps - TargetTaps);

        if (diff <= perfectTapTolerance) return SeasonResult.Perfect;
        if (diff <= goodTapTolerance) return SeasonResult.Good;
        return SeasonResult.Bad;
    }

    private void UpdateVisuals()
    {
        // Move the indicator in a discrete step per tap, not a smooth continuous slide
        float maxPercent = MaxTaps / (float)TargetTaps;
        float currentPercent = currentTaps / (float)TargetTaps;
        float t = Mathf.InverseLerp(0f, maxPercent, currentPercent);

        float yPos = Mathf.Lerp(-trackHalfHeight, trackHalfHeight, t);
        Vector2 pos = indicator.anchoredPosition;
        pos.y = yPos;
        indicator.anchoredPosition = pos;

        if (amountLabel != null)
        {
            float currentTsp = currentTaps * tapIncrementTsp;
            string unitLabel = unit == MeasurementUnit.USTeaspoon ? "tsp (US)" : "tsp (Metric)";
            amountLabel.text = $"{currentTsp:0.##} / {targetTeaspoons:0.##} {unitLabel}  ({currentTaps} shakes)";
        }
    }

    private IEnumerator PunchShaker()
    {
        float elapsed = 0f;
        Vector3 targetScale = shakerOriginalScale * punchScale;

        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float half = punchDuration / 2f;
            float scaleT = elapsed < half
                ? elapsed / half
                : 1f - (elapsed - half) / half;
            shakerImage.localScale = Vector3.Lerp(shakerOriginalScale, targetScale, scaleT);
            yield return null;
        }

        shakerImage.localScale = shakerOriginalScale;
    }

    // Utility: convert teaspoons of the selected unit to milliliters, if needed elsewhere
    public float ToMilliliters(float teaspoons)
    {
        return unit == MeasurementUnit.USTeaspoon
            ? teaspoons * ML_PER_US_TSP
            : teaspoons * ML_PER_METRIC_TSP;
    }
}