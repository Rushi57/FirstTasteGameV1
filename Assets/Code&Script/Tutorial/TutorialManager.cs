using System.Collections.Generic;
using UnityEngine;
using System.Collections;
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

    [Tooltip("Write savegame.json as soon as the tutorial finishes/skips, instead of waiting for the Save button.")]
    public bool saveImmediately = false;

    private int tapCount;
    private float lastTapTime;
    private string CompletedKey => $"tutorial_{tutorialId}_completed";
    private string ProgressKey => $"tutorial_{tutorialId}_step";

    [Header("Start")]
    [Tooltip("ON: tutorial starts when the scene loads (Map scene). OFF: something else, like StoryTeller, calls StartTutorial().")]
    public bool autoStart = true;

    [Header("Mistakes")]
    [Tooltip("Master switch for reverting when the player makes a mistake.")]
    public bool revertOnMistake = true;


    [Tooltip("How many steps to go back on a mistake. 0 = replay the current step, 1 = previous step.")]
    [Min(0)] public int stepsToRevert = 1;


    public event System.Action OnTutorialReverted;

    private string[] currentLines;          // lines actually being played (may include a mistake message)
    private string pendingMistakeMessage;
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


    private Coroutine advanceRoutine;
    private int busyCount;

    private bool lockByMistake;


    public bool IsActive => tutorialActive;
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
        if (steps.Count == 0) return;
        if (HasCompletedTutorial()) return;

        RegisterAllInteractables();   // pick up everything inside BackGroundImage now that it's active

        tutorialActive = true;

        int resumeIndex = resumeFromLastStep ? PlayerPrefs.GetInt(ProgressKey, 0) : 0;
        stepIndex = Mathf.Clamp(resumeIndex, 0, steps.Count - 1) - 1;
        lockByMistake = false;
        AdvanceStep();
        Debug.Log($"[Tutorial] id='{tutorialId}' completed={HasCompletedTutorial()}");
    }

    /// <summary>Force-start regardless of saved progress (e.g. a "Replay Tutorial" button).</summary>
    public void ForceRestartTutorial()
    {
        ResetProgress();
        tutorialActive = true;
        stepIndex = -1;
        lockByMistake = false;
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

    /// <summary>
    /// Resets EVERY tutorial this game has ever run (across all scenes) back
    /// to unseen/unstarted - the opposite of SkipTutorial(). Static, so it
    /// can be called from anywhere (e.g. a "New Game" button on the main
    /// menu) without needing a live TutorialManager instance for every
    /// tutorial. Does NOT touch unrelated PlayerPrefs keys your save system
    /// might use - only this system's own completed/progress keys.
    /// </summary>
    /// 
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

        currentLines = step.dialogueLines;
        if (!string.IsNullOrEmpty(pendingMistakeMessage))
        {
            var list = new List<string> { pendingMistakeMessage };
            list.AddRange(step.dialogueLines);
            currentLines = list.ToArray();
            pendingMistakeMessage = null;
        }

        RectTransform targetRect = null;
        if (!string.IsNullOrEmpty(step.targetId) && registry.TryGetValue(step.targetId.Trim(), out var target))
            targetRect = target.RectTransform;

        currentTargetRect = targetRect;
        currentSourceRects = ResolveDragSources(step);

        Debug.Log($"[TutorialManager] Step '{step.name}' - targetId='{step.targetId}' resolved={(targetRect != null ? targetRect.name : "NULL")}, dragSourceId='{step.dragSourceId}' resolved {currentSourceRects.Count} source(s), registry has {registry.Count} entries: {string.Join(", ", registry.Keys)}");

        ApplyInputBlocker();

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

        ApplyInputBlocker();

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

        ApplyInputBlocker();

        // If the player is already supposed to act, pulse now.
        // Otherwise PlayCurrentLine pulses it when the last line shows.
        if (waitingForAction && pulseTargets)
            AddPulse(currentTargetRect);
    }

    private void PlayCurrentLine()
    {
        TutorialStep step = steps[stepIndex];
        bool isLastLine = lineIndex >= currentLines.Length - 1;
        // Show the "Next" button only if this line doesn't hand off to a
        // required game action, OR it's not yet the last line.
        bool showNext = !isLastLine || step.actionType == TutorialActionType.None;

        dialogueBox.PlayLine(currentLines[lineIndex], showNext);

        waitingForAction = isLastLine && step.actionType != TutorialActionType.None;
        tapCount = 0;

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
        ApplyInputBlocker();
    }

    private void HandleNextPressed()
    {
        if (waitingForAction || advanceRoutine != null) return;

        lockByMistake = false;   // Oops line dismissed, unlock before the next line plays

        TutorialStep step = steps[stepIndex];
        if (lineIndex < currentLines.Length - 1)
        {
            lineIndex++;
            PlayCurrentLine();   // ApplyInputBlocker runs here with the lock off
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



        if (step.targetId?.Trim() != id || step.actionType != type)
        {
            Debug.LogWarning($"[TutorialManager] NotifyAction('{id}', {type}) did NOT match step '{step.name}' (expected '{step.targetId}', {step.actionType}).");
            HandleMistake(step);
            return;
        }

        waitingForAction = false;
        ClearPulses();   // stop pulsing while the animation plays
        CancelPendingAdvance(false);
        advanceRoutine = StartCoroutine(AdvanceWhenReady(step));
    }

    /// <summary>Call when a mechanic starts an animation the tutorial must wait for.</summary>
    public void BeginBusy() => busyCount++;

    /// <summary>Call when that animation finishes.</summary>
    public void EndBusy() => busyCount = Mathf.Max(0, busyCount - 1);

    private IEnumerator AdvanceWhenReady(TutorialStep step)
    {
        yield return null;   // let same-frame handlers call BeginBusy first

        if (step.advanceDelay > 0f)
            yield return new WaitForSecondsRealtime(step.advanceDelay);

        while (busyCount > 0)
            yield return null;

        advanceRoutine = null;
        AdvanceStep();
    }

    private void CancelPendingAdvance(bool resetBusy = true)
    { 
        if (advanceRoutine != null)
        {
            StopCoroutine(advanceRoutine);
            advanceRoutine = null;
        }
        if (resetBusy) busyCount = 0;
    }

    private void GoToStep(int index)
    {
        CancelPendingAdvance();
        stepIndex = Mathf.Clamp(index, 0, steps.Count - 1);
        lineIndex = 0;
        waitingForAction = false;
        PlayerPrefs.SetInt(ProgressKey, stepIndex);
        PlayerPrefs.Save();
        BeginCurrentStep();
    }

    public void ReportMistake()
    {
        if (!tutorialActive || !waitingForAction) return;
        HandleMistake(steps[stepIndex]);
    }

    private void HandleMistake(TutorialStep failedStep)
    {
        if (!revertOnMistake || !failedStep.revertOnMistake) return;

        int back = failedStep.revertSteps >= 0 ? failedStep.revertSteps : stepsToRevert;

        waitingForAction = false;
        pendingMistakeMessage = failedStep.mistakeMessage;
        Debug.Log($"[TutorialManager] Mistake on step '{failedStep.name}' - reverting {back} step(s).");
        OnTutorialReverted?.Invoke();
        lockByMistake = !string.IsNullOrEmpty(failedStep.mistakeMessage);
        GoToStep(stepIndex - back);
    }

    private void EndTutorial()
    {
        CancelPendingAdvance();
        tutorialActive = false;
        dialogueBox.Hide();
        lockByMistake = false;
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
        CancelPendingAdvance();
        lockByMistake = false;
        if (!tutorialActive) return;

        tutorialActive = false;
        waitingForAction = false;
        dialogueBox.Hide();
        ClearPulses();

        if (inputBlocker != null)
        {
            inputBlocker.GetComponent<TutorialInputBlockerFilter>().ClearAllowedAreas();
            inputBlocker.SetActive(false);
        }
    }
    private void ApplyInputBlocker()
    {
        if (inputBlocker == null || dialogueBox == null) return;
        if (stepIndex < 0 || stepIndex >= steps.Count) return;

        TutorialStep step = steps[stepIndex];

        // After a mistake the blocker is always on, whatever the step's setting
        bool block = step.blockOtherInput || lockByMistake;

        inputBlocker.SetActive(block);
        if (!block) return;

        var filter = inputBlocker.GetComponent<TutorialInputBlockerFilter>();
        if (filter == null)
        {
            Debug.LogWarning("[TutorialManager] InputBlocker has no TutorialInputBlockerFilter component attached!");
            return;
        }

        // Always allowed: the dialogue box and the Continue button
        var allowed = new List<RectTransform> { dialogueBox.RootRect };
        if (dialogueBox.nextButton != null)
            allowed.Add(dialogueBox.nextButton.transform as RectTransform);

        // Target and drag sources open up only when NOT locked by a mistake
        if (!lockByMistake)
        {
            if (currentTargetRect != null) allowed.Add(currentTargetRect);
            allowed.AddRange(currentSourceRects);
        }

        filter.SetAllowedAreas(allowed.ToArray());
    }
}