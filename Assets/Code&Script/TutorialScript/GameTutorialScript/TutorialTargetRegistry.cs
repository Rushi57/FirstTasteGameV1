using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central lookup so TutorialStep assets can reference scene objects by a string ID
/// instead of a direct RectTransform reference (which ScriptableObjects can't persist).
/// Add one of these to your Tutorial system GameObject in every scene that uses tutorials.
/// </summary>
public class TutorialTargetRegistry : MonoBehaviour
{
    public static TutorialTargetRegistry Instance;

    private readonly Dictionary<string, RectTransform> targets = new Dictionary<string, RectTransform>();

    void Awake()
    {
        Instance = this;
    }

    public void Register(string id, RectTransform rt)
    {
        if (string.IsNullOrEmpty(id)) return;
        targets[id] = rt;
    }

    public void Unregister(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        targets.Remove(id);
    }

    public RectTransform Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        targets.TryGetValue(id, out var rt);
        return rt;
    }
}