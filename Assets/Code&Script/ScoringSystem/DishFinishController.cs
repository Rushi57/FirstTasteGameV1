using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DishFinishController : MonoBehaviour
{
    [Header("Prep list")]
    public CookingPrepListUI prepList;
    
    [Header("Finish button")]
    public GameObject finishButtonPanel;    // FinishBtnPanelShow
    public Button continueButton;           // ContinueBtn

    [Header("Scoreboard")]
    public GameObject finishCookingRoot;    // FinishCookingGameObject
    public GameObject scoreBoardPanel;      // ScoreBoardPanel

    [Header("Must ALL be closed before Continue shows")]
    public GameObject[] miniGamePanels;     // Mixing, Chopping/Cutting, PourSeasoning, Boil/Simmer, Salt/Pepper
    public GameObject[] animationObjects;   // e.g. Pitcher with water, WaterStream (pour animation)

    [Header("Timing")]
    public float extraDelay = 0.5f;         // short pause after the last mini-game closes

    private bool allStepsDone;
    private Coroutine waitRoutine;

    [Header("Scoreboard content")]
    public CookingPrepPanelController cookingPrepPanel;   // the same one LevelRecipeLoader fills
    public Image recipeImage;                             // the Image inside RecipeImageHolder
    public TMP_Text dishTitleLabel;
    private void Start()
    {
        finishButtonPanel.SetActive(false);
        scoreBoardPanel.SetActive(false);
        continueButton.onClick.AddListener(OnContinue);

        if (prepList != null) prepList.OnAllStepsComplete += HandleAllStepsComplete;
        else Debug.LogWarning("[Finish] Prep List is not assigned.");
    }

    private void OnDestroy()
    {
        if (prepList != null) prepList.OnAllStepsComplete -= HandleAllStepsComplete;
    }

    private void HandleAllStepsComplete()
    {
        allStepsDone = true;
        if (waitRoutine == null)
            waitRoutine = StartCoroutine(WaitThenShow());
    }

    private IEnumerator WaitThenShow()
    {
        // Wait until nothing is running
        yield return new WaitUntil(NothingRunning);
        yield return new WaitForSecondsRealtime(extraDelay);

        // A mini-game could have opened during the delay, so check again
        while (!NothingRunning())
            yield return null;

        waitRoutine = null;

        if (ScoreManager.Instance != null && ScoreManager.Instance.IsGameOver) yield break;

        Debug.Log("[Finish] All steps done and all mini-games closed - showing Continue.");
        finishButtonPanel.SetActive(true);
    }

    private bool NothingRunning()
    {
        foreach (var p in miniGamePanels)
            if (p != null && p.activeInHierarchy) return false;
        foreach (var a in animationObjects)
            if (a != null && a.activeInHierarchy) return false;
        return true;
    }

    private void OnContinue()
    {
        finishButtonPanel.SetActive(false);
        finishCookingRoot.SetActive(true);
        scoreBoardPanel.SetActive(true);

        RecipeData recipe = cookingPrepPanel != null ? cookingPrepPanel.currentRecipe : null;
        if (recipe != null)
        {
            if (recipeImage != null) recipeImage.sprite = recipe.recipeIcon;   // see note below
            if (dishTitleLabel != null) dishTitleLabel.text = recipe.recipeName;
        }

        ScoreManager.Instance?.SaveDishResult();

    }

    // Call this from Retry so the flag resets
    public void ResetFinish()
    {
        allStepsDone = false;
        if (waitRoutine != null) { StopCoroutine(waitRoutine); waitRoutine = null; }
        finishButtonPanel.SetActive(false);
    }
}