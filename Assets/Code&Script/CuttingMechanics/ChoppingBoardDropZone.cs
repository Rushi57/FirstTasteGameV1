using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Put this on ChoppingBoardDropZoneObj, alongside an Image with Raycast
/// Target ON. Accepts:
///  - An ingredient (any object with TestDrag + a TutorialInteractable whose
///    sourceData is an IngredientData) - snaps it onto the board and
///    remembers it.
///  - The Knife (identified by its "KnifeObj" tag) - once dropped here AND
///    an ingredient is already on the board, activates Chopping/CuttingPanel
///    and configures CuttingMechanic to target that ingredient's visual
///    state. The knife is NOT snapped into place - it's left to bounce back
///    to its rack automatically via TestDrag's normal return-to-origin
///    behavior, so it's ready to be dragged again next time.
/// </summary>
public class ChoppingBoardDropZone : MonoBehaviour, IDropHandler
{
    [Header("What to trigger")]
    [Tooltip("The panel holding CuttingMechanic - activating it auto-starts the minigame via CuttingMechanic's own OnEnable().")]
    public GameObject choppingCuttingPanel;

    public CuttingMechanic cuttingMechanic;

    [Header("Knife identification")]
    [Tooltip("Tag used on your Knife GameObject.")]
    public string knifeTag = "KnifeObj";

    private GameObject currentIngredientObj;
    private IngredientStateController currentIngredientState;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        TestDrag drag = droppedObj.GetComponent<TestDrag>();
        if (drag == null) return;

        if (droppedObj.CompareTag(knifeTag))
        {
            HandleKnifeDropped();
        }
        else
        {
            HandleIngredientDropped(droppedObj, drag);
        }
    }

    private void HandleIngredientDropped(GameObject obj, TestDrag drag)
    {
        currentIngredientObj = obj;
        currentIngredientState = obj.GetComponent<IngredientStateController>();

        drag.SnapTo(transform as RectTransform);

        Debug.Log($"[ChoppingBoardDropZone] Ingredient placed on board: {obj.name}");
    }

    private void HandleKnifeDropped()
    {
        if (currentIngredientObj == null)
        {
            Debug.Log("[ChoppingBoardDropZone] Knife dropped but no ingredient on the board yet - ignoring.");
            return; // Knife bounces back to its rack automatically, since we never call SnapTo for it.
        }

        Debug.Log("[ChoppingBoardDropZone] Knife dropped with ingredient present - starting cutting.");

        if (choppingCuttingPanel != null)
            choppingCuttingPanel.SetActive(true); // CuttingMechanic.OnEnable() auto-starts the minigame
    }

    /// <summary>
    /// Call this from wherever the player closes/finishes the cutting panel
    /// (e.g. a manual "Done" button) - CuttingMechanic no longer fires an
    /// automatic completion event, since scoring now happens on every tap
    /// individually rather than a single final "required" cut.
    /// </summary>
    public void HandleCuttingComplete()
    {
        if (choppingCuttingPanel != null)
            choppingCuttingPanel.SetActive(false);

        currentIngredientObj = null;
        currentIngredientState = null;
    }
}