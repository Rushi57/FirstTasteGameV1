using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Reusable level-completion reflection dialog.
/// Picks a DishReflectionData by the selected level, pages the text, types it out,
/// and fires onFinished after the last page.
public class LevelCompletionReflection : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text reflectionText;   // text inside DialogueBox
    [SerializeField] private Button continueBtn;        // Continue button
    [SerializeField] private Image dishImage;           // RecipeImageHolder's image
    // StarHolder has StarRatingDisplay on it. It reads ScoreManager by itself in OnEnable.

    [Header("Fallback (used if the selected level has no reflection)")]
    [SerializeField] private DishReflectionData fallback;

    [Header("Typing")]
    [SerializeField] private float charsPerSecond = 40f;

    [SerializeField] private GameObject replayOrGotToMap;

    [Header("Events")]
    public UnityEvent onFinished;   // e.g. load LevelMapScene

    [Header("Hide when text finishes")]
    [SerializeField] private GameObject[] hideOnFinish;   // DialogueBox, ContinueBtn, etc.

    private DishReflectionData current;
    private int pageIndex;
    private bool isTyping;
    private Coroutine typeRoutine;

    private void Awake()
    {
        continueBtn.onClick.AddListener(OnContinue);
    }

    /// Call when the level ends: picks the data from the selected level.
    public void Show()
    {
        Show(FindForSelectedLevel());
    }

    /// Or pass a specific DishReflectionData yourself.
    public void Show(DishReflectionData data)
    {
        if (data == null || data.pages == null || data.pages.Length == 0)
        {
            Debug.LogWarning("[LevelCompletionReflection] No reflection data/pages found - skipping.");
            onFinished?.Invoke();
            return;
        }

        current = data;

        // Save first so the stars match the saved result
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.SaveDishResult();

        if (dishImage != null)
        {
            var selected = LevelSelectionManager.SelectedLevel;
            if (current.dishSprite != null)
                dishImage.sprite = current.dishSprite;
            else if (selected != null && selected.recipe != null && selected.recipe.recipeIcon != null)
                dishImage.sprite = selected.recipe.recipeIcon;   // fallback: recipe icon
        }

        gameObject.SetActive(true);
        if (replayOrGotToMap != null)
            replayOrGotToMap.SetActive(false);

        foreach (var go in hideOnFinish)
            if (go != null) go.SetActive(true);
        pageIndex = 0;
        ShowPage();
    }

    private DishReflectionData FindForSelectedLevel()
    {
        var selected = LevelSelectionManager.SelectedLevel;
        if (selected != null && selected.reflection != null)
            return selected.reflection;
        return fallback;
    }

    private void ShowPage()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeText(current.pages[pageIndex]));
    }

    private IEnumerator TypeText(string full)
    {
        isTyping = true;
        reflectionText.text = full;
        reflectionText.maxVisibleCharacters = 0;
        reflectionText.ForceMeshUpdate();

        if (reflectionText.isTextOverflowing)
            Debug.LogWarning($"[LevelCompletionReflection] Page {pageIndex + 1} of '{current.name}' is too long for the text box " +
                             $"({full.Length} chars). Shorten it or split it into two pages.");

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
        // First tap: finish typing the current page
        if (isTyping)
        {
            StopCoroutine(typeRoutine);
            reflectionText.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
            return;
        }

        pageIndex++;
        if (pageIndex < current.pages.Length)
            ShowPage();
        else
        {
            foreach (var go in hideOnFinish)
                if (go != null) go.SetActive(false);

            if (replayOrGotToMap != null)
                replayOrGotToMap.SetActive(true);

            onFinished?.Invoke();
        }
    }
}