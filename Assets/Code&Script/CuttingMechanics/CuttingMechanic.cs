using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public enum CutQuality
{
    VeryGood,
    Good,
    Bad
}


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

    public event System.Action<IngredientData, IngredientPrepState> OnStateChanged;

    public event System.Action OnCuttingFinished;

    private readonly List<Image> activeHistoryCircles = new List<Image>();

    private bool isIndicatorMoving;
    private int movingDirection = 1;
    private float minX, maxX;
    private int cutCount;


    public int CutCount => cutCount;


    private bool finishedEarly;
    public bool IsFinished => cutCount >= maxCuts || finishedEarly;

    public bool IsCutIncomplete => cutCount > 0 && CurrentState != lockedFinalState;

    private IngredientPrepState lockedFinalState = IngredientPrepState.Minced;

    public event System.Action OnCuttingSessionClosed;
    void Awake()
    {
        if (tutorialInteractable == null)
            tutorialInteractable = tapToCutButton.GetComponent<TutorialInteractable>();
        tapToCutAgainButton.gameObject.SetActive(false);


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


    public void StartCuttingMinigame()
    {
        cutCount = 0;
        finishedEarly = false;
        lockedFinalState = IngredientPrepState.Minced;
        ApplyState(IngredientPrepState.Whole);
        isIndicatorMoving = true;
        tapToCutButton.gameObject.SetActive(true);
        tapToCutAgainButton.gameObject.SetActive(false);

        ClearAndInitializeHistoryUI();
    }

    public void OnTapToCutClicked()
    {
        if (IsFinished) return;
        if (!isIndicatorMoving) return;

        SFXManager.Instance?.PlayChop();


        CutQuality quality = EvaluateCutQuality();


        if (quality == CutQuality.Bad)
        {
            UpdateQualityDisplay(quality);


            if (activeHistoryCircles.Count > 0)
            {
                Image currentCircle = activeHistoryCircles[activeHistoryCircles.Count - 1];
                if (currentCircle != null)
                    currentCircle.color = ColorForQuality(quality);
            }

            bool inTutorial = TutorialManager.Instance != null && TutorialManager.Instance.IsActive;
            if (inTutorial)
            {
                tutorialInteractable?.ReportMistake();
            }
            else
            {
                ScoreManager.Instance?.ReportResult(ToResultQuality(quality));
            }
            return;
        }


        IngredientPrepState targetState = (cutCount == 0)
         ? IngredientPrepState.Sliced
         : IngredientPrepState.Minced;
        IngredientPrepState finalState = GetFinalState();
        lockedFinalState = finalState;
        if (currentIngredient != null && CookingPrepListUI.Instance != null)
        {

            IngredientPrepState stepState =
                (targetState == IngredientPrepState.Sliced && finalState == IngredientPrepState.Minced)
                    ? IngredientPrepState.Minced
                    : targetState;

            if (!CookingPrepListUI.Instance.TryAccept($"{currentIngredient.id}:{stepState}"))
                return;
        }

        isIndicatorMoving = false;
        UpdateQualityDisplay(quality);

        if (activeHistoryCircles.Count > 0)
        {
            Image currentCircle = activeHistoryCircles[activeHistoryCircles.Count - 1];
            if (currentCircle != null)
                currentCircle.color = ColorForQuality(quality);
        }

        ScoreManager.Instance?.ReportResult(ToResultQuality(quality));
        tutorialInteractable?.ReportTap();
        cutCount++;
        ApplyState(targetState);
        tapToCutButton.gameObject.SetActive(false);

        bool done = cutCount >= maxCuts;
        if (done)
        {
            tapToCutAgainButton.gameObject.SetActive(false);
            OnCuttingFinished?.Invoke();
        }
        else
        {
            tapToCutAgainButton.gameObject.SetActive(true);
        }
    }

    public void OnTapToCutAgainClicked()
    {
        if (IsFinished) return;

        if (GetFinalState() == IngredientPrepState.Sliced)
        {
            ScoreManager.Instance?.ReportMistake("Already sliced!\nMinus 1 heart");
            return;
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


        foreach (Transform child in colorSavedCutContainer)
            Destroy(child.gameObject);

        activeHistoryCircles.Clear();


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
            circleImg.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            activeHistoryCircles.Add(circleImg);
        }
        else
        {
            Debug.LogError("[CuttingMechanic] circleHistoryPrefab is missing an Image component.");
        }
    }


    public void SetIngredient(IngredientData data)
    {
        currentIngredient = data;
        ApplyState(IngredientPrepState.Whole);
    }

    private void ApplyState(IngredientPrepState state)
    {
        CurrentState = state;

        if (stateLabel != null) stateLabel.text = state.ToString();
        Debug.Log("the cut is " + CurrentState);
        if (currentIngredient != null && ingredientImage != null)
        {
            ingredientImage.sprite = currentIngredient.GetSpriteForState(state);
            ingredientImage.preserveAspect = true;
            ingredientImage.color = Color.white;
        }

        OnStateChanged?.Invoke(currentIngredient, state);
    }

    private IngredientPrepState GetFinalState()
    {
        var list = CookingPrepListUI.Instance;
        if (currentIngredient == null || list == null) return IngredientPrepState.Minced;

        bool wantsSliced = list.HasStep($"{currentIngredient.id}:{IngredientPrepState.Sliced}");
        bool wantsMinced = list.HasStep($"{currentIngredient.id}:{IngredientPrepState.Minced}");
        return (wantsSliced && !wantsMinced) ? IngredientPrepState.Sliced : IngredientPrepState.Minced;
    }


    public void OnCloseClicked()
    {
        if (IsCutIncomplete)
        {
            ScoreManager.Instance?.ReportMistake("Finish the cut first!\nMinus 1 heart");
            return;
        }
        bool completed = cutCount > 0;
        gameObject.SetActive(false);
        if (completed) OnCuttingSessionClosed?.Invoke();
    }
}
