using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI References")]
    public GameObject tutorialOverlayPanel;
    public TextMeshProUGUI dialogText;
    public Button nextButton;
    public GameObject errorPanel;
    public TextMeshProUGUI errorText;

    [Header("Mechanic Managers")]
    public MixingGameManager mixingManager;
    public SimmerAndBoilManager simmerManager;

    [Header("Tutorial Sequence")]
    public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    private int currentStepIndex = 0;
    private Coroutine errorCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);

        if (errorPanel != null)
            errorPanel.SetActive(false);

        StartTutorial();
    }

    public void StartTutorial()
    {
        currentStepIndex = 0;
        tutorialOverlayPanel.SetActive(true);
        LoadStep(currentStepIndex);
    }

    private void LoadStep(int index)
    {
        if (index >= tutorialSteps.Count)
        {
            CompleteTutorial();
            return;
        }

        TutorialStep step = tutorialSteps[index];
        dialogText.text = step.dialogText;

        // Configure step behavior
        switch (step.requiredMechanic)
        {
            case GameMechanicType.DialogOnly:
                nextButton.gameObject.SetActive(true);
                if (mixingManager != null) mixingManager.gameObject.SetActive(false);
                if (simmerManager != null) simmerManager.gameObject.SetActive(false);
                break;

            case GameMechanicType.MixingSpatula:
                nextButton.gameObject.SetActive(false); // Hide next, force mechanic completion
                if (mixingManager != null)
                {
                    mixingManager.gameObject.SetActive(true);
                    mixingManager.targetCirculations = step.targetStirsCount;
                    mixingManager.StartMixingGame();
                }
                break;

            case GameMechanicType.SimmerAndBoil:
                nextButton.gameObject.SetActive(false);
                if (simmerManager != null)
                {
                    simmerManager.gameObject.SetActive(true);
                    simmerManager.StartSimmerAndBoilGame();
                }
                break;
        }
    }

    public void OnNextButtonClicked()
    {
        currentStepIndex++;
        LoadStep(currentStepIndex);
    }

    // Call this from mechanics when action completes successfully
    public void NotifyMechanicSuccess()
    {
        currentStepIndex++;
        LoadStep(currentStepIndex);
    }

    // Call this from mechanics when player makes an incorrect move
    public void ShowTutorialError(string message)
    {
        if (errorCoroutine != null) StopCoroutine(errorCoroutine);
        errorCoroutine = StartCoroutine(DisplayErrorRoutine(message));
    }

    private IEnumerator DisplayErrorRoutine(string message)
    {
        errorText.text = message;
        errorPanel.SetActive(true);

        // Android Haptic Feedback for error
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif

        yield return new WaitForSeconds(2.5f);
        errorPanel.SetActive(false);
    }

    private void CompleteTutorial()
    {
        dialogText.text = "Great job! You are ready to cook!";
        nextButton.gameObject.SetActive(true);
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(() => {
            tutorialOverlayPanel.SetActive(false);
            Debug.Log("[TUTORIAL] Completed successfully.");
        });
    }
}