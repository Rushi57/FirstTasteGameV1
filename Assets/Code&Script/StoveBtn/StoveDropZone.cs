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
    [Tooltip("This object's own Image. Its Raycast Target is turned off once the pan is placed.")]
    public Image myImage;

    // NEW
    [Tooltip("Empty child object used as the pan's center pivot. If left empty, the pan snaps to this object's own center.")]
    public RectTransform panSnapPoint;

    private TutorialInteractable tutorialTag;
    public GameObject CurrentPan { get; private set; }
    public bool HasPan => CurrentPan != null;

    public event System.Action<GameObject> OnPanPlaced;

    private void Awake()
    {
        if (myImage == null) myImage = GetComponent<Image>();
        tutorialTag = GetComponent<TutorialInteractable>();

        // NEW: fallback so it never breaks if you forget to assign it
        if (panSnapPoint == null) panSnapPoint = transform as RectTransform;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (HasPan) return;

        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        TestDrag drag = dropped.GetComponent<TestDrag>();
        if (drag == null) return;

        if (drag.itemId != panId)
        {
            ScoreManager.Instance?.ReportMistake("Wrong Step");
            return;
        }

        //Block the pan until Cooking Prep
        if(CookingPhaseGate.Instance != null && !CookingPhaseGate.Instance.CanUseStove())
        {
            drag.ReturnToOrigin();
            return;
        }

        if (CookingPrepListUI.Instance != null && !CookingPrepListUI.Instance.TryAccept(panStepId))
        {
            Debug.Log($"[StoveDrop] Pan dropped too early (current step is '{CookingPrepListUI.Instance.CurrentStepId}'). Bouncing back.");
            drag.ReturnToOrigin();
            return;
        }

        PlacePan(dropped, drag);
    }

    private void PlacePan(GameObject dropped, TestDrag drag)
    {
        // CHANGED: snap to the pivot object instead of this object
        drag.SnapTo(panSnapPoint);
        CurrentPan = dropped;

        Debug.Log($"[StoveDrop] Pan placed on stove: {dropped.name}");

        OnPanPlaced?.Invoke(dropped);
        CookingPrepListUI.Instance?.CompleteStep(panStepId);
        tutorialTag?.ReportDrop();

        if (myImage != null)
        {
            myImage.raycastTarget = false;
            Debug.Log("[StoveDrop] Raycast Target disabled - StoveDropZone will no longer intercept drops.");
        }
    }

    public void ResetStove()
    {
        CurrentPan = null;
        if (myImage != null) myImage.raycastTarget = true;
    }
}