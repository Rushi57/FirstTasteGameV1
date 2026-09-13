using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimmerAndBoilManager : MonoBehaviour
{
    [Header("UI Image References")]
    public Image redImage;
    public Image yellowImage;
    public Image greenImage;
    public RectTransform handArrow;
    public Button stopButton;

    [Header("Hand Movement Settings")]
    public float handRotationSpeed = 200f; // Speed of needle spinning
    public bool isClockwise = true;

    [Header("Difficulty / Slice Ranges")]
    [Range(0.1f, 0.4f)] public float minYellowFill = 0.2f; // 20% of circle
    [Range(0.2f, 0.5f)] public float maxYellowFill = 0.35f;

    [Range(0.05f, 0.15f)] public float minGreenFill = 0.08f; // 8% of circle (sweet spot)
    [Range(0.1f, 0.25f)] public float maxGreenFill = 0.15f;

    // Internal State
    private bool isSpinning = false;
    private float currentHandAngle = 0f;

    // Angle Boundaries (0° to 360°)
    private float yellowStartAngle, yellowEndAngle;
    private float greenStartAngle, greenEndAngle;

    void Start()
    {
        if (stopButton != null)
        {
            stopButton.onClick.AddListener(OnStopButtonPressed);
        }

        StartSimmerAndBoilGame();
    }

    public void StartSimmerAndBoilGame()
    {
        RandomizeColorZones();

        currentHandAngle = Random.Range(0f, 360f);
        handArrow.localRotation = Quaternion.Euler(0f, 0f, currentHandAngle);

        isSpinning = true;
        if (stopButton != null) stopButton.interactable = true;
    }

    void Update()
    {
        if (!isSpinning) return;

        // Spin the needle continuously
        float direction = isClockwise ? -1f : 1f;
        currentHandAngle += direction * handRotationSpeed * Time.deltaTime;

        // Keep angle constrained between 0 and 360
        currentHandAngle = (currentHandAngle % 360f + 360f) % 360f;

        handArrow.localRotation = Quaternion.Euler(0f, 0f, currentHandAngle);
    }

    public void RandomizeColorZones()
    {
        // 1. Pick a random starting angle for the Yellow zone (0° to 360°)
        yellowStartAngle = Random.Range(0f, 360f);
        float yellowFillAmount = Random.Range(minYellowFill, maxYellowFill);

        yellowImage.fillAmount = yellowFillAmount;
        yellowImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -yellowStartAngle);

        float yellowArcDegrees = yellowFillAmount * 360f;
        yellowEndAngle = (yellowStartAngle + yellowArcDegrees) % 360f;

        // 2. Place Green zone INSIDE the Yellow zone
        float greenFillAmount = Random.Range(minGreenFill, Mathf.Min(maxGreenFill, yellowFillAmount * 0.6f));
        float greenArcDegrees = greenFillAmount * 360f;

        // Offset green start so it sits safely inside the yellow zone bounds
        float maxGreenOffset = yellowArcDegrees - greenArcDegrees;
        float greenOffsetFromYellowStart = Random.Range(5f, Mathf.Max(6f, maxGreenOffset));

        greenStartAngle = (yellowStartAngle + greenOffsetFromYellowStart) % 360f;
        greenEndAngle = (greenStartAngle + greenArcDegrees) % 360f;

        greenImage.fillAmount = greenFillAmount;
        greenImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -greenStartAngle);
    }

    public void OnStopButtonPressed()
    {
        if (!isSpinning) return;

        isSpinning = false;
        if (stopButton != null) stopButton.interactable = false;

        // The UI rotation origin starts at Top (90° in standard polar space)
        // Convert needle Z-rotation to UI Radial Fill angle (0° = Top, clockwise)
        float needleFillAngle = (360f - currentHandAngle) % 360f;

        EvaluateResult(needleFillAngle);
    }

    private void EvaluateResult(float hitAngle)
    {
        // Check if hit landed inside Green Zone
        if (IsAngleInsideSector(hitAngle, greenStartAngle, greenImage.fillAmount * 360f))
        {
            Debug.Log("<color=green>[SIMMER & BOIL] PERFECT! (Very Good)</color>");
        }
        // Check if hit landed inside Yellow Zone
        else if (IsAngleInsideSector(hitAngle, yellowStartAngle, yellowImage.fillAmount * 360f))
        {
            Debug.Log("<color=yellow>[SIMMER & BOIL] GOOD! (Pass)</color>");
        }
        // Landed on Red default background
        else
        {
            Debug.Log("<color=red>[SIMMER & BOIL] BAD! (Overcooked/Undercooked)</color>");
        }
    }

    private bool IsAngleInsideSector(float testAngle, float sectorStart, float sectorArc)
    {
        float sectorEnd = (sectorStart + sectorArc) % 360f;

        if (sectorStart <= sectorEnd)
        {
            return testAngle >= sectorStart && testAngle <= sectorEnd;
        }
        else
        {
            // Handles wrap-around across 0° / 360° boundary
            return testAngle >= sectorStart || testAngle <= sectorEnd;
        }
    }
}