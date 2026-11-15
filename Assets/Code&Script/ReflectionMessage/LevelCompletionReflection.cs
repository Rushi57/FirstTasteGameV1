using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LevelCompletionReflection : MonoBehaviour
{
    private enum Phase { StarMessage, FAQ, Reflection }

    [Header("UI References")]
    [SerializeField] private TMP_Text reflectionText;   
    [SerializeField] private Button continueBtn;       
    [SerializeField] private Image dishImage;         
 

    [Header("FAQ")]
    [SerializeField] private FAQController faqController; 

    [Header("Star messages (element 0 = 1 star, 1 = 2 stars, 2 = 3 stars)")]
    [SerializeField, TextArea(2, 4)]
    private string[] starMessages =
    {
        "Well, practice makes perfect! Why don't we give this dish another shot and see what we can come up with?",
        "Nicely done! You've got most of it figured out. Keep it up and we'll get an even better result on our next attempt!",
        "Excellent work! You've got quite the talent in the kitchen! I bet everyone at the family table would love a taste!"
    };

    [Header("Fallback (used if the selected level has no reflection)")]
    [SerializeField] private DishReflectionData fallback;

    [Header("Typing")]
    [SerializeField] private float charsPerSecond = 40f;

    [Header("Text fitting")]
    [SerializeField] private float minFontSize = 43f;
    [SerializeField] private float maxFontSize = 49f;
    [SerializeField] private Vector2 textBoxSize = new Vector2(709.37f, 138f);   

    [SerializeField] private GameObject replayOrGotToMap;

    [Header("Events")]
    public UnityEvent onFinished; 

    [Header("Hide when text finishes")]
    [SerializeField] private GameObject[] hideOnFinish;  

    [Header("Testing")]
    [Tooltip("Only used when no level was selected on the map (e.g. running LevelMapScene directly in the Editor).")]
    [SerializeField] private LevelData testLevel;

    [Header("Debug")]
    [Tooltip("0 = use the real score. 1-3 = force that star message (for testing).")]
    [SerializeField, Range(0, 3)] private int debugStarOverride = 0;

    private DishReflectionData current;
    private readonly List<string> pages = new List<string>();  
    private Phase phase;
    private int pageIndex;
    private bool isTyping;
    private Coroutine typeRoutine;

    private void Awake()
    {
        continueBtn.onClick.AddListener(OnContinue);
        ConfigureText();
    }

 
    private LevelData GetLevel()
    {
        var selected = LevelSelectionManager.SelectedLevel;
        return selected != null ? selected : testLevel;
    }

    public void Show()
    {
        Show(FindForSelectedLevel());
    }

 
    public void Show(DishReflectionData data)
    {
        current = data;

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        isTyping = false;
        if (faqController != null)
        {
            faqController.OnFAQFinished -= HandleFAQFinished;
            faqController.gameObject.SetActive(false); 
        }

    
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.SaveDishResult();

        if (dishImage != null)
        {
            var selected = GetLevel();
            if (current != null && current.dishSprite != null)
                dishImage.sprite = current.dishSprite;
            else if (selected != null && selected.recipe != null && selected.recipe.recipeIcon != null)
                dishImage.sprite = selected.recipe.recipeIcon; 
        }

        gameObject.SetActive(true);
        if (replayOrGotToMap != null)
            replayOrGotToMap.SetActive(false);

        foreach (var go in hideOnFinish)
            if (go != null) go.SetActive(true);

        ConfigureText();
        StartPhase(Phase.StarMessage);
    }

    private DishReflectionData FindForSelectedLevel()
    {
        var selected = GetLevel();
        if (selected != null && selected.reflection != null)
            return selected.reflection;
        return fallback;
    }

    // ---------- Phases ----------
    private void StartPhase(Phase next)
    {
        phase = next;

        switch (next)
        {
            case Phase.StarMessage:
                pages.Clear();
                pages.AddRange(Slice(GetStarMessage()));
                BeginPages();
                break;

            case Phase.FAQ:
                StartFAQ();
                break;

            case Phase.Reflection:
                pages.Clear();
                if (current != null && current.pages != null)
                    foreach (string page in current.pages)
                        pages.AddRange(Slice(page));
                BeginPages();
                break;
        }
    }

    private void AdvancePhase()
    {
        switch (phase)
        {
            case Phase.StarMessage: StartPhase(Phase.FAQ); break;
            case Phase.FAQ: StartPhase(Phase.Reflection); break;
            case Phase.Reflection: FinishAll(); break;
        }
    }

    private void BeginPages()
    {
        if (pages.Count == 0)  
        {
            AdvancePhase();
            return;
        }
        pageIndex = 0;
        ShowPage();
    }

    private void StartFAQ()
    {
        var level = GetLevel();

        if (faqController == null)
        {
            Debug.LogWarning("[Reflection] Faq Controller is not assigned - skipping FAQ.");
            AdvancePhase(); return;
        }
        if (level == null)
        {
            Debug.LogWarning("[Reflection] No level found (SelectedLevel is null and Test Level is empty) - skipping FAQ.");
            AdvancePhase(); return;
        }
        if (level.faqEntries == null || level.faqEntries.Length == 0)
        {
            Debug.LogWarning($"[Reflection] '{level.name}' has no FAQ entries - skipping FAQ.");
            AdvancePhase(); return;
        }
        if (faqController.gameObject == gameObject)
        {
            Debug.LogError("[Reflection] FAQController must be on FAQGame, not on this object.");
            AdvancePhase(); return;
        }

        faqController.OnFAQFinished -= HandleFAQFinished;
        faqController.OnFAQFinished += HandleFAQFinished;
        faqController.Play(level);
    }

    private void HandleFAQFinished()
    {
        faqController.OnFAQFinished -= HandleFAQFinished;
        if (phase == Phase.FAQ) AdvancePhase();
    }

    private void FinishAll()
    {
        foreach (var go in hideOnFinish)
            if (go != null) go.SetActive(false);

        if (replayOrGotToMap != null)
            replayOrGotToMap.SetActive(true);

        onFinished?.Invoke();
    }

    // ---------- Star message ----------
    private int GetStarCount()
    {
        if (debugStarOverride > 0) return debugStarOverride;
        return ScoreManager.Instance != null ? ScoreManager.Instance.GetStarRating() : 1;
    }

    private string GetStarMessage()
    {
        int stars = Mathf.Clamp(GetStarCount(), 1, 3);
        if (starMessages != null && starMessages.Length >= stars && !string.IsNullOrWhiteSpace(starMessages[stars - 1]))
            return starMessages[stars - 1];
        return "";
    }

    // ---------- Text fitting ----------
    private void ConfigureText()
    {
        if (reflectionText == null) return;
        reflectionText.enableAutoSizing = true;
        reflectionText.fontSizeMin = minFontSize;
        reflectionText.fontSizeMax = maxFontSize;
    }

    private bool Fits(string s)
    {
        float w = textBoxSize.x - reflectionText.margin.x - reflectionText.margin.z;
        float h = textBoxSize.y - reflectionText.margin.y - reflectionText.margin.w;

        bool prevAuto = reflectionText.enableAutoSizing;
        float prevSize = reflectionText.fontSize;

        reflectionText.enableAutoSizing = false;
        reflectionText.fontSize = minFontSize;
        Vector2 preferred = reflectionText.GetPreferredValues(s, w, h);

        reflectionText.enableAutoSizing = prevAuto;
        reflectionText.fontSize = prevSize;

        return preferred.y <= h;
    }

    private List<string> Slice(string text)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text) || reflectionText == null) return result;

        string[] sentences = Regex.Split(text.Trim(), @"(?<=[.!?\u2026])\s+");
        string cur = "";

        foreach (string sentence in sentences)
        {
            string candidate = cur.Length == 0 ? sentence : cur + " " + sentence;
            if (Fits(candidate)) { cur = candidate; continue; }

            if (cur.Length > 0) { result.Add(cur); cur = ""; }

            if (Fits(sentence)) { cur = sentence; continue; }

            // One sentence alone is too big -> split by words
            foreach (string word in sentence.Split(' '))
            {
                string cand = cur.Length == 0 ? word : cur + " " + word;
                if (cur.Length == 0 || Fits(cand)) cur = cand;
                else { result.Add(cur); cur = word; }
            }
        }
        if (cur.Length > 0) result.Add(cur);
        return result;
    }

    // ---------- Paging / typing ----------
    private void ShowPage()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeText(pages[pageIndex]));
    }

    private IEnumerator TypeText(string full)
    {
        isTyping = true;
        reflectionText.text = full;
        reflectionText.maxVisibleCharacters = 0;
        reflectionText.ForceMeshUpdate();

        if (reflectionText.isTextOverflowing)
            Debug.LogWarning($"[LevelCompletionReflection] Page {pageIndex + 1} ({phase}) still overflows the text box " +
                             $"({full.Length} chars). Check Text Box Size matches the TMP Rect.");

        int total = reflectionText.textInfo.characterCount;
        float delay = 1f / charsPerSecond;

        for (int i = 1; i <= total; i++)
        {
            reflectionText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(delay);
        }
        isTyping = false;
    }

    private void OnContinue()
    {
        if (phase == Phase.FAQ) return;   

        if (isTyping)
        {
            StopCoroutine(typeRoutine);
            reflectionText.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
            return;
        }

        pageIndex++;
        if (pageIndex < pages.Count)
            ShowPage();
        else
            AdvancePhase();
    }
}