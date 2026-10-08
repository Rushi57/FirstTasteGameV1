using System.Collections;
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

    [Header("Panels")]
    public GameObject storyPanel;  // BackgroudStoryTelling
    public GameObject gamePanel;   // BackGroundImage (all mini-games)

    [Header("Story")]
    public DishStorySO fallbackStory;
    public float typeSpeed = 0.03f;

    [Header("Enable after story")]
    public GameObject[] enableAfterStory;
    [Header("After story")]
    public TutorialManager tutorialManager;

    DishStorySO currentStory;
    int index;
    bool isTyping;
    bool isReplay;              // true when opened from HistoryBtn
    Coroutine typingRoutine;

    void Awake()
    {
        Debug.Log("[StoryTeller] AWAKE");
        Time.timeScale = 1f;
        if (characterImage != null) characterImage.preserveAspect = true;
        if (dishImage != null) dishImage.preserveAspect = true;

        bool seen = HasSeenStory();
        if (storyPanel != null) storyPanel.SetActive(!seen);
        if (gamePanel != null) gamePanel.SetActive(seen);
    }

    void Start()
    {
       
        LevelData level = LevelSelectionManager.SelectedLevel;
        currentStory = (level != null && level.story != null) ? level.story : fallbackStory;

        nextButton.onClick.AddListener(OnNext);   // add ONCE here

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

        SetupDishImage();

        if (HasSeenStory())
        {
            // Not first time for this dish -> go straight to gameplay
            ShowGameplay(startTutorial: true);
        }
        else
        {
            // First time for this dish -> story is mandatory
            isReplay = false;
            OpenStoryPanel();
        }
    }

    // ---------- Called by HistoryBtn OnClick ----------
    public void ReplayStory()
    {
        if (currentStory == null || currentStory.lines.Length == 0) return;
        isReplay = true;
        Time.timeScale = 0f;   // pause gameplay while reading (typing uses realtime)
        OpenStoryPanel();
    }

    // ---------- Seen flag (per dish) ----------
    string StoryKey()
    {
        LevelData level = LevelSelectionManager.SelectedLevel;
        // Use a unique id. If LevelData has a levelId/dishName field, use that instead of .name
        string id = level != null ? level.name : "fallback";
        return "StorySeen_" + id;
    }

    bool HasSeenStory() => PlayerPrefs.GetInt(StoryKey(), 0) == 1;

    void MarkStorySeen()
    {
        PlayerPrefs.SetInt(StoryKey(), 1);
        PlayerPrefs.Save();
        Debug.Log("[StoryTeller] MARKED SEEN: " + StoryKey());
    }

    // ---------- Panels ----------
    void OpenStoryPanel()
    {
        if (storyPanel != null) storyPanel.SetActive(true);
        if (!isReplay && gamePanel != null) gamePanel.SetActive(false); // first time: hide games
        ShowLine(0);
    }

    void ShowGameplay(bool startTutorial)
    {
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

    // ---------- Dialogue ----------
    void ShowLine(int i)
    {
        index = i;
        var line = currentStory.lines[index];

        if (characterImage != null && line.characterSprite != null)
            characterImage.sprite = line.characterSprite;

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeText(line.text));
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

    void OnNext()
    {
        if (isTyping)
        {
            StopCoroutine(typingRoutine);
            storyText.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
            return;
        }

        if (index + 1 < currentStory.lines.Length) ShowLine(index + 1);
        else EndStory();
    }

    void EndStory()
    {
        if (isReplay)
        {
            // Just close the story and resume; no tutorial restart
            isReplay = false;
            ShowGameplay(startTutorial: false);
            return;
        }

        MarkStorySeen();
        ShowGameplay(startTutorial: true);
    }

}