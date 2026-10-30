using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    RectTransform rectTransform;
    Canvas canvas;
    Rect lastSafeArea;
    Vector2Int lastScreenSize;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        Rect safe = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);

        if (safe != lastSafeArea || size != lastScreenSize)
        {
            lastSafeArea = safe;
            lastScreenSize = size;
            ApplySafeArea(safe);
        }
    }

    void ApplySafeArea(Rect safe)
    {
        RectTransform parent = rectTransform.parent as RectTransform;
        if (parent == null) return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, safe.min, cam, out Vector2 localMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, safe.max, cam, out Vector2 localMax);

        Rect pr = parent.rect;
        rectTransform.anchorMin = Rect.PointToNormalized(pr, localMin);
        rectTransform.anchorMax = Rect.PointToNormalized(pr, localMax);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Debug.Log($"SafeArea: screen {Screen.width}x{Screen.height}, safe {safe}");
    }
}