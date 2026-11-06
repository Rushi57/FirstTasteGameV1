using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class FullScreenBackground : MonoBehaviour
{
    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private Image image;

    private Vector2 lastCanvasSize;

    private void OnEnable()
    {
        Setup();
        Fit();
    }

    private void Setup()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (canvasRect == null) { Setup(); }
        if (canvasRect == null) return;

        // Only refit when the canvas size changes (rotation, resolution change)
        if (canvasRect.rect.size != lastCanvasSize)
            Fit();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled) Fit();
    }

    private void Fit()
    {
        if (rectTransform == null || canvasRect == null || image == null || image.sprite == null)
            return;

        Vector2 canvasSize = canvasRect.rect.size;
        lastCanvasSize = canvasSize;

        // Center-anchored so we control size directly
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        Vector2 spriteSize = image.sprite.rect.size;
        float spriteAspect = spriteSize.x / spriteSize.y;
        float canvasAspect = canvasSize.x / canvasSize.y;

        Vector2 finalSize;
        if (canvasAspect > spriteAspect)
        {
            // Screen is wider than the image: match width, crop top/bottom
            finalSize = new Vector2(canvasSize.x, canvasSize.x / spriteAspect);
        }
        else
        {
            // Screen is taller than the image: match height, crop left/right
            finalSize = new Vector2(canvasSize.y * spriteAspect, canvasSize.y);
        }

        rectTransform.sizeDelta = finalSize;
    }
}