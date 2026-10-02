using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public enum DirectionMode { UseConfigured, Alternate, Random }

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

    [Tooltip("UseConfigured = ConfigureChallenge decides. Alternate = flips CW/CCW every round. Random = random CW/CCW every round.")]
    public DirectionMode directionMode = DirectionMode.Alternate;
    public MixDirection firstDirection = MixDirection.Clockwise;

    private MixDirection lastPickedDirection;
    private bool hasPickedDirection = false;

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
    public TMP_Text directionLabel;


    [Header("Debug")]
    public bool debugIndicator = true;
    private float nextDebugTime;

    [Header("Tutorial(Optioanal)")]
    [Tooltip("If assigned, the tutorial is told when the mix timer fills up.")]
    public TutorialInteractable tutorialInteractable;

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
    private float directionSign; // smoothed -1..1 (raw, before invertDirection is applied)

    private float targetAngle;
    private float visualAngle;
    private float angleVelocity;

    void Start()
    {
        trackHalfHeight = meterTrack.rect.height / 2f;

        

        if (spatula != null)
        {
            spatulaImage = spatula.GetComponent<Image>();
            if (spatulaImage != null)
                spatulaOriginalColor = spatulaImage.color;
        }
        PickNextDirection();
    }

    /// <summary>Call before the player grabs the spatula: sets this round's required direction/speed and moves the pivot.</summary>
    public void ConfigureChallenge(MixDirection direction, float targetIdealSpeed)
    {
        requiredDirection = direction;
        idealSpeed = targetIdealSpeed;
        UpdateDirectionLabel();

        Debug.Log($"[Mixing] Challenge configured: direction={direction}, idealSpeed={targetIdealSpeed}");
    }

    /// <summary>Moves the pivot (and therefore the spatula, which orbits it) to a new spot.</summary>
   

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
        bool stalled = isDragging && (Time.time - lastDragTime) > 0.08f;

        if (!isDragging || stalled)
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

        if (instSpeed > 15f)
            directionSign = Mathf.Lerp(directionSign, Mathf.Sign(delta), 0.4f);

        targetAngle = clampAngle ? Mathf.Clamp(NormalizeAngle(angle), minAngle, maxAngle) : angle;
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
                // Wrong direction - push up toward the bad end
                targetVelocity = driftSpeed;
            }
            else
            {
                float speedError = currentAngularSpeed - idealSpeed; // + too fast, - too slow

                if (Mathf.Abs(speedError) <= greenTolerance)
                {
                    // Correct speed - ease back toward center
                    targetVelocity = Mathf.Abs(indicatorY) < 1f ? 0f : -Mathf.Sign(indicatorY) * driftSpeed;
                }
                else
                {
                    // Too fast pushes up, too slow pushes down
                    targetVelocity = Mathf.Sign(speedError) * driftSpeed;
                }
            }
        }
        else
        {
            // Not rotating = too slow, drift to the bottom
            targetVelocity = -driftSpeed;
        }

        indicatorY = Mathf.Clamp(indicatorY + targetVelocity * Time.deltaTime, -trackHalfHeight, trackHalfHeight);

        Vector2 pos = indicator.anchoredPosition;
        pos.y = indicatorY;
        indicator.anchoredPosition = pos;

        if (debugIndicator && Time.time >= nextDebugTime)
        {
            nextDebugTime = Time.time + 0.25f;

            string state = targetVelocity > 0f ? "RISING"
                         : targetVelocity < 0f ? "DRIFTING DOWN"
                         : "HOLDING";

            Debug.Log($"[Indicator] {state} | y={indicatorY:0.0} / -{trackHalfHeight:0.0}..{trackHalfHeight:0.0} | vel={targetVelocity:0.0} | speed={currentAngularSpeed:0} | dragging={isDragging}");
        }
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
        PickNextDirection();
    }

    private void PickNextDirection()
    {
        if (directionMode == DirectionMode.UseConfigured) return;

        MixDirection next;
        if(directionMode == DirectionMode.Random)
        {
            next = UnityEngine.Random.value < 0.5f
                ? MixDirection.Clockwise
                :MixDirection.CounterClockwise;
        }
        else
        {
            if (!hasPickedDirection)
                next = firstDirection;
            else
                next = lastPickedDirection == MixDirection.Clockwise
                    ? MixDirection.CounterClockwise
                    : MixDirection.CounterClockwise;
        }
        lastPickedDirection = next;
        hasPickedDirection = true;
        requiredDirection = next;
        UpdateDirectionLabel();
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
        tutorialInteractable?.ReportTimerComplete();
    }

    private void UpdateDirectionLabel()
    {
        if (directionLabel == null) return;

        directionLabel.text = requiredDirection switch
        {
            MixDirection.Clockwise => "Rotate Clockwise",
            MixDirection.CounterClockwise => "Rotate Counter-Clockwise",
            _=>"Rotate in Any Direction"
        };
    }

    
}