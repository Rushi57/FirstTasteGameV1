using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC dialogue panel with a typewriter effect. Shows a Continue button only for
/// steps that don't require the player to perform an action first.
/// </summary>
public class DialogueBoxUI : MonoBehaviour
{
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button continueButton;
    [SerializeField] private float charDelay = 0.02f;

    private Coroutine typingRoutine;

    public void Show(string text, Sprite portrait)
    {
        gameObject.SetActive(true);
        if (portrait != null) portraitImage.sprite = portrait;
        continueButton.gameObject.SetActive(false);

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeText(text));
    }

    IEnumerator TypeText(string text)
    {
        dialogueText.text = "";
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(charDelay);
        }
    }

    public void ShowContinueButton(Action onContinue)
    {
        continueButton.gameObject.SetActive(true);
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() => onContinue());
    }

    public void HideContinueButton()
    {
        continueButton.gameObject.SetActive(false);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}