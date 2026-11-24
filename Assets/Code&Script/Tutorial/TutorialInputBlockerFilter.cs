using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Graphic))]
public class TutorialInputBlockerFilter : MonoBehaviour, ICanvasRaycastFilter
{
    private readonly List<RectTransform> allowedAreas = new List<RectTransform>();

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
                return false; 
        }
        return true; 
    }
}