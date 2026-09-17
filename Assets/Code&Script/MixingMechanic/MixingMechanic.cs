using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Stirring mini-game:
///  - Player holds the Spatula and drags it in an arc around a fixed pivot (RotationZone)
///  - Angular speed of the drag is measured each frame
///  - The Indicator has "velocity": it drifts toward Red(top) if stirring too fast,
///    drifts toward Red(bottom) if too slow, and stays near center (Green) if the
///    speed is close to ideal.
///  - A timer bar fills over a fixed duration; when full, mixing ends and whatever
///    zone the indicator is sitting in becomes the score (VeryGood / Good / Bad).
/// </summary>
public class MixingMechanic : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum MixResult { VeryGood, Good, Bad }

    [Header("Pivot & Spatula")]
    [Tooltip("The fixed point the spatula rotates around (e.g. RotationZone)")]
    public RectTransform pivot;
    [Tooltip("The spatula's RectTransform - its pivot should be at the handle/grip point")]
    public RectTransform spatula;
    [Tooltip("Assign only if Canvas Render Mode is Screen Space - Camera. Leave null for Overlay.")]
    public Camera uiCamera;

    [Header("Optional: restrict the swing to an arc instead of a full 360")]
    public bool clampAngle = false;
    public float minAngle = -70f;
    public float maxAngle = 70f;

    [Tooltip("If greater than 0, the spatula orbits at this fixed distance from the pivot instead of whatever distance it was placed at in the editor. Lower this to shrink the circle it traces.")]
    public float orbitRadiusOverride = -1f;

    [Header("Speed Zones (degrees / second)")]
    [Tooltip("The stirring speed that counts as perfect")]
    public float idealSpeed = 200f;
    [Tooltip("+/- range around idealSpeed that still counts as Very Good (Green)")]
    public float greenTolerance = 40f;
    [Tooltip("+/- range around idealSpeed that counts as Good (Yellow). Outside this = Bad (Red)")]
    public float yellowTolerance = 90f;
    [Tooltip("How quickly a raw per-frame speed reading is smoothed (0-1, higher = snappier)")]
    [Range(0.05f, 1f)] public float speedSmoothing = 0.3f;
    [Tooltip("How fast the measured speed decays toward 0 when the player isn't dragging (deg/sec^2)")]
    public float idleDecay = 300f;

    [Header("Indicator")]
    public RectTransform indicator;
    public RectTransform meterTrack; // parent gauge, defines the top/bottom travel bounds
    [Tooltip("How fast the indicator drifts per second at maximum deviation from ideal speed")]
    public float driftSpeed = 40f;
    [Tooltip("Fraction of half-height that counts as the Green zone (0-1)")]
    [Range(0f, 1f)] public float greenZoneFraction = 0.2f;
    [Tooltip("Fraction of half-height that counts as the Yellow zone (0-1), outside this is Red")]
    [Range(0f, 1f)] public float yellowZoneFraction = 0.55f;

    [Header("Timer")]
    [Tooltip("Image with Image Type = Filled representing the mixing time limit")]
    public Image timerFillImage;
    [Tooltip("Alternative to Timer Fill Image: a non-interactive Slider representing time progress")]
    public Slider timerSlider;
    public float mixDuration = 8f;

    [Header("UI Feedback")]
    public TMP_Text resultLabel;
    public TMP_Text speedDebugLabel; // optional, handy while tuning idealSpeed/tolerances

    private bool isDragging = false;
    private bool mixingActive = false;
    private bool hasStarted = false;
    private float orbitRadius; // distance from pivot the spatula orbits at, captured on grab
    private float lastAngle;
    private float currentAngularSpeed;
    private float indicatorY;
    private float trackHalfHeight;
    private float elapsedTime;
    private float lastDragTime;
    private Image spatulaImage;
    private Color spatulaOriginalColor;

    void Start()
    {
        trackHalfHeight = meterTrack.rect.height / 2f;

        if (spatula != null)
        {
            spatulaImage = spatula.GetComponent<Image>();
            if (spatulaImage != null)
                spatulaOriginalColor = spatulaImage.color;
        }
    }

    /// <summary>Call this when the recipe step begins (e.g. when the player enters the mixing panel).</summary>
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

        if (!mixingActive) return; // e.g. a previous round already finished

        isDragging = true;
        lastAngle = GetAngleFromPivot(eventData);
        lastDragTime = Time.time;

        // Lock in the orbit radius: use the manual override if set, otherwise whatever
        // distance the spatula was placed at in the editor.
        if (spatula != null)
            orbitRadius = orbitRadiusOverride > 0f ? orbitRadiusOverride : spatula.anchoredPosition.magnitude;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!mixingActive || !isDragging) return;

        float angle = GetAngleFromPivot(eventData);
        float delta = Mathf.DeltaAngle(lastAngle, angle); // signed shortest-path delta, handles wraparound
        lastAngle = angle;

        float dt = Mathf.Max(Time.time - lastDragTime, 0.0001f); // real time since last reading, not assumed frame time
        lastDragTime = Time.time;
        float instSpeed = Mathf.Abs(delta) / dt; // deg/sec
        currentAngularSpeed = Mathf.Lerp(currentAngularSpeed, instSpeed, speedSmoothing);

        if (spatula != null)
        {
            float positionAngle = angle;
            if (clampAngle)
                positionAngle = Mathf.Clamp(NormalizeAngle(angle), minAngle, maxAngle);

            float rad = positionAngle * Mathf.Deg2Rad;
            // Orbit the spatula's position around the pivot at a fixed radius -
            // its rotation is intentionally left untouched so it doesn't spin, only circles.
            spatula.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    private float GetAngleFromPivot(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            pivot, eventData.position, uiCamera, out Vector2 localPoint);
        return Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
    }

    private float NormalizeAngle(float angle)
    {
        // keeps angle in a continuous -180..180 range for clamping purposes
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private void UpdateIndicator()
    {
        float targetVelocity = 0f;

        // If the player is actively dragging and moving the spatula in an orbit
        if (isDragging && currentAngularSpeed > 10f)
        {
            // Active orbiting pushes the indicator UP (positive velocity)
            // You can scale it by speed if you want faster stirring to fill it faster
            targetVelocity = driftSpeed;
        }
        else
        {
            // When the player stops stirring or lets go, the indicator drifts back down
            targetVelocity = -driftSpeed * 0.8f;
        }

        indicatorY = Mathf.Clamp(indicatorY + targetVelocity * Time.deltaTime, -trackHalfHeight, trackHalfHeight);

        Vector2 pos = indicator.anchoredPosition;
        pos.y = indicatorY;
        indicator.anchoredPosition = pos;
    }

    private void UpdateTimerVisual()
    {
        float progress = Mathf.Clamp01(elapsedTime / mixDuration);

        if (timerFillImage != null)
            timerFillImage.fillAmount = progress;

        if (timerSlider != null)
            timerSlider.value = progress;
    }

    /// <summary>Call this when a new recipe/round begins, so the next grab starts a fresh timer.</summary>
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
        isDragging = false; // force-stop any in-progress drag the instant time runs out

        float fraction = Mathf.Abs(indicatorY) / trackHalfHeight; // 0 = center, 1 = edge

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

        // Lock the spatula: stop it receiving pointer events at all, and dim it so
        // it visibly reads as "done" rather than just silently unresponsive.
        if (spatulaImage != null)
        {
            spatulaImage.raycastTarget = false;
            spatulaImage.color = new Color(0.6f, 0.6f, 0.6f, spatulaOriginalColor.a);
        }

        Debug.Log($"Mixing finished. Indicator landed at {fraction:0.00} of track -> {result}");
    }
}