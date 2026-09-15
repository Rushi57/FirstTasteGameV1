using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>How well-timed a single cut was.</summary>
public enum CutQuality
{
    VeryGood,
    Good,
    Bad
}

/// <summary>
/// Standalone cutting/chopping mini-game, extracted from KitchenManager:
///  - indicatorArrow bounces left/right within a Red/Yellow/Green meter
///  - Tapping "Tap To Cut" freezes it and records a CutQuality based on
///    which zone it landed in
///  - Each cut colors the current "history" circle; tapping "Tap To Cut
///    Again" adds a fresh gray placeholder circle and restarts the bounce
/// </summary>
public class CuttingMechanic : MonoBehaviour
{
    [Header("Meter & Indicator")]
    [Tooltip("The bar/arrow that bounces left and right")]
    public RectTransform indicatorArrow;
    [Tooltip("Used only to measure the full travel width of the meter")]
    public RectTransform redZone;
    public RectTransform yellowZone;
    public RectTransform greenZone;
    public float indicatorSpeed = 300f;

    [Header("Buttons")]
    public Button tapToCutButton;
    public Button tapToCutAgainButton;

    [Header("Result Display")]
    [Tooltip("Optional: shows the color of the most recent cut prominently, separate from the history row")]
    public Image colorSavedCutDisplay;

    [Header("Cut History Row")]
    [Tooltip("Parent that lays out one circle per cut performed so far")]
    public Transform colorSavedCutContainer;
    [Tooltip("Prefab for each history circle - needs an Image component")]
    public GameObject circleHistoryPrefab;

    private readonly List<Image> activeHistoryCircles = new List<Image>();

    private bool isIndicatorMoving;
    private int movingDirection = 1; // 1 = right, -1 = left
    private float minX, maxX;

    void Awake()
    {
        tapToCutAgainButton.gameObject.SetActive(false);

        // Travel bounds based on the meter's full background width
        float trackWidth = redZone.rect.width;
        minX = -trackWidth / 2f;
        maxX = trackWidth / 2f;

        tapToCutButton.onClick.AddListener(OnTapToCutClicked);
        tapToCutAgainButton.onClick.AddListener(OnTapToCutAgainClicked);
    }

    void OnEnable()
    {
        StartCuttingMinigame();
    }

    void Update()
    {
        if (!isIndicatorMoving) return;

        Vector2 pos = indicatorArrow.anchoredPosition;
        pos.x += indicatorSpeed * movingDirection * Time.deltaTime;

        if (pos.x >= maxX) { pos.x = maxX; movingDirection = -1; }
        if (pos.x <= minX) { pos.x = minX; movingDirection = 1; }

        indicatorArrow.anchoredPosition = pos;
    }

    /// <summary>Call this when the cutting panel opens (e.g. knife dropped on an ingredient).</summary>
    public void StartCuttingMinigame()
    {
        isIndicatorMoving = true;
        tapToCutButton.gameObject.SetActive(true);
        tapToCutAgainButton.gameObject.SetActive(false);

        ClearAndInitializeHistoryUI();
    }

    // LINK TO "TapToCut" BUTTON
    public void OnTapToCutClicked()
    {
        isIndicatorMoving = false; // freeze indicator

        CutQuality quality = EvaluateCutQuality();
        UpdateQualityDisplay(quality);

        // Color the current gray placeholder circle instead of spawning a new one
        if (activeHistoryCircles.Count > 0)
        {
            Image currentCircle = activeHistoryCircles[activeHistoryCircles.Count - 1];
            if (currentCircle != null)
                currentCircle.color = ColorForQuality(quality);
        }

        tapToCutButton.gameObject.SetActive(false);
        tapToCutAgainButton.gameObject.SetActive(true);
    }

    // LINK TO "TapToCutAgain" BUTTON
    public void OnTapToCutAgainClicked()
    {
        SpawnNewPlaceholderCircle();

        isIndicatorMoving = true;
        tapToCutButton.gameObject.SetActive(true);
        tapToCutAgainButton.gameObject.SetActive(false);
    }

    private CutQuality EvaluateCutQuality()
    {
        float currentX = Mathf.Abs(indicatorArrow.anchoredPosition.x);

        float greenBound = greenZone.rect.width / 2f;
        float yellowBound = yellowZone.rect.width / 2f;

        if (currentX <= greenBound) return CutQuality.VeryGood;
        if (currentX <= yellowBound) return CutQuality.Good;
        return CutQuality.Bad;
    }

    private Color ColorForQuality(CutQuality quality)
    {
        switch (quality)
        {
            case CutQuality.VeryGood: return Color.green;
            case CutQuality.Good: return Color.yellow;
            default: return Color.red;
        }
    }

    private void UpdateQualityDisplay(CutQuality quality)
    {
        if (colorSavedCutDisplay != null)
            colorSavedCutDisplay.color = ColorForQuality(quality);
    }

    private void ClearAndInitializeHistoryUI()
    {
        if (colorSavedCutContainer == null) return;

        // Clear out old leftover runtime clones
        foreach (Transform child in colorSavedCutContainer)
            Destroy(child.gameObject);

        activeHistoryCircles.Clear();

        // Load exactly one gray placeholder to start the first cut
        SpawnNewPlaceholderCircle();
    }

    private void SpawnNewPlaceholderCircle()
    {
        if (circleHistoryPrefab == null || colorSavedCutContainer == null)
        {
            Debug.LogWarning("[CuttingMechanic] Missing circleHistoryPrefab or colorSavedCutContainer reference.");
            return;
        }

        GameObject newCircle = Instantiate(circleHistoryPrefab, colorSavedCutContainer);
        newCircle.SetActive(true);

        Image circleImg = newCircle.GetComponent<Image>();
        if (circleImg != null)
        {
            circleImg.enabled = true;
            circleImg.color = new Color(0.5f, 0.5f, 0.5f, 1f); // neutral gray until this cut lands
            activeHistoryCircles.Add(circleImg);
        }
        else
        {
            Debug.LogError("[CuttingMechanic] circleHistoryPrefab is missing an Image component.");
        }
    }
}