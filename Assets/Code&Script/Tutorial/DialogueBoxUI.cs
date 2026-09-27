using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The visual NPC dialogue box. Handles typewriter text reveal, portrait,
/// name, and a "Next" button for lines that don't require a game action.
/// </summary>
public class DialogueBoxUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject root;              // whole dialogue box, enable/disable this
    public Image portraitImage;
    public TMP_Text nameText;
    public TMP_Text bodyText;
    public Button nextButton;
    public GameObject tapToContinueHint;  // optional "tap anywhere" hint

    [Header("Typewriter")]
    public float charsPerSecond = 40f;

    [Header("Adaptive Positioning")]
    [Tooltip("If true, the box automatically moves between Top Anchored Position and Bottom Anchored Position to avoid covering the current tutorial target.")]
    public bool adaptivePositioning = true;

    [Tooltip("anchoredPosition to use when placed at the top of the screen.")]
    public Vector2 topAnchoredPosition = new Vector2(0f, -100f);

    [Tooltip("anchoredPosition to use when placed at the bottom of the screen.")]
    public Vector2 bottomAnchoredPosition = new Vector2(0f, 100f);

    private Coroutine typingRoutine;
    private string currentFullLine;
    private bool isTyping;

    public event Action OnNextPressed;

    /// <summary>The dialogue box's own rect, so it can be exempted from the input blocker.</summary>
    public RectTransform RootRect => root != null ? root.transform as RectTransform : null;

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(HandleNextClicked);
        Hide();
    }

    public void Show(string npcName, Sprite portrait)
    {
        root.SetActive(true);
        if (nameText != null) nameText.text = npcName;
        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }
    }

    public void Hide()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        root.SetActive(false);
    }

    /// <summary>Show a line of dialogue with a typewriter effect. showNextButton controls whether the "Next" button appears once typing finishes.</summary>
    public void PlayLine(string line, bool showNextButton)
    {
        currentFullLine = line;
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeLine(line, showNextButton));
    }

    private IEnumerator TypeLine(string line, bool showNextButton)
    {
        isTyping = true;
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (tapToContinueHint != null) tapToContinueHint.SetActive(false);

        bodyText.text = "";
        float delay = 1f / Mathf.Max(1f, charsPerSecond);

        foreach (char c in line)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        if (showNextButton && nextButton != null)
            nextButton.gameObject.SetActive(true);
        if (showNextButton && tapToContinueHint != null)
            tapToContinueHint.SetActive(true);
    }

    /// <summary>Call this if you want a tap-anywhere-on-box behavior: skip typing, or advance.</summary>
    public void OnBoxTapped()
    {
        if (isTyping)
        {
            // Skip to full text instantly.
            StopCoroutine(typingRoutine);
            bodyText.text = currentFullLine;
            isTyping = false;
            if (nextButton != null) nextButton.gameObject.SetActive(true);
        }
        else
        {
            HandleNextClicked();
        }
    }

    private void HandleNextClicked()
    {
        OnNextPressed?.Invoke();
    }

    /// <summary>
    /// Positions the dialogue box according to the given DialoguePosition.
    /// Auto picks whichever preset (Top/Bottom) doesn't overlap target -
    /// Custom places it at the exact customPosition given, ignoring overlap.
    /// Safe to call with target == null under Auto (box just stays put).
    /// </summary>
    public void SetPosition(DialoguePosition position, RectTransform target, Vector2 customPosition)
    {
        if (!adaptivePositioning || RootRect == null) return;

        switch (position)
        {
            case DialoguePosition.Custom:
                RootRect.anchoredPosition = customPosition;
                break;
            case DialoguePosition.Auto:
            default:
                PositionAwayFrom(target);
                break;
        }
    }

    /// <summary>
    /// Moves the dialogue box to whichever preset position (top or bottom)
    /// does NOT overlap the given target - so the box never covers the
    /// button/item the player actually needs to tap or drag. Call this
    /// before showing a step's dialogue, once the target is known.
    /// Safe to call with target == null (just leaves the box where it is).
    /// </summary>
    public void PositionAwayFrom(RectTransform target)
    {
        if (!adaptivePositioning || target == null || RootRect == null) return;

        // Try top first - if it overlaps, fall back to bottom.
        RootRect.anchoredPosition = topAnchoredPosition;
        if (Overlaps(RootRect, target))
            RootRect.anchoredPosition = bottomAnchoredPosition;
    }

    private static bool Overlaps(RectTransform a, RectTransform b)
    {
        Rect rectA = GetScreenRect(a);
        Rect rectB = GetScreenRect(b);
        return rectA.Overlaps(rectB);
    }

    private static Rect GetScreenRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        // corners[0] = bottom-left, corners[2] = top-right
        return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
    }
}