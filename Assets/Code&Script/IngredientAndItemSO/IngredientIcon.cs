using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the ingredient prefab (the one with the Image component).
/// Forces a fixed 85x85 size, scale 1, and Preserve Aspect so any sprite fits without stretching.
/// </summary>
[RequireComponent(typeof(Image))]
public class IngredientIcon : MonoBehaviour
{
    [SerializeField] private Vector2 iconSize = new Vector2(85f, 85f);

    private Image image;
    private RectTransform rect;

    private void Awake()
    {
        CacheComponents();
        ApplySettings();
    }

    // Reapplies settings in the editor when values change or the script is added.
    private void OnValidate()
    {
        CacheComponents();
        ApplySettings();
    }

    private void CacheComponents()
    {
        if (image == null) image = GetComponent<Image>();
        if (rect == null) rect = GetComponent<RectTransform>();
    }

    private void ApplySettings()
    {
        if (image == null || rect == null) return;

        image.preserveAspect = true;
        rect.sizeDelta = iconSize;
        rect.localScale = Vector3.one;
    }

    /// <summary>
    /// Call this when you spawn the prefab, e.g. icon.SetSprite(onionSprite);
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        CacheComponents();
        image.sprite = sprite;
        ApplySettings();
    }
}