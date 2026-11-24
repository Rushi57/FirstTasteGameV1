using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class MixingSeasoningController : MonoBehaviour
{
    [Header("Panel")]
    public GameObject mixingPanel;
    public Button closeButton;

    [Header("Mechanic")]
    public MixingMechanic mixingMechanic;

    [Header("Speed presets (degrees/second)")]
    public float slowSpeed = 90f;
    public float defaultSpeed = 150f;
    public float fastSpeed = 220f;

    [Header("Recipe check")]
    public CookingPrepPanelController cookingPrepPanel;
    [Tooltip("Result must be at least this good to count as success (VeryGood or Good).")]
    public MixingMechanic.MixResult minimumSuccess = MixingMechanic.MixResult.Good;

    private readonly HashSet<int> completedLines = new HashSet<int>();
    private int pendingLineIndex = -1;
    private bool challengeActive;

    [Header("Direction")]
    public bool randomizeDirection = true;
    private MixingMechanic.MixDirection lastDirection = MixingMechanic.MixDirection.Any;

    [Header("Direction Arrow")]
    public Image arrowChanger;
    public Sprite clockwiseSprite;
    public Sprite counterClockwiseSprite;


    private MixingMechanic.MixDirection configuredDirection;
    private float configuredSpeed;

    private void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (mixingMechanic != null)
        {
            mixingMechanic.OnMixFinished += HandleMixFinished;
            mixingMechanic.OnMixStarted += HandleMixStarted;
        }
    }

    private void OnDestroy()
    {
        if (mixingMechanic != null)
        {
            mixingMechanic.OnMixFinished -= HandleMixFinished;
            mixingMechanic.OnMixStarted -= HandleMixStarted;
        }
    }


    public void OpenAndConfigure()
    {
        RecipeData recipe = cookingPrepPanel != null ? cookingPrepPanel.currentRecipe : null;
        if (recipe == null)
        {
            Debug.LogWarning("[Mixing] No recipe available (assign Cooking Prep Panel).");
            return;
        }

        if (!TryFindNextStep(recipe, out int lineIndex, out MixingMechanic.MixDirection direction, out float speed))
        {
            Debug.Log("[Mixing] Recipe has no (remaining) saute/stir step - nothing to do.");
            return;
        }

        pendingLineIndex = lineIndex;
        challengeActive = true;

        mixingPanel.SetActive(true);
        SetCloseButtonVisible(true);   // hidden automatically in tutorial while challengeActive
        mixingMechanic.ResetForNewRound();
        mixingMechanic.ConfigureChallenge(direction, speed);
        configuredDirection = mixingMechanic.requiredDirection;
        configuredSpeed = speed;
        UpdateArrow(configuredDirection);
        Debug.Log($"[Mixing] Opened for line {lineIndex}: '{recipe.cookingInstructions[lineIndex]}' -> direction={direction}, idealSpeed={speed}");
    }

    private static bool InTutorial =>
        TutorialManager.Instance != null && TutorialManager.Instance.IsActive;


    private void RestartRound()
    {
        challengeActive = true;          // must be set BEFORE the button call
        SetCloseButtonVisible(true);     // stays hidden in tutorial
        mixingMechanic.ResetForNewRound();
        mixingMechanic.ConfigureChallenge(configuredDirection, configuredSpeed);
        UpdateArrow(configuredDirection);
    }

    private void HandleMixFinished(MixingMechanic.MixResult result)
    {
        if (!challengeActive)
        {
            SetCloseButtonVisible(true);
            return;
        }

        bool success = result <= minimumSuccess;
        Debug.Log($"[Mixing] Result: {result} -> {(success ? "PASS" : "FAIL")}");

        ResultQuality quality = result switch
        {
            MixingMechanic.MixResult.VeryGood => ResultQuality.VeryGood,
            MixingMechanic.MixResult.Good => ResultQuality.Good,
            _ => ResultQuality.Bad
        };

        bool inTutorial = InTutorial;

        if (!inTutorial)
            ScoreManager.Instance?.ReportResult(quality);

        if (success && pendingLineIndex >= 0)
        {
            completedLines.Add(pendingLineIndex);
            CookingPrepListUI.Instance?.CompleteStep($"Mix:{pendingLineIndex}");
        }

        challengeActive = false;         // BEFORE showing the button
        SetCloseButtonVisible(true);     // timer finished -> close button shows

        if (inTutorial && !success)
            RestartRound();              // hides the button again for the retry
    }

    public void Close()
    {
        if (mixingMechanic != null && mixingMechanic.IsMixing) return;

        // Tutorial: ignore the tap until the challenge is passed
        if (InTutorial && challengeActive) return;

        mixingPanel.SetActive(false);
        pendingLineIndex = -1;
    }

    private bool TryFindNextStep(RecipeData recipe, out int lineIndex, out MixingMechanic.MixDirection direction, out float speed)
    {
        for (int i = 0; i < recipe.cookingInstructions.Count; i++)
        {
            if (completedLines.Contains(i)) continue;

            string line = recipe.cookingInstructions[i];
            if (string.IsNullOrEmpty(line)) continue;

            string lower = line.ToLowerInvariant();
            bool isMixLine = lower.Contains("saut") || lower.Contains("stir") || lower.Contains("mix");
            if (!isMixLine) continue;

            direction = lower.Contains("counter") || lower.Contains("anti-clockwise") || lower.Contains("anticlockwise")
                ? MixingMechanic.MixDirection.CounterClockwise
                : lower.Contains("clockwise")
                    ? MixingMechanic.MixDirection.Clockwise
                    : MixingMechanic.MixDirection.Any;

            speed = lower.Contains("slow") ? slowSpeed
                  : (lower.Contains("fast") || lower.Contains("quick")) ? fastSpeed
                  : defaultSpeed;

            lineIndex = i;
            return true;
        }

        lineIndex = -1;
        direction = MixingMechanic.MixDirection.Any;
        speed = defaultSpeed;
        return false;
    }

    public void ResetProgress()
    {
        completedLines.Clear();
        pendingLineIndex = -1;
        challengeActive = false;         // before the button call
        SetCloseButtonVisible(true);
        mixingPanel.SetActive(false);
        mixingMechanic?.ResetForNewRound();
        lastDirection = MixingMechanic.MixDirection.Any;
    }

    private void UpdateArrow(MixingMechanic.MixDirection dir)
    {
        if (arrowChanger == null) return;

        switch (dir)
        {
            case MixingMechanic.MixDirection.Clockwise:
                arrowChanger.sprite = clockwiseSprite;
                arrowChanger.enabled = true;
                break;
            case MixingMechanic.MixDirection.CounterClockwise:
                arrowChanger.sprite = counterClockwiseSprite;
                arrowChanger.enabled = true;
                break;
            default:
                arrowChanger.enabled = false;
                break;
        }
    }


    private MixingMechanic.MixDirection PickDirection()
    {

        if (lastDirection == MixingMechanic.MixDirection.Any)
        {
            lastDirection = UnityEngine.Random.value < 0.5f
                ? MixingMechanic.MixDirection.Clockwise
                : MixingMechanic.MixDirection.CounterClockwise;
        }
        else
        {
            lastDirection = lastDirection == MixingMechanic.MixDirection.Clockwise
                ? MixingMechanic.MixDirection.CounterClockwise
                : MixingMechanic.MixDirection.Clockwise;
        }
        return lastDirection;
    }

    private void SetCloseButtonVisible(bool visible)
    {
        if (closeButton == null) return;

        // Tutorial: keep it hidden while the challenge is still pending
        if (visible && InTutorial && challengeActive) visible = false;

        closeButton.gameObject.SetActive(visible);
    }

    private void HandleMixStarted()
    {
        SetCloseButtonVisible(false);
    }
}