using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Unified quality result every mini-game reports, regardless of its own
/// internal naming (MixResult, CutQuality, PourResult, etc. all map to this).
/// </summary>
public enum ResultQuality
{
    VeryGood,
    Good,
    Bad
}

/// <summary>
/// How many points/hearts each quality tier costs. Exposed in the Inspector
/// so you can tune balance per-dish without touching code. Defaults assume
/// score starts at 100 and a recipe has roughly 8-12 gradable actions total -
/// adjust badPointDeduction so a couple of mistakes are recoverable but a
/// string of them meaningfully drops your star rating.
/// </summary>
[System.Serializable]
public class ScoreDeductionSettings
{
    [Header("Point deduction per mistake")]
    public int veryGoodPointDeduction = 0;
    public int goodPointDeduction = 5;
    public int badPointDeduction = 15;

    [Header("Hearts lost per mistake")]
    public int veryGoodHeartLoss = 0;
    public int goodHeartLoss = 0;
    public int badHeartLoss = 1;
}

/// <summary>
/// Singleton scoring system every mini-game reports into. Attach to a
/// persistent GameObject in your kitchen/level scene.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score")]
    public int startingScore = 100;

    [Header("Hearts")]
    public int startingHearts = 3;

    [Header("Deduction Settings")]
    public ScoreDeductionSettings deductions = new ScoreDeductionSettings();

    [Header("Star Thresholds (score >= this value)")]
    public int threeStarMinScore = 90;
    public int twoStarMinScore = 75;
    // Anything below twoStarMinScore = 1 star.

    [Header("UI (optional - wire up or leave blank)")]
    public TMP_Text scoreLabel;
    public TMP_Text heartsLabel;
    public GameObject gameOverPanel;

    public System.Action<int> OnScoreChanged;
    public System.Action<int> OnHeartsChanged;
    public System.Action OnGameOver;

    public int CurrentScore { get; private set; }
    public int CurrentHearts { get; private set; }
    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Call this when a new dish/level begins, to reset score and hearts.</summary>
    public void StartNewDish()
    {
        CurrentScore = startingScore;
        CurrentHearts = startingHearts;
        IsGameOver = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        RefreshUI();
    }

    /// <summary>
    /// Every mini-game calls this exactly once per GRADABLE action (not per
    /// tap/intermediate step - see CuttingMechanic for the multi-cut example).
    /// </summary>
    public void ReportResult(ResultQuality quality)
    {
        if (IsGameOver) return;

        int pointLoss = quality switch
        {
            ResultQuality.Bad => deductions.badPointDeduction,
            ResultQuality.Good => deductions.goodPointDeduction,
            _ => deductions.veryGoodPointDeduction
        };
        int heartLoss = quality switch
        {
            ResultQuality.Bad => deductions.badHeartLoss,
            ResultQuality.Good => deductions.goodHeartLoss,
            _ => deductions.veryGoodHeartLoss
        };

        CurrentScore = Mathf.Max(0, CurrentScore - pointLoss);
        CurrentHearts = Mathf.Max(0, CurrentHearts - heartLoss);

        Debug.Log($"[ScoreManager] Result: {quality} | -{pointLoss} pts (score now {CurrentScore}) | -{heartLoss} hearts (hearts now {CurrentHearts})");

        RefreshUI();
        OnScoreChanged?.Invoke(CurrentScore);
        OnHeartsChanged?.Invoke(CurrentHearts);

        if (CurrentHearts <= 0)
            TriggerGameOver();
    }

    private void TriggerGameOver()
    {
        IsGameOver = true;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Debug.Log("[ScoreManager] GAME OVER - hearts reached 0.");
        OnGameOver?.Invoke();
    }

    /// <summary>3/2/1 stars based on CurrentScore against the thresholds above.</summary>
    public int GetStarRating()
    {
        if (CurrentScore >= threeStarMinScore) return 3;
        if (CurrentScore >= twoStarMinScore) return 2;
        return 1;
    }

    /// <summary>
    /// Call this when a dish is finished (not when it starts!). Saves the
    /// player's ACTUAL CurrentScore/stars achieved this run into persistent
    /// SaveData - NOT startingScore, which is only ever the fresh-attempt
    /// default before any deductions happened.
    /// </summary>
    /// <param name="levelNumber">Which level this result belongs to. If omitted, uses LevelSelectionManager.SelectedLevel if available.</param>
    /// <param name="keepBest">If true (default), only overwrites a previous save if this score is better.</param>
    public void SaveDishResult(int levelNumber = -1, bool keepBest = true)
    {
        if (levelNumber < 0)
        {
            var selected = LevelSelectionManager.SelectedLevel;
            if (selected == null)
            {
                Debug.LogWarning("[ScoreManager] SaveDishResult() called with no levelNumber and no LevelSelectionManager.SelectedLevel - nothing to save against.");
                return;
            }
            levelNumber = selected.levelNumber;
        }

        SaveData data = GameSession.GetOrCreateData();
        data.SetLevelResult(levelNumber, CurrentScore, GetStarRating(), keepBest);
        SaveSystem.Save(data);

        Debug.Log($"[ScoreManager] Saved Level {levelNumber} result: score={CurrentScore}, stars={GetStarRating()}");
    }

    private void RefreshUI()
    {
        if (scoreLabel != null) scoreLabel.text = CurrentScore.ToString();
        if (heartsLabel != null) heartsLabel.text = CurrentHearts.ToString();
    }
}