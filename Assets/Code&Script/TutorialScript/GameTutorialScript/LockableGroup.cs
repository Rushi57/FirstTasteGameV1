using UnityEngine;

/// <summary>
/// Drop this on any panel that should be lockable as a whole during tutorials
/// (ingredient shelf, recipe list, pause button, HUD, etc). Requires a CanvasGroup
/// on the same object. Give it a unique groupId and use that in TutorialStep's
/// Allowed Group Id field to whitelist it for a given step.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class LockableGroup : MonoBehaviour
{
    [SerializeField] private string groupId;

    void Awake()
    {
        InteractionLock.Instance?.Register(groupId, GetComponent<CanvasGroup>());
    }
}