using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DishFinishController : MonoBehaviour
{
    [Header("Prep list")]
    public CookingPrepListUI prepList;

    [Header("Finish button")]
    public GameObject finishButtonPanel;   
    public Button continueButton;          

    [Header("Level completion reflection")]
    public LevelCompletionReflection reflection;  

    [Header("Scoreboard")]
    public GameObject finishCookingRoot;   
    public GameObject scoreBoardPanel;    

    [Header("Must ALL be closed before Continue shows")]
    public GameObject[] miniGamePanels;    
    public GameObject[] animationObjects;  

    [Header("Timing")]
    public float extraDelay = 0.5f;       

    private bool allStepsDone;
    private Coroutine waitRoutine;

    [Header("Scoreboard content")]
    public CookingPrepPanelController cookingPrepPanel;   
    public Image recipeImage;                            
    private void Start()
    {
        finishButtonPanel.SetActive(false);
        scoreBoardPanel.SetActive(false);
        if (finishCookingRoot != null) finishCookingRoot.SetActive(false); 
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
        if (waitRoutine != null) { StopCoroutine(waitRoutine); waitRoutine = null; }

        if (isActiveAndEnabled)
            waitRoutine = StartCoroutine(WaitThenShow());
        else
            Debug.LogWarning("[Finish] DishFinishController is inactive - can't start WaitThenShow.");
    }

    private IEnumerator WaitThenShow()
    {

        yield return new WaitUntil(NothingRunning);
        yield return new WaitForSecondsRealtime(extraDelay);

   
        while (!NothingRunning())
            yield return null;

        waitRoutine = null;

        if (ScoreManager.Instance != null && ScoreManager.Instance.IsGameOver) yield break;


        Transform parent = finishButtonPanel.transform.parent;
        if (parent != null && !parent.gameObject.activeSelf)
            parent.gameObject.SetActive(true);

        finishButtonPanel.SetActive(true);
        Debug.Log($"[Finish] Panel active in hierarchy: {finishButtonPanel.activeInHierarchy}");
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

        RecipeData recipe = cookingPrepPanel != null ? cookingPrepPanel.currentRecipe : null;
        if (recipe != null)
        {
            if (recipeImage != null) recipeImage.sprite = recipe.recipeIcon;
           
        }


        scoreBoardPanel.SetActive(true);

        if (reflection != null)
        {
            
            reflection.Show();
        }
        else
        {
      
            finishCookingRoot.SetActive(true);
            ScoreManager.Instance?.SaveDishResult();
        }
    }

    public void ResetFinish()
    {
        allStepsDone = false;
        if (waitRoutine != null) { StopCoroutine(waitRoutine); waitRoutine = null; }
        finishButtonPanel.SetActive(false);
    }
    void OnDisable() { Debug.Log("FinishBtnShowGameObject disabled\n" + System.Environment.StackTrace); }
}