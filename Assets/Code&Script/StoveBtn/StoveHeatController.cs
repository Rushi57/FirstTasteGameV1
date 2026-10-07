using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum StoveHeat { Off, Low, Medium, High }

public class StoveHeatController : MonoBehaviour, IPointerClickHandler
{
    [Header("Dial")]
    [Tooltip("StoveDialGameObject (starts inactive).")]
    public GameObject dialPanel;
    public StoveDialUI dialUI;             // on DialImage
    public Button closeButton;             // CloseStoveDial

    [Header("Visuals (all optional)")]
    public Image stoveImage;
    public Sprite offSprite;
    public Sprite lowSprite;
    public Sprite mediumSprite;
    public Sprite highSprite;
    public TMP_Text heatLabel;

    [Header("Recipe")]
    public RecipeData recipe;
    public float confirmDelay = 1f;

    [Header("Tutorial")]
    public TutorialInteractable tutorialInteractable;

    public StoveHeat CurrentHeat { get; private set; } = StoveHeat.Off;
    public event System.Action<StoveHeat> OnHeatChanged;

    private Coroutine confirmRoutine;

    private void Awake()
    {
        if (tutorialInteractable == null)
            tutorialInteractable = GetComponent<TutorialInteractable>();

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseDial);

        if (dialPanel != null) dialPanel.SetActive(false);
    }

    private void Start()
    {
        ApplyVisuals();
    }

    // ---------------- Dial open / close ----------------

    public void OnPointerClick(PointerEventData eventData)
    {
        // Ignore clicks that bubble up from the dial itself
        if (dialPanel != null && dialPanel.activeSelf) return;
        OpenDial();
    }

    public void OpenDial()
    {
        if (dialPanel == null) return;
        dialPanel.SetActive(true);
        dialUI?.SetVisual(CurrentHeat);
        tutorialInteractable?.ReportTap();   // see note below
    }

    public void CloseDial()
    {
        if (dialPanel != null) dialPanel.SetActive(false);
    }

    // ---------------- Heat ---------------- (unchanged except ApplyVisuals)

    public void SetHeat(StoveHeat newHeat)
    {
        if (newHeat == CurrentHeat) return;

        StoveHeat old = CurrentHeat;
        CurrentHeat = newHeat;
        Debug.Log($"[Stove] Heat changed: {old} -> {newHeat}");

        ApplyVisuals();
        OnHeatChanged?.Invoke(newHeat);

        if (confirmRoutine != null)
        {
            StopCoroutine(confirmRoutine);
            confirmRoutine = null;
        }

        if (newHeat != StoveHeat.Off && isActiveAndEnabled)
            confirmRoutine = StartCoroutine(ConfirmHeatRoutine(newHeat));
    }

    // ... keep ConfirmHeatRoutine exactly as it is ...

    public void ResetStoveProgress()
    {
        SetHeat(StoveHeat.Off);
        CloseDial();
    }

    private void ApplyVisuals()
    {
        if (heatLabel != null) heatLabel.text = CurrentHeat.ToString();
        dialUI?.SetVisual(CurrentHeat);

        if (stoveImage == null) return;
        Sprite s = CurrentHeat switch
        {
            StoveHeat.Low => lowSprite,
            StoveHeat.Medium => mediumSprite,
            StoveHeat.High => highSprite,
            _ => offSprite
        };
        if (s != null) stoveImage.sprite = s;
    }

    private System.Collections.IEnumerator ConfirmHeatRoutine(StoveHeat heat)
    {
        if (confirmDelay > 0f)
            yield return new WaitForSeconds(confirmDelay);
        confirmRoutine = null;

        if (recipe == null)
        {
            Debug.LogWarning("[Stove] No recipe assigned - can't check required heat.");
            yield break;
        }

        CookingPrepListUI prepList = CookingPrepListUI.Instance;
        if (prepList == null) yield break;

        string currentId = prepList.CurrentStepId;
        if (currentId == null)
        {
            Debug.Log("[Stove] All prep steps are already done.");
            yield break;
        }

        // Find the recipe line that matches the prep list's current step
        for (int i = 0; i < recipe.cookingInstructions.Count; i++)
        {
            if (recipe.GetStepId(i) != currentId) continue;

            if (!recipe.TryGetStoveStep(i, out int order, out StoveHeat required))
            {
                Debug.Log($"[Stove] Current step '{currentId}' isn't a stove step.");
                yield break;
            }

            if (required == heat)
            {
                Debug.Log($"[Stove] Step {order} done: {required} held for {confirmDelay}s -> '{currentId}'");
                prepList.CompleteStep(currentId);
            }
            else
            {
                Debug.Log($"[Stove] Waiting for {required} (step {order}), but stove is {heat}");
            }
            yield break;
        }
    }
    private void OnEnable()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnTutorialReverted += OnTutorialReverted;
    }

    private void OnDisable()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnTutorialReverted -= OnTutorialReverted;
    }

    private void OnTutorialReverted()
    {
        // Back to Off so the replayed step starts clean.
        // Keep the dial open so the player can turn it again.
        SetHeat(StoveHeat.Off);
        dialUI?.SetVisual(StoveHeat.Off);
    }


    public void SetRecipe(RecipeData newRecipe)
    {
        recipe = newRecipe;
        ResetStoveProgress();
    }

}