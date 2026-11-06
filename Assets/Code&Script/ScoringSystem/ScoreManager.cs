using UnityEngine;
using TMPro;
using UnityEngine.UI;

public enum ResultQuality
{
    VeryGood,
    Good,
    Bad
}

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

public class ScoreManager : MonoBehaviour
{
    private static bool InTutorial =>
    TutorialManager.Instance != null && TutorialManager.Instance.IsActive;

    public static ScoreManager Instance { get; private set; }

    [Header("Score")]
    public int startingScore = 100;

    [Header("Hearts")]
    public int startingHearts = 5;

    [Header("Deduction Settings")]
    public ScoreDeductionSettings deductions = new ScoreDeductionSettings();

    [Header("Star Thresholds (score >= this value)")]
    public int threeStarMinScore = 90;
    public int twoStarMinScore = 75;
   
    [Header("UI (optional - wire up or leave blank)")]
    public TMP_Text scoreLabel;
    public TMP_Text heartsLabel;
    public GameObject gameOverPanel;

    public System.Action<int> OnScoreChanged;
    public System.Action<int> OnHeartsChanged;
    public System.Action OnGameOver;

    [Header("Wrong Step (Skipping Ahead)")]
    public int wrongStepPointDeduction = 5;
    public int wrongStepHeartLoss = 1;

    public int CurrentScore { get; private set; }
    public int CurrentHearts { get; private set; }
    public bool IsGameOver { get; private set; }

    private void Awake()
    {

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        StartNewDish();
    }

    public void StartNewDish()
    {
        CurrentScore = startingScore;
        CurrentHearts = startingHearts;
        IsGameOver = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        RefreshUI();
    }

 
    public void ReportResult(ResultQuality quality)
    {


        if (IsGameOver || InTutorial) return;

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

    public void ReportWrongStep()
    {
        if (IsGameOver || InTutorial) return;

        CurrentScore = Mathf.Max(0, CurrentScore - wrongStepPointDeduction);
        CurrentHearts = Mathf.Max(0, CurrentHearts - wrongStepHeartLoss);

        Debug.Log($"[ScoreManager] WRONG STEP | -{wrongStepPointDeduction} pts (score now {CurrentScore}) | -{wrongStepHeartLoss} hearts (hearts now {CurrentHearts})");

        RefreshUI();
        OnScoreChanged?.Invoke(CurrentScore);
        OnHeartsChanged?.Invoke(CurrentHearts);

        if (CurrentHearts <= 0)
            TriggerGameOver();
    }
    

    public void ReportMistake(string message, int points = 6, int hearts = 1 )
    {
        if (IsGameOver) return;

        if (InTutorial)
        {
            WarningMessageUI.Instance?.Show(message);   
            return;
        }

        CurrentScore = Mathf.Max(0, CurrentScore -  points);
        CurrentHearts = Mathf.Max(0, CurrentHearts - hearts);
        Debug.Log($"[ScoreManager] MISTAKE '{message}' | -{points} pts (score now {CurrentScore}) | -{hearts} hearts (hearts now {CurrentHearts})");

        if (CurrentHearts > 0)
            WarningMessageUI.Instance?.Show(message);

        RefreshUI();
        OnScoreChanged?.Invoke(CurrentScore);
        OnHeartsChanged?.Invoke(CurrentHearts);

        if (CurrentHearts <= 0)
            TriggerGameOver();
    }

    public void SetPerfectScore()
    {
        CurrentScore = startingScore;   
        CurrentHearts = startingHearts;
        IsGameOver = false;
        RefreshUI();
        OnScoreChanged?.Invoke(CurrentScore);
        OnHeartsChanged?.Invoke(CurrentHearts);
    }
    private void TriggerGameOver()
    {
        IsGameOver = true;
        WarningMessageUI.Instance?.Hide();

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (SFXManager.Instance != null) SFXManager.Instance.Play(SFXManager.Instance.gameOverPanel);

        Debug.Log("[ScoreManager] GAME OVER - hearts reached 0.");
        OnGameOver?.Invoke();
    }

    public int GetStarRating()
    {
        if (CurrentScore >= threeStarMinScore) return 3;
        if (CurrentScore >= twoStarMinScore) return 2;
        return 1;
    }


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
        

        Debug.Log($"[ScoreManager] Saved Level {levelNumber} result: score={CurrentScore}, stars={GetStarRating()}");
    }

    private void RefreshUI()
    {
        if (scoreLabel != null) scoreLabel.text = CurrentScore.ToString();
        if (heartsLabel != null) heartsLabel.text = CurrentHearts.ToString();
    }
}