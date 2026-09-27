using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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
///  - Only the FINAL required cut is scored (see requiredCuts) and each cut
///    can advance the target ingredient's visual prep state (see
///    targetIngredient + cutStageProgression)
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

    [Header("Multi-Stage Cutting (whole / sliced / minced)")]
    [Tooltip("How many taps this ingredient needs to reach its required prep stage. 1 = single cut, 2 = sliced, 3 = minced. Only the FINAL cut's quality is scored - earlier cuts are practice/visual only, so an ingredient needing 3 cuts doesn't cost 3x the score.")]
    public int requiredCuts = 1;

    [Tooltip("Fired once the ingredient reaches its required cut count (the final, scored cut). Wire this to whatever should happen next (e.g. close the cutting panel, move to the next prep step).")]
    public UnityEvent onCuttingComplete;

    [Header("Ingredient Visual State")]
    [Tooltip("The spawned ingredient instance actually being cut right now - its sprite updates to match cut progress. Assign this whenever the cutting panel opens for a specific ingredient (e.g. when the knife is dropped on it).")]
    public IngredientStateController targetIngredient;

    [Tooltip("Which prep state each cut reaches, in order. E.g. [Sliced, Minced] means cut #1 -> Sliced, cut #2 -> Minced. Should have at least Required Cuts entries.")]
    public List<IngredientPrepState> cutStageProgression = new List<IngredientPrepState> { IngredientPrepState.Sliced };

    private readonly List<Image> activeHistoryCircles = new List<Image>();

    private bool isIndicatorMoving;
    private int movingDirection = 1; // 1 = right, -1 = left
    private float minX, maxX;
    private int cutsPerformed = 0;

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
        cutsPerformed = 0;

        ClearAndInitializeHistoryUI();
    }

    // LINK TO "TapToCut" BUTTON
    public void OnTapToCutClicked()
    {
        isIndicatorMoving = false; // freeze indicator
        cutsPerformed++;

        CutQuality quality = EvaluateCutQuality();
        UpdateQualityDisplay(quality);

        // Color the current gray placeholder circle instead of spawning a new one
        if (activeHistoryCircles.Count > 0)
        {
            Image currentCircle = activeHistoryCircles[activeHistoryCircles.Count - 1];
            if (currentCircle != null)
                currentCircle.color = ColorForQuality(quality);
        }

        // Advance the ingredient's visual prep state for THIS cut, if a
        // stage is defined for it (cutsPerformed is 1-based, list is 0-based).
        int stageIndex = cutsPerformed - 1;
        if (targetIngredient != null && stageIndex >= 0 && stageIndex < cutStageProgression.Count)
        {
            targetIngredient.SetState(cutStageProgression[stageIndex]);
        }

        bool isFinalCut = cutsPerformed >= requiredCuts;

        if (isFinalCut)
        {
            // Only the cut that actually reaches the required prep stage
            // (whole/sliced/minced) counts toward score - earlier cuts were
            // just getting there and shouldn't be penalized/rewarded again.
            ScoreManager.Instance?.ReportResult(ToResultQuality(quality));
            onCuttingComplete?.Invoke();
        }

        tapToCutButton.gameObject.SetActive(false);
        tapToCutAgainButton.gameObject.SetActive(!isFinalCut);
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

    private static ResultQuality ToResultQuality(CutQuality quality)
    {
        return quality switch
        {
            CutQuality.VeryGood => ResultQuality.VeryGood,
            CutQuality.Good => ResultQuality.Good,
            _ => ResultQuality.Bad
        };
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