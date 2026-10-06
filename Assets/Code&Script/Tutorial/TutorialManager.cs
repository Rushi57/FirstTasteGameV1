using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Drives the tutorial: shows dialogue lines in order, waits for the required
/// player action (if any), highlights the target, blocks other input, then
/// advances to the next step. Attach to a persistent GameObject (e.g. in
/// your UI Canvas) and wire up the refs in the Inspector.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Mistake")]
    public bool retryOnWrongAction = true;

    [Tooltip("ON: on a mistake, replay the explanation step right before this one (only if it's dialogue-only). OFF: replay the current step.")]
    public bool goBackOneStepOnMistake = true;

    [Tooltip("Used when the step has no Retry Message of its own.")]
    [TextArea] public string defaultRetryMessage = "Oops, that wasn't quite right. Let's try that again!";

    private bool showingRetryNotice = false;

    [Header("Tutorial Data")]
    [Tooltip("Unique key for this tutorial, in case you have more than one (e.g. per-level tutorials).")]
    public string tutorialId = "main_tutorial";
    public List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Persistence")]
    [Tooltip("If true, StartTutorial() resumes from the last step the player reached instead of restarting at 0.")]
    public bool resumeFromLastStep = true;

    [Tooltip("Write savegame.json as soon as the tutorial finishes/skips, instead of waiting for the Save button.")]
    public bool saveImmediately = false;

    private int tapCount;
    private float lastTapTime;
    private string CompletedKey => $"tutorial_{tutorialId}_completed";
    private string ProgressKey => $"tutorial_{tutorialId}_step";

    [Header("Start")]
    [Tooltip("ON: tutorial starts when the scene loads (Map scene). OFF: something else, like StoryTeller, calls StartTutorial().")]
    public bool autoStart = true;

    // A persisted, comma-separated list of every tutorialId that has ever
    // run in this game - lets ResetAllTutorials() find and clear every
    // tutorial's progress from a single static call, even from a scene
    // (e.g. the main menu) that has no live TutorialManager instance for
    // most of those ids.
    private const string KnownTutorialIdsKey = "TutorialManager_AllKnownIds";

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

        Debug.Log($"[TutorialManager] Awake() on '{gameObject.name}' (tutorialId='{tutorialId}'). InputBlocker assigned: {(inputBlocker != null ? inputBlocker.name : "NULL")}, forcing it inactive now.");

        // Safe baseline: InputBlocker should NEVER be active just because the
        // scene loaded - it should only ever be turned on by an actively
        // running step (see BeginCurrentStep). This matters most when the
        // tutorial is already completed: StartTutorial() returns immediately
        // in that case and never gets a chance to touch InputBlocker, so
        // without this it would stay stuck at whatever state the scene file
        // itself was saved with.
        if (inputBlocker != null)
            inputBlocker.SetActive(false);

        RegisterKnownTutorialId(tutorialId);
    }

    private void Start()
    {
        RegisterAllInteractables();

        if (dialogueBox != null)
            dialogueBox.OnNextPressed += HandleNextPressed;

        if (autoStart)
            StartTutorial();
    }

    public void Register(TutorialInteractable interactable)
    {
        if (interactable == null) return;
        Debug.Log($"[TutorialManager] Register('{interactable.ResolvedId}') on '{interactable.name}'");
        if (string.IsNullOrEmpty(interactable.ResolvedId)) return;
        registry[interactable.ResolvedId] = interactable;
        RefreshDragSourcesIfNeeded();
        RefreshTargetIfNeed(interactable);
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

        RegisterAllInteractables();   // pick up everything inside BackGroundImage now that it's active

        tutorialActive = true;

        int resumeIndex = resumeFromLastStep ? PlayerPrefs.GetInt(ProgressKey, 0) : 0;
        stepIndex = Mathf.Clamp(resumeIndex, 0, steps.Count - 1) - 1;
        AdvanceStep();
        Debug.Log($"[Tutorial] id='{tutorialId}' completed={HasCompletedTutorial()}");
    }

    /// <summary>Force-start regardless of saved progress (e.g. a "Replay Tutorial" button).</summary>
    public void ForceRestartTutorial()
    {
        ResetProgress();
        tutorialActive = true;
        stepIndex = -1;
        AdvanceStep();
    }

    public bool HasCompletedTutorial()
    {
        return GameSession.GetOrCreateData().IsTutorialCompleted(tutorialId);
    }

    /// <summary>Clears saved progress/completion for this tutorial id.</summary>
    public void ResetProgress()
    {
        GameSession.GetOrCreateData().completedTutorials.Remove(tutorialId);
        PlayerPrefs.DeleteKey(ProgressKey);
    }

    /// <summary>Adds id to the persisted set of every tutorialId that has ever run, if not already present.</summary>
    private static void RegisterKnownTutorialId(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        string existing = PlayerPrefs.GetString(KnownTutorialIdsKey, "");
        var ids = new HashSet<string>(existing.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));

        if (ids.Add(id))
        {
            PlayerPrefs.SetString(KnownTutorialIdsKey, string.Join(",", ids));
            PlayerPrefs.Save();
        }
    }

    private void RegisterAllInteractables()
    {
        // 'true' includes inactive objects (the old call skipped them)
        foreach (var interactable in FindObjectsByType<TutorialInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Register(interactable);
    }

    private void MarkCompleted()
    {
        SaveData data = GameSession.GetOrCreateData();
        data.MarkTutorialCompleted(tutorialId);
        if (saveImmediately) SaveSystem.Save(data);
    }

    /// <summary>
    /// Resets EVERY tutorial this game has ever run (across all scenes) back
    /// to unseen/unstarted - the opposite of SkipTutorial(). Static, so it
    /// can be called from anywhere (e.g. a "New Game" button on the main
    /// menu) without needing a live TutorialManager instance for every
    /// tutorial. Does NOT touch unrelated PlayerPrefs keys your save system
    /// might use - only this system's own completed/progress keys.
    /// </summary>
    public static void ResetAllTutorials()
    {
        string existing = PlayerPrefs.GetString(KnownTutorialIdsKey, "");
        string[] ids = existing.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string id in ids)
        {
            PlayerPrefs.DeleteKey($"tutorial_{id}_completed");
            PlayerPrefs.DeleteKey($"tutorial_{id}_step");
        }
        PlayerPrefs.Save();

        Debug.Log($"[TutorialManager] Reset {ids.Length} known tutorial(s) for a new game: {existing}");
    }

    /// <summary>
    /// Marks this tutorial as permanently completed WITHOUT stepping through
    /// it - so StartTutorial() will no-op on every future call, even if the
    /// player never actually finished the steps (e.g. they quit out early).
    /// Also immediately shuts down any active tutorial UI. Call this from
    /// wherever the player can leave/skip/quit the level (e.g. SceneChanger.QuitInGame).
    /// </summary>
    public void SkipTutorial()
    {
        MarkCompleted();
        PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.Save();

        if (tutorialActive)
            EndTutorial(); // also handles hiding UI, clearing pulses/blocker, etc.

        Debug.Log($"[TutorialManager] Tutorial '{tutorialId}' marked as skipped/completed - it will not start again.");
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
        if (!string.IsNullOrEmpty(step.targetId) && registry.TryGetValue(step.targetId.Trim(), out var target))
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
            if (registry.TryGetValue(step.dragSourceId.Trim(), out var source))
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
            ApplyBlockerAreas(step);
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

    private void RefreshTargetIfNeed(TutorialInteractable justRegistered)
    {
         if (!tutorialActive || stepIndex < 0 || stepIndex >= steps.Count) return;

        TutorialStep step = steps[stepIndex];
        if (string.IsNullOrEmpty(step.targetId)) return;
        if (justRegistered.ResolvedId != step.targetId.Trim()) return;

        currentTargetRect = justRegistered.RectTransform;
        Debug.Log($"[TutorialManager] Late-registered target '{justRegistered.ResolvedId}' for step '{step.name}'.");

        // Re-open the blocker hole now that the target actually exists.
        ApplyBlockerAreas(step);

        // If the player is already supposed to act, pulse now.
        if (waitingForAction && pulseTargets)
            AddPulse(currentTargetRect);
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
        tapCount = 0;
        ApplyBlockerAreas(step);

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
        // After the "Oops, try again" message, Next replays the step.
        if (showingRetryNotice)
        {
            showingRetryNotice = false;
            BeginCurrentStep();
            return;
        }

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
    /// <summary>Called by TutorialInteractable when the player performs an action.</summary>
    public void NotifyAction(string id, TutorialActionType type)
    {
        if (!tutorialActive || stepIndex < 0 || stepIndex >= steps.Count) return;
        if (!waitingForAction) return;

        TutorialStep step = steps[stepIndex];

        string targetId = step.targetId?.Trim();
        string sourceId = step.dragSourceId?.Trim();

        bool relatedToStep = id == targetId ||
                             (!string.IsNullOrEmpty(sourceId) && id == sourceId);

        // Some other button in the scene (e.g. CloseChopping) - not part of this step.
        if (!relatedToStep)
        {
            Debug.Log($"[TutorialManager] '{id}' ({type}) isn't part of step '{step.name}' - ignored.");
            return;
        }

        // Right object, wrong kind of action.
        if (step.actionType != type)
        {
            Debug.LogWarning($"[TutorialManager] '{id}': got {type}, expected {step.actionType}.");
            if (retryOnWrongAction) RetryStep();
            return;
        }

        waitingForAction = false;
        AdvanceStep();
    }

    /// <summary>Called by TutorialInteractable.ReportWrong() when the player makes a mistake.</summary>
    public void NotifyWrongAction()
    {
        if (!tutorialActive || !waitingForAction || !retryOnWrongAction) return;
        RetryStep();
    }

    private void RetryStep()
    {
        TutorialStep failedStep = steps[stepIndex];
        waitingForAction = false;
        ClearPulses();

        // Only step back onto a dialogue-only step, so the player never has to
        // redo a real action (e.g. spawning a second ingredient).
        if (goBackOneStepOnMistake && stepIndex > 0 &&
            steps[stepIndex - 1].actionType == TutorialActionType.None)
            stepIndex--;

        lineIndex = 0;
        PlayerPrefs.SetInt(ProgressKey, stepIndex);
        PlayerPrefs.Save();

        string message = !string.IsNullOrEmpty(failedStep.retryMessage)
            ? failedStep.retryMessage
            : defaultRetryMessage;

        if (!string.IsNullOrEmpty(message))
        {
            showingRetryNotice = true;
            dialogueBox.PlayLine(message, true);
        }
        else
        {
            BeginCurrentStep();
        }
    }

    /// <summary>
    /// Sets which areas pass through the blocker. The target/drag sources only
    /// become clickable once the manager is actually waiting for the action.
    /// </summary>
    private void ApplyBlockerAreas(TutorialStep step)
    {
        if (inputBlocker == null || !step.blockOtherInput) return;

        var filter = inputBlocker.GetComponent<TutorialInputBlockerFilter>();
        if (filter == null) return;

        var allowed = new List<RectTransform>();
        if (dialogueBox != null) allowed.Add(dialogueBox.RootRect);

        if (waitingForAction)
        {
            // Re-resolve now: the target may have registered after the step began.
            if (!string.IsNullOrEmpty(step.targetId) &&
                registry.TryGetValue(step.targetId.Trim(), out var t) && t != null)
                currentTargetRect = t.RectTransform;

            allowed.Add(currentTargetRect);
            allowed.AddRange(currentSourceRects);
        }

        filter.SetAllowedAreas(allowed.ToArray());
        Debug.Log($"[TutorialManager] Blocker areas for '{step.name}': waiting={waitingForAction}, target={(currentTargetRect != null ? currentTargetRect.name : "NULL")}, allowedCount={allowed.Count}");
    }

    private void EndTutorial()
    {
        tutorialActive = false;
        showingRetryNotice = false;
        dialogueBox.Hide();
        ClearPulses();

        if (inputBlocker != null)
        {
            inputBlocker.GetComponent<TutorialInputBlockerFilter>()?.ClearAllowedAreas();
            inputBlocker.SetActive(false);
        }

        MarkCompleted();
        PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.Save();

        Debug.Log("[TutorialManager] Tutorial ended, InputBlocker set inactive.");
    }

    public void AbortTutorial()
    {
        if (!tutorialActive) return;

        tutorialActive = false;
        waitingForAction = false;
        showingRetryNotice = false;
        dialogueBox.Hide();
        ClearPulses();

        if (inputBlocker != null)
        {
            inputBlocker.GetComponent<TutorialInputBlockerFilter>().ClearAllowedAreas();
            inputBlocker.SetActive(false);
        }
    }
}