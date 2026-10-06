using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coarse-grained lock: disables every registered CanvasGroup except the one whose
/// groupId matches the current tutorial step's Allowed Group Id. Use this for whole
/// panels (recipe list, pause button, ingredient shelf) that should be completely
/// untouchable while a tutorial step targets something else.
/// </summary>
public class InteractionLock : MonoBehaviour
{
    public static InteractionLock Instance;

    private readonly Dictionary<string, CanvasGroup> groups = new Dictionary<string, CanvasGroup>();

    void Awake()
    {
        Instance = this;
    }

    public void Register(string id, CanvasGroup group)
    {
        if (string.IsNullOrEmpty(id)) return;
        groups[id] = group;
    }

    public void LockAllExcept(string allowedId)
    {
        foreach (var kvp in groups)
        {
            bool allowed = kvp.Key == allowedId;
            kvp.Value.interactable = allowed;
            kvp.Value.blocksRaycasts = allowed;
        }
    }

    public void UnlockAll()
    {
        foreach (var kvp in groups)
        {
            kvp.Value.interactable = true;
            kvp.Value.blocksRaycasts = true;
        }
    }
}