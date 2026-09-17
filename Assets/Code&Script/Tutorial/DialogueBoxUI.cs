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

    private Coroutine typingRoutine;
    private string currentFullLine;
    private bool isTyping;

    public event Action OnNextPressed;

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
}