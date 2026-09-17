using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to the SAME GameObject as your full-screen InputBlocker Image
/// (the one with "Raycast Target" checked). It lets you punch logical "holes"
/// in the blocker for specific RectTransforms (e.g. the current tutorial
/// target and the dialogue box) without needing to fight Hierarchy sibling
/// order or reparent things.
///
/// How it works: Unity calls IsRaycastLocationValid for every raycast that
/// hits this Graphic. Returning true means "yes, I am hit here" (blocks
/// whatever's underneath). Returning false means "let this click pass
/// through me" (whatever's underneath, e.g. your button, receives it).
/// </summary>
[RequireComponent(typeof(Graphic))]
public class TutorialInputBlockerFilter : MonoBehaviour, ICanvasRaycastFilter
{
    private readonly List<RectTransform> allowedAreas = new List<RectTransform>();

    /// <summary>Set which areas should be click-through right now. Call this every time the step changes.</summary>
    public void SetAllowedAreas(params RectTransform[] areas)
    {
        allowedAreas.Clear();
        foreach (var area in areas)
            if (area != null) allowedAreas.Add(area);
    }

    public void ClearAllowedAreas()
    {
        allowedAreas.Clear();
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        foreach (var area in allowedAreas)
        {
            if (area == null) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(area, screenPoint, eventCamera))
                return false; // inside an allowed area -> click passes through the blocker
        }
        return true; // everywhere else -> blocker intercepts the click
    }
}