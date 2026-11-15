using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryTeller : MonoBehaviour
{



    [Header("UI")]
    public TMP_Text storyText;
    public Button nextButton;
    public Image characterImage;
    public Image dishImage;
    public Button tapAnywhereButton;

    [Header("Panels")]
    public GameObject storyPanel; 
    public GameObject gamePanel;   

    [Header("Story")]
    public DishStorySO fallbackStory;
    public float typeSpeed = 0.03f;

    [Header("Text fitting")]
    public float minFontSize = 42f;
    public float maxFontSize = 50f;
    public Vector2 textBoxSize = new Vector2(701f, 165f);   

    [Header("Enable after story")]
    public GameObject[] enableAfterStory;
    [Header("After story")]
    public TutorialManager tutorialManager;


    class Page
    {
        public string text;
        public Sprite sprite;
    }

    readonly List<Page> pages = new List<Page>();

    DishStorySO currentStory;
    int index;
    bool isTyping;
    bool isReplay;            
    Coroutine typingRoutine;

    void Awake()
    {
        Debug.Log("[StoryTeller] AWAKE");
        Time.timeScale = 1f;
        if (characterImage != null) characterImage.preserveAspect = true;
        if (dishImage != null) dishImage.preserveAspect = true;
        ConfigureText();

        bool seen = HasSeenStory();
        if (storyPanel != null) storyPanel.SetActive(!seen);
        if (gamePanel != null) gamePanel.SetActive(seen);
    }

    void Start()
    {
        LevelData level = LevelSelectionManager.SelectedLevel;
        currentStory = (level != null && level.story != null) ? level.story : fallbackStory;

        if (tapAnywhereButton != null) tapAnywhereButton.onClick.AddListener(OnNext);

        Debug.Log($"[StoryTeller] level={(level != null ? level.name : "NULL")}, " +
             $"story={(currentStory != null ? currentStory.name : "NULL")}, " +
             $"lines={(currentStory != null ? currentStory.lines.Length : 0)}, " +
             $"seen={HasSeenStory()}, key={StoryKey()}");

        if (currentStory == null || currentStory.lines.Length == 0)
        {
            Debug.Log("[StoryTeller] No story data -> skipping to gameplay");
            ShowGameplay(startTutorial: true);
            return;
        }

        BuildPages();

        if (pages.Count == 0)
        {
            Debug.Log("[StoryTeller] Story has no text -> skipping to gameplay");
            ShowGameplay(startTutorial: true);
            return;
        }

        SetupDishImage();

        if (HasSeenStory())
        {
         
            ShowGameplay(startTutorial: true);
        }
        else
        {
        
            isReplay = false;
            OpenStoryPanel();
        }
    }

    // ---------- Called by HistoryBtn OnClick ----------
    public void ReplayStory()
    {
        if (currentStory == null || pages.Count == 0) return;
        isReplay = true;
        Time.timeScale = 0f;  
        OpenStoryPanel();
    }

    string StoryKey()
    {
        LevelData level = LevelSelectionManager.SelectedLevel;
      
        string id = level != null ? level.name : "fallback";
        return "StorySeen_" + id;
    }

    bool HasSeenStory() => PlayerPrefs.GetInt(StoryKey(), 0) == 1;

    void MarkStorySeen()
    {
        PlayerPrefs.SetInt(StoryKey(), 1);
        RegisterFlag(StoryKey());
        PlayerPrefs.Save();
        Debug.Log("[StoryTeller] MARKED SEEN: " + StoryKey());
    }

    void MarkTutorialSeen()
    {
        PlayerPrefs.SetInt(TutorialKey(), 1);
        RegisterFlag(TutorialKey());
        PlayerPrefs.Save();
    }

    // ---------- Panels ----------
    void OpenStoryPanel()
    {
        if (storyPanel != null) storyPanel.SetActive(true);
        if (!isReplay && gamePanel != null) gamePanel.SetActive(false); 
        ShowLine(0);
    }

    void ShowGameplay(bool startTutorial)
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        isTyping = false;

        if (storyPanel != null) storyPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);
        Time.timeScale = 1f;

        foreach (var go in enableAfterStory)
            if (go != null) go.SetActive(true);

        if (startTutorial && tutorialManager != null)
            tutorialManager.StartTutorial();
    }

    void SetupDishImage()
    {
        if (dishImage == null) return;
        LevelData level = LevelSelectionManager.SelectedLevel;
        Sprite preview = level != null ? level.levelPreviewImage : null;

        dishImage.sprite = preview;
        dishImage.color = Color.white;
        dishImage.enabled = preview != null;
    }

    // ---------- Building pages from the SO ----------
    void BuildPages()
    {
        pages.Clear();
        foreach (var line in currentStory.lines)
        {
            foreach (string chunk in Slice(line.text))
                pages.Add(new Page { text = chunk, sprite = line.characterSprite });
        }
    }

    // ---------- Text fitting ----------
    void ConfigureText()
    {
        if (storyText == null) return;
        storyText.enableAutoSizing = true;
        storyText.fontSizeMin = minFontSize;
        storyText.fontSizeMax = maxFontSize;
    }

   
    bool Fits(string s)
    {
        float w = textBoxSize.x - storyText.margin.x - storyText.margin.z;
        float h = textBoxSize.y - storyText.margin.y - storyText.margin.w;

        bool prevAuto = storyText.enableAutoSizing;
        float prevSize = storyText.fontSize;

        storyText.enableAutoSizing = false;
        storyText.fontSize = minFontSize;
        Vector2 preferred = storyText.GetPreferredValues(s, w, h);

        storyText.enableAutoSizing = prevAuto;
        storyText.fontSize = prevSize;

        return preferred.y <= h;
    }


    List<string> Slice(string text)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text) || storyText == null) return result;

        string[] sentences = Regex.Split(text.Trim(), @"(?<=[.!?…])\s+");
        string current = "";

        foreach (string sentence in sentences)
        {
            string candidate = current.Length == 0 ? sentence : current + " " + sentence;
            if (Fits(candidate)) { current = candidate; continue; }

            if (current.Length > 0) { result.Add(current); current = ""; }

            if (Fits(sentence)) { current = sentence; continue; }

            // One sentence alone is too big -> split by words
            foreach (string word in sentence.Split(' '))
            {
                string cand = current.Length == 0 ? word : current + " " + word;
                if (current.Length == 0 || Fits(cand)) current = cand;
                else { result.Add(current); current = word; }
            }
        }
        if (current.Length > 0) result.Add(current);
        return result;
    }

    // ---------- Dialogue ----------
    void ShowLine(int i)
    {
        index = i;
        var page = pages[index];

        if (characterImage != null && page.sprite != null)
            characterImage.sprite = page.sprite;

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeText(page.text));
    }

    IEnumerator TypeText(string full)
    {
        isTyping = true;
        storyText.text = full;
        storyText.maxVisibleCharacters = 0;

        for (int i = 0; i <= full.Length; i++)
        {
            storyText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        isTyping = false;
    }

    void FinishTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        storyText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
    }


    void OnNext()
    {
        if (isTyping)
        {
            FinishTyping();
            return;
        }

        GoToLine(index + 1);
    }

    void GoToLine(int target)
    {
        if (target < pages.Count) ShowLine(target);
        else EndStory();
    }

    void EndStory()
    {
        if (isReplay)
        {
       
            isReplay = false;
            ShowGameplay(startTutorial: false);
            return;
        }

        MarkStorySeen();
        ShowGameplay(startTutorial: true);
    }

    // ---------- Reset support (call on New Game) ----------
    const string FlagIndexKey = "StoryFlagIndex";

    static void RegisterFlag(string key)
    {
        string index = PlayerPrefs.GetString(FlagIndexKey, "");
        if (!("|" + index + "|").Contains("|" + key + "|"))
        {
            index = string.IsNullOrEmpty(index) ? key : index + "|" + key;
            PlayerPrefs.SetString(FlagIndexKey, index);
        }
    }

    public static void ResetAllFlags()
    {
        string index = PlayerPrefs.GetString(FlagIndexKey, "");
        if (!string.IsNullOrEmpty(index))
            foreach (string key in index.Split('|'))
                PlayerPrefs.DeleteKey(key);

        for (int i = 1; i <= 10; i++)
        {
            PlayerPrefs.DeleteKey("StorySeen_Level" + i);
            PlayerPrefs.DeleteKey("TutorialSeen_Level" + i);
        }

        PlayerPrefs.DeleteKey(FlagIndexKey);
        PlayerPrefs.Save();
        Debug.Log("[StoryTeller] All story/tutorial flags reset");
    }

    // ---------- Tutorial seen flag (per dish) ----------
    string TutorialKey()
    {
        LevelData level = LevelSelectionManager.SelectedLevel;
        string id = level != null ? level.name : "fallback";
        return "TutorialSeen_" + id;
    }

    bool HasSeenTutorial() => PlayerPrefs.GetInt(TutorialKey(), 0) == 1;
}