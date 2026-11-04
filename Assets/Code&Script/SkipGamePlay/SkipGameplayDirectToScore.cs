using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Skips the cooking gameplay, forces a perfect score (3 stars) and jumps
/// straight into the existing finish flow:
///   DishFinishController.OnContinue -> ScoreBoardPanel -> LevelCompletionReflection (dialogue/story).
///
/// Attach to SkipGamePlayDirectToPerfectScore (the parent of the SkipGamePlay button).
///
/// Two-APK setup:
///  - FirstTasteMainGame.apk         -> do NOT define STORY_TEST_BUILD (skip button is removed)
///  - FirstTasteDialogueAndStory.apk -> add scripting define STORY_TEST_BUILD
///        (Build Profiles > your Android profile > Scripting Defines)
/// The skip button also works inside the Unity Editor.
/// </summary>
public class SkipGameplayDirectToScore : MonoBehaviour
{
    [Header("Build Mode")]
    [Tooltip("If true, the skip button is removed unless this is the Editor or a STORY_TEST_BUILD build.")]
    public bool onlyInStoryTestBuild = true;

    [Header("Wiring")]
    [Tooltip("The SkipGamePlay button. Auto-detected if left empty.")]
    public Button skipButton;

    [Tooltip("The DishFinishController (on FinishBtnShowGameObject). Its mini-game panels, animation objects and Continue button are reused.")]
    public DishFinishController dishFinish;

    [Tooltip("Extra gameplay objects to switch off (GameMechanicsGameObject, TableZone, TopUtensils, MainFridge, CabinetMain, SetAsideInstruction, InGameTutorialGameObject...). " +
             "Do NOT add FinishBtnShowGameObject, FinishCookingGameObject, ScoringSystemGameObject or StoryTellingGameObject.")]
    public GameObject[] objectsToDisable;

    private bool _skipped;

    private void Awake()
    {
#if !UNITY_EDITOR && !STORY_TEST_BUILD
        if (onlyInStoryTestBuild)
        {
            Destroy(gameObject);   // main game build: no skip button
            return;
        }
#endif
        if (skipButton == null) skipButton = GetComponentInChildren<Button>(true);
        if (skipButton != null) skipButton.onClick.AddListener(SkipGameplay);
    }

    private void OnDestroy()
    {
        if (skipButton != null) skipButton.onClick.RemoveListener(SkipGameplay);
    }

    public void SkipGameplay()
    {
        if (_skipped) return;
        _skipped = true;

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsActive)
            TutorialManager.Instance.AbortTutorial();
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.SetPerfectScore();
        else
            Debug.LogWarning("[SkipGameplay] No ScoreManager.Instance found.");

        if (dishFinish != null)
        {
            foreach (var p in dishFinish.miniGamePanels)
                if (p != null) p.SetActive(false);
            foreach (var a in dishFinish.animationObjects)
                if (a != null) a.SetActive(false);

            // Stop a pending "show Finish button" wait, if any.
            dishFinish.ResetFinish();
        }
        foreach (var go in objectsToDisable)
            if (go != null) go.SetActive(false);

        // 4) Hide the skip button.
        if (skipButton != null) skipButton.gameObject.SetActive(false);

        // 5) Same code path as the real Continue button: scoreboard + SaveDishResult + dialogue/story.
        if (dishFinish != null && dishFinish.continueButton != null)
            dishFinish.continueButton.onClick.Invoke();
        else
            Debug.LogWarning("[SkipGameplay] dishFinish / continueButton not assigned - cannot open the scoreboard.");

        Debug.Log($"[SkipGameplay] Skipped. Score={ScoreManager.Instance?.CurrentScore}, Stars={ScoreManager.Instance?.GetStarRating()}");
    }
}