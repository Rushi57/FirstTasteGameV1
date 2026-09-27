using UnityEngine;

/// <summary>
/// What kind of player action completes this step.
/// None = just dialogue, advance via the "Next" button.
/// </summary>
public enum TutorialActionType
{
    None,
    Tap,
    Drag
}

/// <summary>Where the dialogue box should sit for this step.</summary>
public enum DialoguePosition
{
    [InspectorName("Auto (avoid overlapping target)")]
    Auto,
    Custom
}

/// <summary>
/// Data-only description of one tutorial step. Create these as assets:
/// Right click in Project window -> Create -> Tutorial -> Step
/// </summary>
[CreateAssetMenu(fileName = "TutorialStep", menuName = "Tutorial/Step")]
public class TutorialStep : ScriptableObject
{
    [Header("NPC Dialogue")]
    public string npcName = "Guide";
    public Sprite npcPortrait;

    [TextArea(2, 5)]
    public string[] dialogueLines;

    [Tooltip("Where the dialogue box should be placed for this step. Auto picks whichever of the box's two preset positions (Top/Bottom on DialogueBoxUI) doesn't overlap the target. Custom uses the exact Custom Position below instead.")]
    public DialoguePosition dialoguePosition = DialoguePosition.Auto;

    [Tooltip("Used only when Dialogue Position is set to Custom - the exact anchoredPosition (Pos X, Pos Y) to place the dialogue box at for this step.")]
    public Vector2 customPosition = Vector2.zero;

    [Header("Required Player Action")]
    public TutorialActionType actionType = TutorialActionType.None;

    [Tooltip("Must match the 'Interactable Id' on the TutorialInteractable this step targets (e.g. the drop zone for Drag steps). Leave empty if actionType is None.")]
    public string targetId;

    [Tooltip("DRAG STEPS ONLY: must match the 'Interactable Id' on the TutorialInteractable attached to the draggable item itself, so it stays clickable/draggable while input is blocked. Leave empty for Tap steps.")]
    public string dragSourceId;

    [Header("Input")]
    [Tooltip("If true, block all input except the target and dialogue box while this step is active.")]
    public bool blockOtherInput = true;
}