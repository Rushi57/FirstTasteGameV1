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

    [Header("Required Player Action")]
    public TutorialActionType actionType = TutorialActionType.None;

    [Tooltip("Must match the 'Interactable Id' on the TutorialInteractable this step targets. Leave empty if actionType is None.")]
    public string targetId;

    [Header("Highlight")]
    [Tooltip("If true, dim the screen and cut a spotlight hole around the target's RectTransform.")]
    public bool highlightTarget = true;

    [Tooltip("If true, block all input except the target while this step is active.")]
    public bool blockOtherInput = true;
}