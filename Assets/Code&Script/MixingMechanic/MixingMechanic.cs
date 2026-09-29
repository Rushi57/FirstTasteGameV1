using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MixingMechanic : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum MixResult { VeryGood, Good, Bad }
    public enum MixDirection { Any, Clockwise, CounterClockwise }

    [Header("Pivot & Spatula")]
    public RectTransform pivot;
    public RectTransform spatula;
    public Camera uiCamera;

    [Header("Optional: restrict the swing to an arc instead of a full 360")]
    public bool clampAngle = false;
    public float minAngle = -70f;
    public float maxAngle = 70f;
    public float orbitRadiusOverride = -1f;

    [Header("Smoothing")]
    public float rotationSmoothTime = 0.06f;

    [Header("Speed Zones (degrees / second)")]
    [Tooltip("Set per-round by ConfigureChallenge - the speed the player needs to hit.")]
    public float idealSpeed = 200f;
    public float greenTolerance = 40f;
    public float yellowTolerance = 90f;
    [Range(0.05f, 1f)] public float speedSmoothing = 0.3f;
    public float idleDecay = 300f;

    [Header("Direction Requirement")]
    [Tooltip("Set per-round by ConfigureChallenge.")]
    public MixDirection requiredDirection = MixDirection.Any;
    [Tooltip("Flip if Clockwise/CounterClockwise feel reversed for your canvas setup.")]
    public bool invertDirection = false;
    [Tooltip("Minimum smoothed direction confidence (0-1) before a direction is considered 'locked in'.")]
    [Range(0f, 1f)] public float directionConfidence = 0.3f;

    [Header("Pivot Randomization")]
    [Tooltip("Preset spots (RectTransforms) the pivot can jump to each round. Leave empty to use Pivot Random Radius instead.")]
    public RectTransform[] pivotSpots;
    [Tooltip("If no pivot spots are assigned, the pivot moves to a random point within this radius of its starting position.")]
    public float pivotRandomRadius = 0f;

    [Header("Indicator")]
    public RectTransform indicator;
    public RectTransform meterTrack;
    public float driftSpeed = 40f;
    [Range(0f, 1f)] public float greenZoneFraction = 0.2f;
    [Range(0f, 1f)] public float yellowZoneFraction = 0.55f;

    [Header("Timer")]
    public Image timerFillImage;
    public Slider timerSlider;
    public float mixDuration = 8f;

    [Header("UI Feedback")]
    public TMP_Text resultLabel;
    public TMP_Text speedDebugLabel;

    /// <summary>Raised once the timer runs out and a result has been decided.</summary>
    public event Action<MixResult> OnMixFinished;

    private bool isDragging = false;
    private bool mixingActive = false;
    private bool hasStarted = false;
    private float orbitRadius;
    private float lastAngle;
    private float currentAngularSpeed;
    private float indicatorY;
    private float trackHalfHeight;
    private float elapsedTime;
    private float lastDragTime;
    private Image spatulaImage;
    private Color spatulaOriginalColor;
    private Vector2 pivotHomePosition;
    private float directionSign; // smoothed -1..1 (raw, before invertDirection is applied)

    private float targetAngle;
    private float visualAngle;
    private float angleVelocity;

    void Start()
    {
        trackHalfHeight = meterTrack.rect.height / 2f;

        if (pivot != null) pivotHomePosition = pivot.anchoredPosition;

        if (spatula != null)
        {
            spatulaImage = spatula.GetComponent<Image>();
            if (spatulaImage != null)
                spatulaOriginalColor = spatulaImage.color;
        }
    }

    /// <summary>Call before the player grabs the spatula: sets this round's required direction/speed and moves the pivot.</summary>
    public void ConfigureChallenge(MixDirection direction, float targetIdealSpeed)
    {
        requiredDirection = direction;
        idealSpeed = targetIdealSpeed;
        RandomizePivot();
        Debug.Log($"[Mixing] Challenge configured: direction={direction}, idealSpeed={targetIdealSpeed}");
    }

    /// <summary>Moves the pivot (and therefore the spatula, which orbits it) to a new spot.</summary>
    public void RandomizePivot()
    {
        if (pivot == null) return;

        if (pivotSpots != null && pivotSpots.Length > 0)
        {
            var chosen = pivotSpots[UnityEngine.Random.Range(0, pivotSpots.Length)];
            pivot.anchoredPosition = chosen.anchoredPosition;
        }
        else if (pivotRandomRadius > 0f)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * pivotRandomRadius;
            pivot.anchoredPosition = pivotHomePosition + offset;
        }

        Debug.Log($"[Mixing] Pivot moved to {pivot.anchoredPosition}");
    }

    public void BeginMixing()
    {
        mixingActive = true;
        elapsedTime = 0f;
        indicatorY = 0f;
        currentAngularSpeed = 0f;
        if (resultLabel != null) resultLabel.text = "";
        UpdateTimerVisual();
    }

    void Update()
    {
        if (!mixingActive) return;

        if (!isDragging)
            currentAngularSpeed = Mathf.MoveTowards(currentAngularSpeed, 0f, idleDecay * Time.deltaTime);

        UpdateIndicator();
        UpdateSpatulaVisual();

        elapsedTime += Time.deltaTime;
        UpdateTimerVisual();

        if (speedDebugLabel != null)
            speedDebugLabel.text = $"{currentAngularSpeed:0} deg/s";

        if (elapsedTime >= mixDuration)
        {
            mixingActive = false;
            EvaluateResult();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!hasStarted)
        {
            hasStarted = true;
            BeginMixing();
        }

        if (!mixingActive) return;

        isDragging = true;
        lastAngle = GetAngleFromPivot(eventData);
        lastDragTime = Time.time;
        directionSign = 0f; // fresh read each grab

        if (spatula != null)
            orbitRadius = orbitRadiusOverride > 0f ? orbitRadiusOverride : spatula.anchoredPosition.magnitude;

        targetAngle = clampAngle ? Mathf.Clamp(NormalizeAngle(lastAngle), minAngle, maxAngle) : lastAngle;
        visualAngle = targetAngle;
        angleVelocity = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!mixingActive || !isDragging) return;

        float angle = GetAngleFromPivot(eventData);
        float delta = Mathf.DeltaAngle(lastAngle, angle);
        lastAngle = angle;

        float dt = Mathf.Max(Time.time - lastDragTime, 0.0001f);
        lastDragTime = Time.time;
        float instSpeed = Mathf.Abs(delta) / dt;
        currentAngularSpeed = Mathf.Lerp(currentAngularSpeed, instSpeed, speedSmoothing);

        // Track rotation direction only on meaningfully fast movement, to avoid noise
        if (instSpeed > 15f)
        {
            float deltaSign = Mathf.Sign(delta); // positive = counter-clockwise (standard math convention)
            directionSign = Mathf.Lerp(directionSign, deltaSign, 0.4f);
        }

        float positionAngle = angle;
        if (clampAngle)
            positionAngle = Mathf.Clamp(NormalizeAngle(angle), minAngle, maxAngle);
        targetAngle = positionAngle;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void UpdateSpatulaVisual()
    {
        if (spatula == null) return;

        visualAngle = rotationSmoothTime > 0f
            ? Mathf.SmoothDampAngle(visualAngle, targetAngle, ref angleVelocity, rotationSmoothTime)
            : targetAngle;

        float rad = visualAngle * Mathf.Deg2Rad;
        spatula.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
    }

    private float GetAngleFromPivot(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            pivot, eventData.position, uiCamera, out Vector2 localPoint);
        return Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private bool IsDirectionOk()
    {
        if (requiredDirection == MixDirection.Any) return true;

        float sign = invertDirection ? -directionSign : directionSign;

        if (requiredDirection == MixDirection.Clockwise) return sign < -directionConfidence;
        return sign > directionConfidence; // CounterClockwise
    }

    private void UpdateIndicator()
    {
        float targetVelocity;

        if (isDragging && currentAngularSpeed > 10f)
        {
            if (!IsDirectionOk())
            {
                // Wrong direction - push toward the bad end regardless of speed
                targetVelocity = driftSpeed;
            }
            else
            {
                float speedError = currentAngularSpeed - idealSpeed; // + too fast, - too slow
                float absErr = Mathf.Abs(speedError);

                if (absErr <= greenTolerance)
                {
                    // Correct speed - ease back toward center
                    targetVelocity = Mathf.Abs(indicatorY) < 1f ? 0f : -Mathf.Sign(indicatorY) * driftSpeed;
                }
                else
                {
                    // Push toward the edge matching the direction of the speed error
                    targetVelocity = Mathf.Sign(speedError) * driftSpeed;
                }
            }
        }
        else
        {
            targetVelocity = Mathf.Abs(indicatorY) < 1f ? 0f : -Mathf.Sign(indicatorY) * driftSpeed * 0.8f;
        }

        indicatorY = Mathf.Clamp(indicatorY + targetVelocity * Time.deltaTime, -trackHalfHeight, trackHalfHeight);

        Vector2 pos = indicator.anchoredPosition;
        pos.y = indicatorY;
        indicator.anchoredPosition = pos;
    }

    private void UpdateTimerVisual()
    {
        float progress = Mathf.Clamp01(elapsedTime / mixDuration);
        if (timerFillImage != null) timerFillImage.fillAmount = progress;
        if (timerSlider != null) timerSlider.value = progress;
    }

    public void ResetForNewRound()
    {
        hasStarted = false;
        mixingActive = false;
        isDragging = false;

        if (spatulaImage != null)
        {
            spatulaImage.raycastTarget = true;
            spatulaImage.color = spatulaOriginalColor;
        }
    }

    private void EvaluateResult()
    {
        isDragging = false;

        float fraction = Mathf.Abs(indicatorY) / trackHalfHeight;

        MixResult result;
        if (fraction <= greenZoneFraction) result = MixResult.VeryGood;
        else if (fraction <= yellowZoneFraction) result = MixResult.Good;
        else result = MixResult.Bad;

        if (resultLabel != null)
        {
            resultLabel.text = result switch
            {
                MixResult.VeryGood => "Very Good!",
                MixResult.Good => "Good",
                _ => "Bad"
            };
        }

        if (spatulaImage != null)
        {
            spatulaImage.raycastTarget = false;
            spatulaImage.color = new Color(0.6f, 0.6f, 0.6f, spatulaOriginalColor.a);
        }

        Debug.Log($"[Mixing] Finished. Indicator at {fraction:0.00} of track -> {result}");
        OnMixFinished?.Invoke(result);
    }
}