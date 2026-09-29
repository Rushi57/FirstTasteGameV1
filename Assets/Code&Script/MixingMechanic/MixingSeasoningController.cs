using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Opens MixingPanel when the spatula is dropped on the heated pan, reads
/// the next un-done "sauté/stir/mix" instruction line from the recipe,
/// extracts a direction (clockwise/counter-clockwise) and speed (slow/fast)
/// from its wording, and configures MixingMechanic for that round.
/// </summary>
public class MixingSeasoningController : MonoBehaviour
{
    [Header("Panel")]
    public GameObject mixingPanel;   // MixingPanel
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

    private void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (mixingMechanic != null) mixingMechanic.OnMixFinished += HandleMixFinished;
    }

    private void OnDestroy()
    {
        if (mixingMechanic != null) mixingMechanic.OnMixFinished -= HandleMixFinished;
    }

    /// <summary>Call this when the spatula is dropped on the (heated) pan.</summary>
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
            Debug.Log("[Mixing] Recipe has no (remaining) sauté/stir step - nothing to do.");
            return;
        }
        if (randomizeDirection)
            direction = PickDirection();

        pendingLineIndex = lineIndex;
        challengeActive = true;

        mixingPanel.SetActive(true);
        mixingMechanic.ResetForNewRound();
        mixingMechanic.ConfigureChallenge(direction, speed);

        Debug.Log($"[Mixing] Opened for line {lineIndex}: '{recipe.cookingInstructions[lineIndex]}' -> direction={direction}, idealSpeed={speed}");
    }

    private void HandleMixFinished(MixingMechanic.MixResult result)
    {
        if (!challengeActive) return;

        bool success = result <= minimumSuccess; // enum order: VeryGood(0) < Good(1) < Bad(2)
        Debug.Log($"[Mixing] Result: {result} -> {(success ? "PASS" : "FAIL")}");

        ResultQuality quality = result switch
        {
            MixingMechanic.MixResult.VeryGood => ResultQuality.VeryGood,
            MixingMechanic.MixResult.Good => ResultQuality.Good,
            _ => ResultQuality.Bad
        };
        ScoreManager.Instance?.ReportResult(quality);

        if (success && pendingLineIndex >= 0)
        {
            completedLines.Add(pendingLineIndex);
            CookingPrepListUI.Instance?.CompleteStep($"Mix:{pendingLineIndex}");
        }

        challengeActive = false;
    }

    public void Close()
    {
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

    /// <summary>Call on Retry / new dish, next to the other reset calls.</summary>
    public void ResetProgress()
    {
        completedLines.Clear();
        pendingLineIndex = -1;
        challengeActive = false;
        mixingPanel.SetActive(false);
        mixingMechanic?.ResetForNewRound();
        lastDirection = MixingMechanic.MixDirection.Any;
    }

    private MixingMechanic.MixDirection PickDirection()
    {
        // First round: random. After that: always the opposite of last time.
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
}