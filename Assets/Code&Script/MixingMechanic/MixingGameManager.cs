using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MixingGameManager : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [Header("Hierarchy UI References")]
    public RectTransform circulationCenterPoint; // Drag zone center (Pan center)
    public RectTransform spatulaImage;           // Spatula UI element
    public GameObject arrowForClockwise;
    public GameObject arrowForCounterClockwise;

    [Header("Text Displays")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI circulationCountText;

    [Header("Game Challenge Settings")]
    public float totalGameTime = 9f;
    public int targetCirculations = 10;
    public float directionSwitchInterval = 3f;

    [Header("Spatula Free Movement Settings")]
    [Tooltip("Maximum radius (in UI pixels) the spatula head can travel inside the pan (Red circle).")]
    public float maxPanRadius = 80f;

    [Tooltip("Fixed tilt angle for the spatula image.")]
    public float fixedSpatulaAngle = -35f;

    // State Tracking
    private float timeRemaining;
    private int completedCirculations = 0;
    private bool isClockwiseTarget = true;
    private bool isGameActive = false;

    // Angle Tracking
    private float previousTouchAngle = 0f;
    private float accumulatedAngle = 0f;
    private Canvas parentCanvas;

    void Awake()
    {
        // Cache parent canvas to handle screen-space conversions accurately
        if (circulationCenterPoint != null)
        {
            parentCanvas = circulationCenterPoint.GetComponentInParent<Canvas>();
        }
    }

    void Start()
    {
        StartMixingGame();
    }

    public void StartMixingGame()
    {
        timeRemaining = totalGameTime;
        completedCirculations = 0;
        accumulatedAngle = 0f;
        isGameActive = true;

        // Reset spatula position directly to the exact center (0,0) of circulationCenterPoint
        if (spatulaImage != null)
        {
            spatulaImage.anchoredPosition = Vector2.zero;
            spatulaImage.localRotation = Quaternion.Euler(0f, 0f, fixedSpatulaAngle);
        }

        UpdateTextDisplays();
        StopAllCoroutines();
        StartCoroutine(DirectionSwitchRoutine());
    }

    void Update()
    {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            isGameActive = false;
            CheckGameOverState();
        }

        UpdateTextDisplays();
    }

    private IEnumerator DirectionSwitchRoutine()
    {
        while (isGameActive)
        {
            isClockwiseTarget = Random.value > 0.5f;

            if (arrowForClockwise != null) arrowForClockwise.SetActive(isClockwiseTarget);
            if (arrowForCounterClockwise != null) arrowForCounterClockwise.SetActive(!isClockwiseTarget);

            yield return new WaitForSeconds(directionSwitchInterval);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isGameActive) return;

        // Correctly initialize touch angle relative to center point in screen space
        previousTouchAngle = GetAngleFromScreenPoint(eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isGameActive) return;

        Camera uiCamera = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? eventData.pressEventCamera
            : null;

        // 1. Convert screen touch point into local position relative to CirculationCenterPoint
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            circulationCenterPoint,
            eventData.position,
            uiCamera,
            out Vector2 localDragPos))
        {
            // 2. Clamp position strictly inside maxPanRadius
            Vector2 clampedPos = Vector2.ClampMagnitude(localDragPos, maxPanRadius);
            spatulaImage.anchoredPosition = clampedPos;

            // Maintain fixed visual rotation angle
            spatulaImage.localRotation = Quaternion.Euler(0f, 0f, fixedSpatulaAngle);

            // 3. Ignore deadzone near exact center to avoid angle noise
            if (clampedPos.magnitude < 10f) return;

            // 4. Calculate current rotation angle around center
            float currentTouchAngle = Mathf.Atan2(clampedPos.y, clampedPos.x) * Mathf.Rad2Deg;
            float angleDelta = Mathf.DeltaAngle(previousTouchAngle, currentTouchAngle);

            // Standard UI Polar Coordinates: Moving Clockwise produces negative delta
            bool isMovingClockwise = angleDelta < 0f;

            // Filter out accidental micro-movements
            if (Mathf.Abs(angleDelta) > 0.1f)
            {
                if (isClockwiseTarget == isMovingClockwise)
                {
                    accumulatedAngle += Mathf.Abs(angleDelta);

                    // Reached 360 degrees
                    if (accumulatedAngle >= 360f)
                    {
                        completedCirculations++;
                        accumulatedAngle -= 360f;
                        UpdateTextDisplays(); // Instantly update UI text score

                        if (completedCirculations >= targetCirculations)
                        {
                            isGameActive = false;
                            CheckGameOverState();
                        }
                    }
                }
                else
                {
                    // Gentle deduction for wrong direction
                    accumulatedAngle = Mathf.Max(0f, accumulatedAngle - (Mathf.Abs(angleDelta) * 0.3f));
                }

                previousTouchAngle = currentTouchAngle;
            }
        }
    }

    private float GetAngleFromScreenPoint(Vector2 screenPosition, Camera cam)
    {
        Camera uiCamera = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? cam : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            circulationCenterPoint,
            screenPosition,
            uiCamera,
            out Vector2 localPoint
        );

        return Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
    }

    private void UpdateTextDisplays()
    {
        if (timerText != null) timerText.text = $"Time: {timeRemaining:F1}s";
        if (circulationCountText != null) circulationCountText.text = $"Stirs: {completedCirculations}/{targetCirculations}";
    }

    private void CheckGameOverState()
    {
        StopAllCoroutines();

        if (completedCirculations >= targetCirculations)
        {
            Debug.Log("<color=green>[MIXING] VICTORY!</color>");
        }
        else
        {
            Debug.Log("<color=red>[MIXING] FAILED!</color>");
        }
    }
}