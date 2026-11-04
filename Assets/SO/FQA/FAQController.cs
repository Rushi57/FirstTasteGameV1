using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FAQController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text dialogText;      
    [SerializeField] Image dishImage;          
    [SerializeField] GameObject yesNoRoot;     
    [SerializeField] Button yesButton;
    [SerializeField] Button noButton;
    [SerializeField] Button tapAnywhere;       

    [Header("Typing")]
    [SerializeField] float charsPerSecond = 40f;

    [Header("Answer Paging")]
    [SerializeField] bool keepCommas = true;   

    public event Action OnFAQFinished;

    enum State { Idle, Typing, Choosing, WaitingForTap }

    FAQEntry[] entries;
    int index = -1;
    State state = State.Idle;
    Coroutine typingRoutine;
    Action onTypingDone;

    readonly List<string> answerPages = new List<string>();
    int answerPageIndex = -1;
    bool inAnswer;

    void Awake()
    {
        tapAnywhere.onClick.AddListener(OnTap);
        yesButton.onClick.AddListener(OnYes);
        noButton.onClick.AddListener(OnNo);
    }

  
    public void Play(LevelData level)
    {
        if (level == null || level.faqEntries == null || level.faqEntries.Length == 0)
        {
            Finish();
            return;
        }

        entries = level.faqEntries;
        dishImage.sprite = level.faqDishImage != null ? level.faqDishImage : level.levelPreviewImage;

        index = -1;
        inAnswer = false;
        gameObject.SetActive(true);
        ShowNext();
    }

    void ShowNext()
    {
        inAnswer = false;
        index++;
        if (index >= entries.Length) { Finish(); return; }

        FAQEntry entry = entries[index];
        yesNoRoot.SetActive(false);

        StartTyping(entry.text, () =>
        {
            if (entry.type == FAQEntryType.Question) ShowChoices();
            else EnterWaitForTap();
        });
    }

    void ShowChoices()
    {
        state = State.Choosing;
        tapAnywhere.gameObject.SetActive(false); 
        yesNoRoot.SetActive(true);
    }

    void EnterWaitForTap()
    {
        state = State.WaitingForTap;
        tapAnywhere.gameObject.SetActive(true);
    }

    void OnYes()
    {
        if (state != State.Choosing) return;
        yesNoRoot.SetActive(false);
        tapAnywhere.gameObject.SetActive(true);
        ShowNext();
    }

    void OnNo() 
    {
        if (state != State.Choosing) return;
        yesNoRoot.SetActive(false);
        tapAnywhere.gameObject.SetActive(true);

        BuildAnswerPages(entries[index].answer);
        answerPageIndex = -1;
        inAnswer = true;
        ShowNextAnswerPage();
    }

    void OnTap()
    {
        if (state == State.Typing) SkipTyping();
        else if (state == State.WaitingForTap)
        {
            if (inAnswer) ShowNextAnswerPage();
            else ShowNext();
        }
    }

    
    void ShowNextAnswerPage()
    {
        answerPageIndex++;
        if (answerPageIndex >= answerPages.Count)
        {
            inAnswer = false;
            ShowNext();          
            return;
        }
        StartTyping(answerPages[answerPageIndex], EnterWaitForTap);
    }

    void BuildAnswerPages(string answer)
    {
        answerPages.Clear();
        if (string.IsNullOrWhiteSpace(answer)) return;

        string[] chunks = answer.Split(',');
        for (int i = 0; i < chunks.Length; i++)
        {
            string chunk = chunks[i].Trim();
            if (chunk.Length == 0) continue;
            if (keepCommas && i < chunks.Length - 1) chunk += ",";
            SplitToFit(chunk);
        }
    }
    void SplitToFit(string chunk)
    {
        if (Fits(chunk)) { answerPages.Add(chunk); return; }

        string current = "";
        foreach (string word in chunk.Split(' '))
        {
            string candidate = current.Length == 0 ? word : current + " " + word;
            if (Fits(candidate)) current = candidate;
            else
            {
                if (current.Length > 0) answerPages.Add(current);
                current = word;
            }
        }
        if (current.Length > 0) answerPages.Add(current);
    }

    bool Fits(string s)
    {
        Vector2 size = dialogText.rectTransform.rect.size;
        Vector4 m = dialogText.margin;
        float w = size.x - m.x - m.z;
        float h = size.y - m.y - m.w;
        return dialogText.GetPreferredValues(s, w, h).y <= h + 0.5f;
    }
    void StartTyping(string content, Action onDone)
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        onTypingDone = onDone;
        state = State.Typing;
        typingRoutine = StartCoroutine(TypeRoutine(content));
    }

    IEnumerator TypeRoutine(string content)
    {
        dialogText.text = content;
        dialogText.maxVisibleCharacters = 0;
        dialogText.ForceMeshUpdate();
        int total = dialogText.textInfo.characterCount;

        float shown = 0f;
        while (dialogText.maxVisibleCharacters < total)
        {
            shown += charsPerSecond * Time.unscaledDeltaTime;
            dialogText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
            yield return null;
        }
        CompleteTyping();
    }

    void SkipTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        dialogText.maxVisibleCharacters = int.MaxValue;
        CompleteTyping();
    }

    void CompleteTyping()
    {
        typingRoutine = null;
        var cb = onTypingDone;
        onTypingDone = null;
        cb?.Invoke();
    }

    void Finish()
    {
        state = State.Idle;
        inAnswer = false;
        gameObject.SetActive(false);
        OnFAQFinished?.Invoke();
    }
}