using UnityEngine;

/// <summary>
/// The tutorial sequencer. Drive it by calling StartTutorial(). Each mechanic script
/// (cutting, mixing, pouring, DraggableIngredient, etc.) should call
/// TutorialManager.Instance.ReportActionCompleted(theirId) when the player successfully
/// performs the expected action - see the setup steps for wiring this into your
/// existing mechanic scripts.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [SerializeField] private TutorialStep[] steps;
    [SerializeField] private DialogueBoxUI dialogueBox;
    [SerializeField] private SpotlightOverlay spotlight;
    [SerializeField] private TutorialInputBlocker blocker;
    [SerializeField] private TutorialHintAnimator hintAnimator;

    private int currentIndex = -1;

    public bool IsRunning => currentIndex >= 0 && currentIndex < steps.Length;

    void Awake()
    {
        Instance = this;
    }

    public void StartTutorial()
    {
        currentIndex = -1;
        AdvanceStep();
    }

    void AdvanceStep()
    {
        currentIndex++;
        hintAnimator.Stop();

        if (currentIndex >= steps.Length)
        {
            EndTutorial();
            return;
        }

        TutorialStep step = steps[currentIndex];

        RectTransform target = TutorialTargetRegistry.Instance.Get(step.targetId);
        RectTransform dropZone = string.IsNullOrEmpty(step.dropZoneId)
            ? null
            : TutorialTargetRegistry.Instance.Get(step.dropZoneId);

        dialogueBox.Show(step.npcDialogue, step.npcPortrait);

        if (target != null)
        {
            spotlight.Focus(target);
            if (dropZone != null) spotlight.ShowSecondaryHighlight(dropZone);
        }
        else
        {
            spotlight.Clear();
        }

        if (!string.IsNullOrEmpty(step.allowedGroupId))
            InteractionLock.Instance.LockAllExcept(step.allowedGroupId);
        else
            InteractionLock.Instance.LockAllExcept(string.Empty); // lock everything, target still gets through via blocker

        blocker.SetAllowedTarget(target);

        switch (step.gesture)
        {
            case GestureType.Tap:
            case GestureType.TapRepeat:
                hintAnimator.PlayTapHint(target);
                break;
            case GestureType.Drag:
            case GestureType.DragToTarget:
                if (target != null && dropZone != null)
                    hintAnimator.PlayDragHint(target.position, dropZone.position);
                break;
        }

        if (!step.waitForAction)
            dialogueBox.ShowContinueButton(AdvanceStep);
        else
            dialogueBox.HideContinueButton();
    }

    /// <summary>
    /// Call this from any mechanic script when the player completes the expected
    /// action. sourceId must match the current step's targetId.
    /// </summary>
    public void ReportActionCompleted(string sourceId)
    {
        if (!IsRunning) return;
        if (steps[currentIndex].targetId == sourceId)
            AdvanceStep();
    }

    void EndTutorial()
    {
        dialogueBox.Hide();
        spotlight.Clear();
        blocker.SetAllowedTarget(null);
        InteractionLock.Instance.UnlockAll();
        currentIndex = -1;
    }
}