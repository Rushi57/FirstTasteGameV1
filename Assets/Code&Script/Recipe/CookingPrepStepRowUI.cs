using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CookingPrepStepRowUI : MonoBehaviour
{
    public TMP_Text label;

    [Tooltip("Optional: the row's background Image (the dark brown bar). Leave empty to only gray the text.")]
    public Image background;

    [Tooltip("Prefix shown before each step's text, e.g. a bullet character.")]
    public string bulletPrefix = "• ";

    [Header("Completed Look")]
    public Color completedBackground = new Color(0.45f, 0.45f, 0.45f, 1f);
    public Color completedTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public bool strikethroughWhenCompleted = true;

    private Color originalBackground;
    private Color originalTextColor;
    private bool originalsCached;

    private void Awake()
    {
        CacheOriginals();
    }

    private void CacheOriginals()
    {
        if (originalsCached) return;

        if (background != null) originalBackground = background.color;
        if (label != null) originalTextColor = label.color;

        originalsCached = true;
    }

    public void SetText(string instruction)
    {
        if (label != null)
            label.text = bulletPrefix + instruction;
    }

    public void SetCompleted(bool done)
    {
        CacheOriginals();

        if (background != null)
            background.color = done ? completedBackground : originalBackground;

        if (label != null)
        {
            label.color = done ? completedTextColor : originalTextColor;

            label.fontStyle = (done && strikethroughWhenCompleted)
                ? FontStyles.Strikethrough
                : FontStyles.Normal;
        }
    }
}