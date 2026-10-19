using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChoppingBoardDropZone : MonoBehaviour, IDropHandler
{
    [Header("What to trigger")]
    public GameObject choppingCuttingPanel;
    public CuttingMechanic cuttingMechanic;

    [Header("Knife identification")]
    public string knifeTag = "KnifeObj";

    private GameObject currentIngredientObj;
    private IngredientData currentIngredientData;
    private Image currentIngredientImage;

    private TutorialInteractable tutorialTag;


    private void Awake()
    {
        tutorialTag = GetComponent<TutorialInteractable>();
    }
    private void OnEnable()
    {
        if (cuttingMechanic != null)
            cuttingMechanic.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (cuttingMechanic != null)
            cuttingMechanic.OnStateChanged -= HandleStateChanged;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        Debug.Log($"[DropZone] OnDrop: {(droppedObj != null ? droppedObj.name : "null")}");
        if (droppedObj == null) return;

        TestDrag drag = droppedObj.GetComponent<TestDrag>();
        if (drag == null) { Debug.Log("[DropZone] no TestDrag on dropped object"); return; }

        if (CookingPhaseGate.Instance != null && !CookingPhaseGate.Instance.CanCut())
        {
            Debug.Log($"[DropZone] CanCut() is false, phase blocks cutting");
            drag.ReturnToOrigin();
            return;
        }
        if (droppedObj.CompareTag(knifeTag))
        {
            HandleKnifeDropped(drag);
            drag.ReturnToOrigin();
            drag.MarkDropHandled();   // stops OnEndDrag from reporting a tutorial mistake
        }
        else
            HandleIngredientDropped(droppedObj, drag);
    }

    private void HandleIngredientDropped(GameObject obj, TestDrag drag)
    {
        if(IsBoardOccupide() && currentIngredientObj != obj)
        {
            return;
        }

        var interactable = obj.GetComponent<TutorialInteractable>();
        IngredientData data = interactable != null ? interactable.sourceData as IngredientData : null;


        if (data == null)
        {
            Debug.LogWarning($"[ChoppingBoardDropZone] {obj.name} has no IngredientData (TutorialInteractable.sourceData) - can't be chopped.");
            return;
        }

        //Wrong Ingredient or item
        if(CookingPrepListUI.Instance !=  null && !CookingPrepListUI.Instance.TryAcceptIngredientForCutting(data.id))
        {
            return;
        }

        currentIngredientObj = obj;
        currentIngredientData = data;
        currentIngredientImage = obj.GetComponent<Image>();

        drag.SnapTo(transform as RectTransform);
        Debug.Log($"[ChoppingBoardDropZone] Ingredient placed on board: {obj.name}");

        tutorialTag?.ReportDrop();
    }

    private void HandleKnifeDropped(TestDrag knifeDrag)
    {
        if (currentIngredientObj == null || currentIngredientData == null)
        {
            Debug.Log("[DropZone] Knife dropped but no ingredient on the board.");
            return;
        }

        // Already-finished ingredient (or not the current step): mistake, no panel
        if (CookingPrepListUI.Instance != null &&
            !CookingPrepListUI.Instance.TryAcceptIngredientForCutting(currentIngredientData.id))
        {
            return;   // popup + heart loss handled by the prep list
        }

        choppingCuttingPanel.SetActive(true);
        cuttingMechanic.SetIngredient(currentIngredientData);
        tutorialTag?.ReportDrop();
    }

    // Keeps the ingredient sitting on the board in sync with the cutting panel
    private void HandleStateChanged(IngredientData data, IngredientPrepState state)
    {
        if (data != currentIngredientData || currentIngredientImage == null) return; // ignore stale data
        currentIngredientImage.sprite = data.GetSpriteForState(state);
    }

    public void HandleCuttingComplete()
    {
        if (choppingCuttingPanel != null)
            choppingCuttingPanel.SetActive(false);

        currentIngredientObj = null;
        currentIngredientData = null;
        currentIngredientImage = null;
    }

    private bool IsBoardOccupide()
    {
        if(currentIngredientObj == null) { ClearCurrent(); return false; }

        RectTransform board =transform as RectTransform;
        RectTransform ing = currentIngredientObj.transform as RectTransform;

        Vector3 worldCenter = ing.TransformPoint(ing.rect.center);
        Vector2 local = board.InverseTransformPoint(worldCenter);
        bool stillOnBoard = board.rect.Contains(local);

        if (!stillOnBoard) ClearCurrent();
        return stillOnBoard;
    }

    private void ClearCurrent()
    {
        currentIngredientObj = null;
        currentIngredientData = null;
        currentIngredientImage = null;
    }
}