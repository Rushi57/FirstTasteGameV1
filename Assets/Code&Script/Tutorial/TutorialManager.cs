using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the tutorial: shows dialogue lines in order, waits for the required
/// player action (if any), highlights the target, blocks other input, then
/// advances to the next step. Attach to a persistent GameObject (e.g. in
/// your UI Canvas) and wire up the refs in the Inspector.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial Data")]
    [Tooltip("Unique key for this tutorial, in case you have more than one (e.g. per-level tutorials).")]
    public string tutorialId = "main_tutorial";
    public List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Persistence")]
    [Tooltip("If true, StartTutorial() resumes from the last step the player reached instead of restarting at 0.")]
    public bool resumeFromLastStep = true;

    private string CompletedKey => $"tutorial_{tutorialId}_completed";
    private string ProgressKey => $"tutorial_{tutorialId}_step";

    [Header("UI Refs")]
    public DialogueBoxUI dialogueBox;
    public TutorialHighlight highlight;

    [Tooltip("A full-screen raycast blocker placed above gameplay but below the current target/dialogue box.")]
    public GameObject inputBlocker;

    [Header("Lookup")]
    [Tooltip("Auto-populated at runtime: id -> TutorialInteractable currently in the scene.")]
    private readonly Dictionary<string, TutorialInteractable> registry = new Dictionary<string, TutorialInteractable>();

    private int stepIndex = -1;
    private int lineIndex = 0;
    private bool waitingForAction = false;
    private bool tutorialActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Auto-register any TutorialInteractable already in the scene.
        foreach (var interactable in FindObjectsOfType<TutorialInteractable>())
            Register(interactable);

        if (dialogueBox != null)
            dialogueBox.OnNextPressed += HandleNextPressed;

        // TEMP TEST HOOK - remove/replace once you trigger StartTutorial()
        // from proper game logic (e.g. first-launch check, level-start event).
        StartTutorial();
    }

    public void Register(TutorialInteractable interactable)
    {
        if (interactable == null || string.IsNullOrEmpty(interactable.interactableId)) return;
        registry[interactable.interactableId] = interactable;
    }

    public void Unregister(TutorialInteractable interactable)
    {
        if (interactable == null) return;
        registry.Remove(interactable.interactableId);
    }

    /// <summary>
    /// Call this wherever you'd normally start the tutorial (splash screen,
    /// level load, etc). Safe to call every launch - it no-ops automatically
    /// if the player already finished it.
    /// </summary>
    public void StartTutorial()
    {
        if (steps.Count == 0) return;
        if (HasCompletedTutorial()) return;

        tutorialActive = true;

        int resumeIndex = resumeFromLastStep ? PlayerPrefs.GetInt(ProgressKey, 0) : 0;
        stepIndex = Mathf.Clamp(resumeIndex, 0, steps.Count - 1) - 1; // -1 because AdvanceStep() increments first
        AdvanceStep();
    }

    /// <summary>Force-start regardless of saved progress (e.g. a "Replay Tutorial" button).</summary>
    public void ForceRestartTutorial()
    {
        ResetProgress();
        tutorialActive = true;
        stepIndex = -1;
        AdvanceStep();
    }

    public bool HasCompletedTutorial() => PlayerPrefs.GetInt(CompletedKey, 0) == 1;

    /// <summary>Clears saved progress/completion for this tutorial id.</summary>
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(CompletedKey);
        PlayerPrefs.DeleteKey(ProgressKey);
    }

    private void AdvanceStep()
    {
        stepIndex++;
        if (stepIndex >= steps.Count)
        {
            EndTutorial();
            return;
        }
        lineIndex = 0;
        PlayerPrefs.SetInt(ProgressKey, stepIndex);
        PlayerPrefs.Save();
        BeginCurrentStep();
    }

    private void BeginCurrentStep()
    {
        TutorialStep step = steps[stepIndex];

        if (inputBlocker != null)
            inputBlocker.SetActive(step.blockOtherInput);

        if (step.highlightTarget && !string.IsNullOrEmpty(step.targetId) && registry.TryGetValue(step.targetId, out var target))
            highlight.ShowAround(target.RectTransform);
        else
            highlight.Hide();

        dialogueBox.Show(step.npcName, step.npcPortrait);
        PlayCurrentLine();
    }

    private void PlayCurrentLine()
    {
        TutorialStep step = steps[stepIndex];
        bool isLastLine = lineIndex >= step.dialogueLines.Length - 1;
        // Show the "Next" button only if this line doesn't hand off to a
        // required game action, OR it's not yet the last line.
        bool showNext = !isLastLine || step.actionType == TutorialActionType.None;

        dialogueBox.PlayLine(step.dialogueLines[lineIndex], showNext);

        waitingForAction = isLastLine && step.actionType != TutorialActionType.None;
    }

    private void HandleNextPressed()
    {
        if (waitingForAction) return; // shouldn't happen, but guard anyway

        TutorialStep step = steps[stepIndex];
        if (lineIndex < step.dialogueLines.Length - 1)
        {
            lineIndex++;
            PlayCurrentLine();
        }
        else
        {
            AdvanceStep();
        }
    }

    /// <summary>Called by TutorialInteractable when the player performs an action.</summary>
    public void NotifyAction(string id, TutorialActionType type)
    {
        if (!tutorialActive || stepIndex < 0 || stepIndex >= steps.Count) return;

        TutorialStep step = steps[stepIndex];
        if (!waitingForAction) return;
        if (step.targetId != id || step.actionType != type) return;

        waitingForAction = false;
        AdvanceStep();
    }

    private void EndTutorial()
    {
        tutorialActive = false;
        dialogueBox.Hide();
        highlight.Hide();
        if (inputBlocker != null) inputBlocker.SetActive(false);

        PlayerPrefs.SetInt(CompletedKey, 1);
        PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.Save();
    }
}