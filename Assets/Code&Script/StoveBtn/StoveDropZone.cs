using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StoveDropZone : MonoBehaviour, IDropHandler
{
    [Header("Pan identification")]
    public string panId = "Pan";

    [Header("Cooking Prep")]
    public string panStepId = "Pan:OnStove";

    [Header("References")]
    [Tooltip("This object's own Image. Its Raycast Target is turned off once the pan is placed, so future drops go straight to the pan instead of being caught here.")]
    public Image myImage;


    private TutorialInteractable tutorialTag;
    public GameObject CurrentPan { get; private set; }
    public bool HasPan => CurrentPan != null;

    public event System.Action<GameObject> OnPanPlaced;

    private void Awake()
    {
        if (myImage == null) myImage = GetComponent<Image>();
        tutorialTag = GetComponent<TutorialInteractable>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (HasPan) return;

        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        TestDrag drag = dropped.GetComponent<TestDrag>();
        if (drag == null) return;

        // 1) wrong item
        if (drag.itemId != panId)
        {
            ScoreManager.Instance?.ReportMistake("Wrong Step");
            return; // TestDrag bounces it back
        }

        // 2) right item, wrong time
        if (CookingPrepListUI.Instance != null && !CookingPrepListUI.Instance.TryAccept(panStepId))
        {
            Debug.Log($"[StoveDrop] Pan dropped too early (current step is '{CookingPrepListUI.Instance.CurrentStepId}'). Bouncing back.");
            drag.ReturnToOrigin(); // explicit, so it never depends on call order
            return;
        }

        // 3) only now place it
        PlacePan(dropped, drag);

    }

    private void PlacePan(GameObject dropped, TestDrag drag)
    {


        drag.SnapTo(transform as RectTransform);
        CurrentPan = dropped;

        Debug.Log($"[StoveDrop] Pan placed on stove: {dropped.name}");

        OnPanPlaced?.Invoke(dropped);
        CookingPrepListUI.Instance?.CompleteStep(panStepId);
        tutorialTag?.ReportDrop();
        // Stop catching drops entirely - let PanDropZone receive them directly from now on
        if (myImage != null)
        {
            myImage.raycastTarget = false;
            Debug.Log("[StoveDrop] Raycast Target disabled - StoveDropZone will no longer intercept drops.");
        }

        if(CookingPrepListUI.Instance != null && !CookingPrepListUI.Instance.TryAccept(panStepId))
        {
            
                Debug.Log($"[StoveDrop] Pan dropped too early (current step is '{CookingPrepListUI.Instance.CurrentStepId}'). Bouncing back.");
                return; // don't SnapTo - TestDrag returns the pan to its original position
        }
        PlacePan(dropped, drag);
    }

    /// <summary>Call on Retry / new dish.</summary>
    public void ResetStove()
    {
        CurrentPan = null;
        if (myImage != null) myImage.raycastTarget = true; // re-enable so a new pan can be dropped
    }
}