using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueBoxUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject root;              
    public Image portraitImage;          
    public Image messageBorderImage;     
    public TMP_Text nameText;
    public TMP_Text bodyText;
    public GameObject tapToContinueHint; 
    public Button tapAnywhereButton;

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
    private bool currentShowNext;

    public event Action OnNextPressed;

    public RectTransform RootRect => root != null ? root.transform as RectTransform : null;
    public RectTransform TapAnywhereRect =>
        tapAnywhereButton != null ? tapAnywhereButton.transform as RectTransform : null;
    private void Awake()
    {
        if (portraitImage != null) portraitImage.preserveAspect = true;
        if (messageBorderImage != null) messageBorderImage.preserveAspect = true;
        if (tapAnywhereButton != null)
        {
            tapAnywhereButton.onClick.AddListener(OnBoxTapped);
            tapAnywhereButton.gameObject.SetActive(false);
        }
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
        isTyping = false;
        SetTapAnywhere(false);
        root.SetActive(false);
    }

    public void PlayLine(string line, bool showNextButton)
    {
        currentFullLine = line;
        currentShowNext = showNextButton;
        SetTapAnywhere(showNextButton);

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeLine(line, showNextButton));
    }

    private void SetTapAnywhere(bool active)
    {
        if (tapAnywhereButton != null)
            tapAnywhereButton.gameObject.SetActive(active);
    }

    private IEnumerator TypeLine(string line, bool showNextButton)
    {
        isTyping = true;
        if (tapToContinueHint != null) tapToContinueHint.SetActive(false);

        bodyText.text = "";
        float delay = 1f / Mathf.Max(1f, charsPerSecond);

        foreach (char c in line)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        if (showNextButton && tapToContinueHint != null)
            tapToContinueHint.SetActive(true);
    }

    public void OnBoxTapped()
    {
        if (!currentShowNext) return;  

        if (isTyping)
        {
           
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            bodyText.text = currentFullLine;
            isTyping = false;
            if (tapToContinueHint != null) tapToContinueHint.SetActive(true);
        }
        else
        {
            OnNextPressed?.Invoke();
        }
    }

   
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

    private void OnDestroy()
    {
        if (tapAnywhereButton != null) tapAnywhereButton.onClick.RemoveListener(OnBoxTapped);
    }
}