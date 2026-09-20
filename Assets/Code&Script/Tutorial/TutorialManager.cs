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

    [Tooltip("A full-screen raycast blocker placed above gameplay but below the current target/dialogue box.")]
    public GameObject inputBlocker;

    [Header("Lookup")]
    [Tooltip("Auto-populated at runtime: id -> TutorialInteractable currently in the scene.")]
    private readonly Dictionary<string, TutorialInteractable> registry = new Dictionary<string, TutorialInteractable>();

    private int stepIndex = -1;
    private int lineIndex = 0;
    private bool waitingForAction = false;
    private bool tutorialActive = false;

    private RectTransform currentTargetRect;
    private List<RectTransform> currentSourceRects = new List<RectTransform>();

    [Header("Pulse Animation")]
    [Tooltip("Automatically pulse the current target/drag item to draw the player's attention.")]
    public bool pulseTargets = true;

    private readonly List<TutorialPulse> activePulses = new List<TutorialPulse>();

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
        if (interactable == null || string.IsNullOrEmpty(interactable.ResolvedId)) return;
        registry[interactable.ResolvedId] = interactable;
        RefreshDragSourcesIfNeeded();
    }

    public void Unregister(TutorialInteractable interactable)
    {
        if (interactable == null || string.IsNullOrEmpty(interactable.ResolvedId)) return;
        // Only remove if it's still the one currently registered under that id
        // (avoids a late-destroying old object wiping out a newer registration).
        if (registry.TryGetValue(interactable.ResolvedId, out var current) && current == interactable)
            registry.Remove(interactable.ResolvedId);
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

        RectTransform targetRect = null;
        if (!string.IsNullOrEmpty(step.targetId) && registry.TryGetValue(step.targetId, out var target))
            targetRect = target.RectTransform;

        currentTargetRect = targetRect;
        currentSourceRects = ResolveDragSources(step);

        Debug.Log($"[TutorialManager] Step '{step.name}' - targetId='{step.targetId}' resolved={(targetRect != null ? targetRect.name : "NULL")}, dragSourceId='{step.dragSourceId}' resolved {currentSourceRects.Count} source(s), registry has {registry.Count} entries: {string.Join(", ", registry.Keys)}");

        if (inputBlocker != null)
        {
            inputBlocker.SetActive(step.blockOtherInput);
            if (step.blockOtherInput)
            {
                var filter = inputBlocker.GetComponent<TutorialInputBlockerFilter>();
                if (filter == null)
                    Debug.LogWarning("[TutorialManager] InputBlocker has no TutorialInputBlockerFilter component attached! Blocking will not exempt anything.");

                // Exempt: the dialogue box (Next keeps working), the target
                // (drop zone or tap button), and every resolved drag source.
                var allowed = new List<RectTransform> { targetRect, dialogueBox.RootRect };
                allowed.AddRange(currentSourceRects);
                filter?.SetAllowedAreas(allowed.ToArray());
            }
        }

        dialogueBox.SetPosition(step.dialoguePosition, targetRect, step.customPosition);
        dialogueBox.Show(step.npcName, step.npcPortrait);
        ClearPulses(); // no pulsing until PlayCurrentLine decides we're actually waiting for the action
        PlayCurrentLine();
    }

    /// <summary>
    /// Resolves which object(s) should be exempted from the input blocker as
    /// the "drag source" for this step. If step.dragSourceId is set, resolves
    /// that one specific object. If it's left blank on a Drag-type step,
    /// resolves EVERY currently-registered object that has a TestDrag
    /// component - letting the player pick up whichever draggable item
    /// exists, without needing to know its exact id ahead of time (useful
    /// when items are spawned dynamically from ScriptableObject data).
    /// </summary>
    private List<RectTransform> ResolveDragSources(TutorialStep step)
    {
        var result = new List<RectTransform>();
        if (step.actionType != TutorialActionType.Drag) return result;

        if (!string.IsNullOrEmpty(step.dragSourceId))
        {
            if (registry.TryGetValue(step.dragSourceId, out var source))
                result.Add(source.RectTransform);
            return result;
        }

        // Wildcard: exempt every registered interactable that's actually draggable.
        foreach (var interactable in registry.Values)
        {
            if (interactable != null && interactable.GetComponent<TestDrag>() != null)
                result.Add(interactable.RectTransform);
        }
        return result;
    }

    /// <summary>
    /// Called whenever something new registers (e.g. a dynamically spawned
    /// ingredient). If we're currently on a wildcard Drag step (dragSourceId
    /// left blank) and already waiting for the action, this re-resolves the
    /// source list and immediately exempts/pulses the newly spawned item -
    /// without this, an item spawned mid-step would be invisible to the
    /// blocker/pulse until the NEXT step began.
    /// </summary>
    private void RefreshDragSourcesIfNeeded()
    {
        if (!tutorialActive || stepIndex < 0 || stepIndex >= steps.Count) return;

        TutorialStep step = steps[stepIndex];
        if (step.actionType != TutorialActionType.Drag) return;
        if (!string.IsNullOrEmpty(step.dragSourceId)) return; // only relevant in wildcard mode

        currentSourceRects = ResolveDragSources(step);

        if (inputBlocker != null && step.blockOtherInput)
        {
            var filter = inputBlocker.GetComponent<TutorialInputBlockerFilter>();
            var allowed = new List<RectTransform> { currentTargetRect, dialogueBox.RootRect };
            allowed.AddRange(currentSourceRects);
            filter?.SetAllowedAreas(allowed.ToArray());
        }

        if (waitingForAction && pulseTargets)
        {
            foreach (var rect in currentSourceRects)
                AddPulse(rect);
        }
    }

    private void AddPulse(RectTransform rect)
    {
        if (rect == null) return;
        // Avoid double-pulsing if target and source happen to be the same object.
        if (activePulses.Exists(p => p != null && p.transform == rect)) return;

        var pulse = rect.gameObject.AddComponent<TutorialPulse>();
        activePulses.Add(pulse);
    }

    private void ClearPulses()
    {
        foreach (var pulse in activePulses)
        {
            if (pulse != null)
                Destroy(pulse);
        }
        activePulses.Clear();
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

        // Only pulse once the player actually needs to perform the action
        // (Next button gone) - not while they're still reading buildup lines.
        if (waitingForAction && pulseTargets)
        {
            AddPulse(currentTargetRect);
            foreach (var sourceRect in currentSourceRects)
                AddPulse(sourceRect);
        }
        else
        {
            ClearPulses();
        }
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
        if (!waitingForAction)
        {
            Debug.Log($"[TutorialManager] NotifyAction('{id}', {type}) received but not currently waiting for an action - ignored.");
            return;
        }
        if (step.targetId != id || step.actionType != type)
        {
            Debug.LogWarning($"[TutorialManager] NotifyAction('{id}', {type}) did NOT match current step's expected targetId='{step.targetId}', actionType={step.actionType} - action rejected. Check for an empty/mismatched Interactable Id.");
            return;
        }

        waitingForAction = false;
        AdvanceStep();
    }

    private void EndTutorial()
    {
        tutorialActive = false;
        dialogueBox.Hide();
        ClearPulses();

        if (inputBlocker != null)
        {
            inputBlocker.GetComponent<TutorialInputBlockerFilter>()?.ClearAllowedAreas();
            inputBlocker.SetActive(false);
        }

        PlayerPrefs.SetInt(CompletedKey, 1);
        PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.Save();

        Debug.Log("[TutorialManager] Tutorial ended, InputBlocker set inactive.");
    }
}