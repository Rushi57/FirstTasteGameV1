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
    public StoveDialUI dialUI;         
    public Button closeButton;          

    [Header("Visuals (all optional)")]
    public Image stoveImage;
    public Sprite offSprite;
    public Sprite lowSprite;
    public Sprite mediumSprite;
    public Sprite highSprite;


    [Header("Recipe")]
    public RecipeData recipe;
    public float confirmDelay = 1f;

    [Header("Tutorial")]
    public TutorialInteractable tutorialInteractable;
    [Header("Optional")]
    public StoveDropZone stoveDropZone;
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
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnTutorialReverted += SyncDialToStep;
    }
    private void OnDestroy()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnTutorialReverted -= SyncDialToStep;
    }

    private void SyncDialToStep()
    {
        var step = TutorialManager.Instance?.CurrentStep;
        if (step == null || tutorialInteractable == null) return;

        string target = step.targetId?.Trim();

        if (step.actionType == TutorialActionType.Tap &&
            target == tutorialInteractable.ResolvedId)
        {
            CloseDial();
        }
        else if (step.actionType == TutorialActionType.Drag &&
                 (target == "StoveLow" || target == "StoveMedium" || target == "StoveHigh"))
        {
            SetHeat(StoveHeat.Off);          
            if (dialPanel != null) dialPanel.SetActive(true);
            dialUI?.SetVisual(CurrentHeat);
        }
    }
    // Dial open / close

    public void OnPointerClick(PointerEventData eventData)
    {
      
        var hit = eventData.pointerCurrentRaycast.gameObject;
        if (hit != null && (hit.GetComponentInParent<TestDrag>() != null ||
                            hit.GetComponentInParent<PanDropZone>() != null))
            return;

        if (dialPanel != null && dialPanel.activeSelf)
        {

            var m = TutorialManager.Instance;
            if (m != null && m.IsActive && tutorialInteractable != null &&
                m.IsExpected(tutorialInteractable.ResolvedId, TutorialActionType.Tap))
                tutorialInteractable.ReportTap();
            return;
        }
        OpenDial();
    }

    public void OpenDial()
    {
        if (dialPanel == null) return;

        //Block the dial 
        if (CookingPhaseGate.Instance != null && !CookingPhaseGate.Instance.CanUseStove()) return;
        if (!TutorialAllowsDial()) return;


        dialPanel.SetActive(true);
        dialUI?.SetVisual(CurrentHeat);
        tutorialInteractable?.ReportTap();  
    }

    public void CloseDial()
    {
        if (dialPanel != null) dialPanel.SetActive(false);
    }

    // Heat

    public void SetHeat(StoveHeat newHeat)
    {
        if (newHeat == CurrentHeat) return;

        StoveHeat old = CurrentHeat;
        CurrentHeat = newHeat;
        Debug.Log($"[Stove] Heat changed: {old} -> {newHeat}");

        ApplyVisuals();
        SFXManager.Instance?.SetFlame(newHeat);  
        if (newHeat == StoveHeat.Off)
            SFXManager.Instance?.StopLoop();
        OnHeatChanged?.Invoke(newHeat);

        if (confirmRoutine != null)
        {
            StopCoroutine(confirmRoutine);
            confirmRoutine = null;
        }

        if (newHeat != StoveHeat.Off && isActiveAndEnabled)
            confirmRoutine = StartCoroutine(ConfirmHeatRoutine(newHeat));
    }



    public void ResetStoveProgress()
    {
        SetHeat(StoveHeat.Off);
        CloseDial();
    }

    private void ApplyVisuals()
    {
        
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
                CloseDial();
            }
            else
            {
                Debug.Log($"[Stove] Waiting for {required} (step {order}), but stove is {heat}");
            }
            yield break;
        }
    }

    public bool IsHeatLocked
    {
        get
        {
            if (recipe == null) return false;

            CookingPrepListUI prepList = CookingPrepListUI.Instance;
            if (prepList == null) return false;          

            string currentId = prepList.CurrentStepId;
            if (currentId == null) return true;          

            for (int i = 0; i < recipe.cookingInstructions.Count; i++)
            {
                if (recipe.GetStepId(i) != currentId) continue;
                return !recipe.TryGetStoveStep(i, out _, out _);
            }
            return false;
        }
    }

    public void SetRecipe(RecipeData newRecipe)
    {
        recipe = newRecipe;
        ResetStoveProgress();
    }
    private void OnDisable()
    {
        SFXManager.Instance?.StopFlame();
    }

    private bool TutorialAllowsDial()
    {
        var m = TutorialManager.Instance;
        if (m == null || !m.IsActive) return true;
        if (tutorialInteractable == null) return true;   

        var step = m.CurrentStep;
        if (step == null) return true;

        string target = step.targetId?.Trim();
        if (string.IsNullOrEmpty(target)) return false;  

        if (target == "StoveLow" || target == "StoveMedium" || target == "StoveHigh") return true;

        return m.IsExpected(tutorialInteractable.ResolvedId, TutorialActionType.Tap);
    }
}