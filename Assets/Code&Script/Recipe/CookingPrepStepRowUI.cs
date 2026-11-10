using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CookingPrepStepRowUI : MonoBehaviour
{
    public TMP_Text label;

    [Tooltip("Optional: the row's background Image (the dark brown bar). Leave empty to only gray the text.")]
    public Image background;

    [Tooltip("The green check object (CheckImage) inside the CheckBox. Shown only when the step is done.")]
    public GameObject checkMark;

    [Header("Prefix")]
    [Tooltip("Used when the row is NOT numbered (e.g. the Ingredient Cut list).")]
    public string bulletPrefix = "• ";

    [Tooltip("Format for numbered rows. {0} = the step number.")]
    public string numberFormat = "{0}. ";

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

    // Existing calls keep working: bullet is used.
    public void SetText(string instruction)
    {
        if (label != null)
            label.text = bulletPrefix + instruction;
    }

    // New: pass a 1-based step number to get "1. text", "2. text", ...
    public void SetText(string instruction, int stepNumber)
    {
        if (label != null)
            label.text = string.Format(numberFormat, stepNumber) + instruction;
    }

    public void SetCompleted(bool done)
    {
        CacheOriginals();

        if (checkMark != null)
            checkMark.SetActive(done);

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