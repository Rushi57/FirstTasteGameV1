using System.Collections.Generic;
using TMPro;
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
///    which zone it landed in, reporting it straight to ScoreManager
///  - Each cut colors the current "history" circle; tapping "Tap To Cut
///    Again" adds a fresh gray placeholder circle and restarts the bounce
///  - Capped at maxCuts (2): Slice = 1st cut, Minced = 2nd cut. After the
///    2nd cut lands, "Tap To Cut Again" stays inactive - the mini-game is done.
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

    [Header("Cut Limit")]
    [Tooltip("How many cuts this mini-game allows before locking out further cuts. 1st cut = Sliced, 2nd cut = Minced.")]
    public int maxCuts = 2;

    [Header("Ingredient Display")]
    [Tooltip("Drag ChoppingBoard/IngredientImage here")]
    public Image ingredientImage;
    [Tooltip("Optional: shows 'Whole' / 'Sliced' / 'Minced'")]
    public TMP_Text stateLabel;

    [Header("Tutorial")]
    public TutorialInteractable tutorialInteractable;

    private IngredientData currentIngredient;
    public IngredientPrepState CurrentState { get; private set; } = IngredientPrepState.Whole;

    /// <summary>Raised whenever the ingredient changes prep state (Whole -> Sliced -> Minced).</summary>
    public event System.Action<IngredientData, IngredientPrepState> OnStateChanged;

    /// <summary>Raised once the player has used up all their cuts (cutCount reaches maxCuts).</summary>
    public event System.Action OnCuttingFinished;

    private readonly List<Image> activeHistoryCircles = new List<Image>();

    private bool isIndicatorMoving;
    private int movingDirection = 1; // 1 = right, -1 = left
    private float minX, maxX;
    private int cutCount;

    /// <summary>How many cuts have landed so far this session (0, 1, or maxCuts).</summary>
    public int CutCount => cutCount;

    /// <summary>True once cutCount has reached maxCuts and no more cuts are allowed.</summary>
    public bool IsFinished => cutCount >= maxCuts;

    void Awake()
    {
        if(tutorialInteractable ==  null)
            tutorialInteractable =tapToCutButton.GetComponent<TutorialInteractable>();
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
        cutCount = 0;
        ApplyState(IngredientPrepState.Whole);
        isIndicatorMoving = true;
        tapToCutButton.gameObject.SetActive(true);
        tapToCutAgainButton.gameObject.SetActive(false);

        ClearAndInitializeHistoryUI();
    }

    // LINK TO "TapToCut" BUTTON
    // LINK TO "TapToCut" BUTTON
    public void OnTapToCutClicked()
    {
        if (IsFinished) return;
        if (!isIndicatorMoving) return;

        // 1. Grade the cut FIRST, while the indicator is still where the player tapped
        CutQuality quality = EvaluateCutQuality();

        // 2. RED: record it, but do NOT change the state. The indicator keeps bouncing.
        if (quality == CutQuality.Bad)
        {
            UpdateQualityDisplay(quality);

            // Color the current circle red, then add a fresh gray one for the retry
            if (activeHistoryCircles.Count > 0)
            {
                Image currentCircle = activeHistoryCircles[activeHistoryCircles.Count - 1];
                if (currentCircle != null)
                    currentCircle.color = ColorForQuality(quality);
            }

            bool inTutorial = TutorialManager.Instance != null && TutorialManager.Instance.IsActive;
            if (inTutorial)
                tutorialInteractable?.ReportMistake();
            else
            //  SpawnNewPlaceholderCircle();

            ScoreManager.Instance?.ReportResult(ToResultQuality(quality)); // -15 pts, -1 heart
            return; // cutCount, ApplyState, and buttons stay untouched
        }

        // 3. GREEN / YELLOW: check the recipe step is valid
        IngredientPrepState targetState = (cutCount == 0)
            ? IngredientPrepState.Sliced
            : IngredientPrepState.Minced;

        if (currentIngredient != null && CookingPrepListUI.Instance != null)
        {
            string id = $"{currentIngredient.id}:{targetState}";
            if (!CookingPrepListUI.Instance.TryAccept(id))
                return; // wrong ingredient/order: popup + heart loss already handled
        }

        // 4. Accept the cut
        isIndicatorMoving = false;
        UpdateQualityDisplay(quality);

        if (activeHistoryCircles.Count > 0)
        {
            Image currentCircle = activeHistoryCircles[activeHistoryCircles.Count - 1];
            if (currentCircle != null)
                currentCircle.color = ColorForQuality(quality);
        }

        ScoreManager.Instance?.ReportResult(ToResultQuality(quality)); // green: 0, yellow: -5 pts
        tutorialInteractable?.ReportTap();
        cutCount++;
        ApplyState(targetState);
        tapToCutButton.gameObject.SetActive(false);

        if (cutCount >= maxCuts)
        {
            tapToCutAgainButton.gameObject.SetActive(false);
            OnCuttingFinished?.Invoke();
        }
        else
        {
            tapToCutAgainButton.gameObject.SetActive(true);
        }
    }

    // LINK TO "TapToCutAgain" BUTTON
    public void OnTapToCutAgainClicked()
    {
        if (IsFinished) return; // safety net - button should already be inactive

        // NEW: the next cut would produce Minced, so ask the prep list first
        if (currentIngredient != null && CookingPrepListUI.Instance != null)
        {
            string id = $"{currentIngredient.id}:{IngredientPrepState.Minced}";
            if (!CookingPrepListUI.Instance.TryAccept(id))
            {
                // Popup + heart loss already happened.
                // No new circle, indicator stays frozen, state is unchanged.
                return;
            }
        }

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

    /// <summary>Call this when the knife is dropped on an ingredient.</summary>
    public void SetIngredient(IngredientData data)
    {
        currentIngredient = data;
        ApplyState(IngredientPrepState.Whole);
    }

    private void ApplyState(IngredientPrepState state)
    {
        CurrentState = state;

        if (stateLabel != null) stateLabel.text = state.ToString();
        Debug.Log("the cut is "+ CurrentState);
        if (currentIngredient != null && ingredientImage != null)
        {
            ingredientImage.sprite = currentIngredient.GetSpriteForState(state);
            ingredientImage.preserveAspect = true;
            ingredientImage.color = Color.white;
        }

        OnStateChanged?.Invoke(currentIngredient, state);
    }
}