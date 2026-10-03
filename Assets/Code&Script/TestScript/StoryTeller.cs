using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryTeller : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text storyText;
    public Button nextButton;
    public Image characterImage;   // Kris
    public Image dishImage;        // DishImage (the white box)

    [Header("Panels")]
    public GameObject storyPanel;  // BackgroudStotyTelling
    public GameObject gamePanel;   // BackGroundImage (all mini-games)

    [Header("Story")]
    public StoryLine[] lines;
    public float typeSpeed = 0.03f;

    [Header("Enable after story")]
    public GameObject[] enableAfterStory;   // TutorialSystemInGame, etc.
    [Header("After story")]
    public TutorialManager tutorialManager;
    int index;
    bool isTyping;
    Coroutine typingRoutine;

    [System.Serializable]
    public class StoryLine
    {
        [TextArea(2, 5)] public string text;
        public Sprite characterSprite;
    }

    void Awake()
    {
        Time.timeScale = 1f;
        // Story first, mini-games hidden so nothing runs behind it
        if (storyPanel != null) storyPanel.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(false);

        if (characterImage != null) characterImage.preserveAspect = true;
        if (dishImage != null) dishImage.preserveAspect = true;
        
    }

    void Start()
    {
        SetupDishImage();
        nextButton.onClick.AddListener(OnNext);
        ShowLine(0);
    }

    void SetupDishImage()
    {
        if (dishImage == null) return;

        // Same data LevelIconButton uses on the map
        LevelData level = LevelSelectionManager.SelectedLevel;   // <-- adjust to your API
        Sprite preview = level != null ? level.levelPreviewImage : null;

        dishImage.sprite = preview;
        dishImage.preserveAspect = true;
        dishImage.color = Color.white;
        dishImage.enabled = preview != null;   // hides the plain white box if no sprite
    }

    void ShowLine(int i)
    {
        index = i;
        var line = lines[index];

        if (characterImage != null && line.characterSprite != null)
        {
            characterImage.sprite = line.characterSprite;
            characterImage.preserveAspect = true;
        }

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
            yield return new WaitForSecondsRealtime(typeSpeed);   // was WaitForSeconds
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

        if (index + 1 < lines.Length) ShowLine(index + 1);
        else EndStory();
    }

    void EndStory()
    {
        if (storyPanel != null) storyPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);   // MessageBox now exists

        if (tutorialManager != null) tutorialManager.StartTutorial();
    }
}