using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dims the whole screen except a rectangular "spotlight" hole around the
/// current tutorial target. Also optionally blocks raycasts everywhere
/// outside that hole, so the player physically can't tap anything else.
///
/// Setup:
/// - Put this on a full-screen UI Image (Canvas child, stretched to fill).
/// - Assign a material using the included shader-free approach below:
///   we build the "hole" using four separate dimmed rectangles surrounding
///   the target instead of a shader, which keeps this dependency-free.
/// </summary>
public class TutorialHighlight : MonoBehaviour
{
    [Header("Refs - four dim panels surrounding the hole")]
    public RectTransform top;
    public RectTransform bottom;
    public RectTransform left;
    public RectTransform right;

    [Header("Optional: a visible ring/outline around the hole")]
    public RectTransform ringOutline;

    [Tooltip("Extra padding around the target rect, in pixels.")]
    public float padding = 12f;

    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        Hide();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>Position the four dim panels so they surround targetRect, leaving a see-through hole.</summary>
    public void ShowAround(RectTransform targetRect)
    {
        if (targetRect == null)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);

        // Get target's world corners, convert to local space of this overlay.
        Vector3[] worldCorners = new Vector3[4];
        targetRect.GetWorldCorners(worldCorners);

        RectTransform overlayRect = transform as RectTransform;
        Vector2 min = overlayRect.InverseTransformPoint(worldCorners[0]); // bottom-left
        Vector2 max = overlayRect.InverseTransformPoint(worldCorners[2]); // top-right

        min -= new Vector2(padding, padding);
        max += new Vector2(padding, padding);

        Rect overlayLocal = overlayRect.rect;

        // TOP panel: from hole's top edge to overlay's top edge, full width.
        SetPanel(top, overlayLocal.xMin, max.y, overlayLocal.xMax, overlayLocal.yMax);
        // BOTTOM panel: from overlay's bottom edge to hole's bottom edge, full width.
        SetPanel(bottom, overlayLocal.xMin, overlayLocal.yMin, overlayLocal.xMax, min.y);
        // LEFT panel: between top and bottom panels, left of hole.
        SetPanel(left, overlayLocal.xMin, min.y, min.x, max.y);
        // RIGHT panel: between top and bottom panels, right of hole.
        SetPanel(right, max.x, min.y, overlayLocal.xMax, max.y);

        if (ringOutline != null)
        {
            ringOutline.position = targetRect.position;
            ringOutline.sizeDelta = targetRect.rect.size + new Vector2(padding, padding) * 2f;
        }
    }

    private void SetPanel(RectTransform panel, float xMin, float yMin, float xMax, float yMax)
    {
        if (panel == null) return;
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.zero;
        panel.pivot = Vector2.zero;
        panel.anchoredPosition = new Vector2(xMin, yMin);
        panel.sizeDelta = new Vector2(Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
    }
}