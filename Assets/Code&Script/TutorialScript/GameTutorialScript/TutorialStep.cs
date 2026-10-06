using UnityEngine;

public enum GestureType
{
    None,
    Tap,
    TapRepeat,
    Drag,
    DragToTarget,
    HoldPour
}

/// <summary>
/// One step of a tutorial sequence. Create via Assets > Create > Tutorial > Step.
/// targetId / dropZoneId / allowedGroupId must match the IDs you set on
/// TutorialTarget / LockableGroup components in the scene.
/// </summary>
[CreateAssetMenu(fileName = "TutorialStep", menuName = "Tutorial/Step")]
public class TutorialStep : ScriptableObject
{
    [Header("NPC Dialogue")]
    [TextArea(3, 6)] public string npcDialogue;
    public Sprite npcPortrait;

    [Header("Targets (must match TutorialTarget id in scene)")]
    public string targetId;
    public string dropZoneId; // only needed for DragToTarget

    [Header("Locking")]
    [Tooltip("Matches a LockableGroup id. Leave blank to just lock everything except the target.")]
    public string allowedGroupId;

    [Header("Behaviour")]
    public GestureType gesture = GestureType.Tap;
    [Tooltip("If true, waits for ReportActionCompleted(targetId). If false, shows a Continue button instead.")]
    public bool waitForAction = true;
}
