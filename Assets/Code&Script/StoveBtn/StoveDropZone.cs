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

    public GameObject CurrentPan { get; private set; }
    public bool HasPan => CurrentPan != null;

    public event System.Action<GameObject> OnPanPlaced;

    private void Awake()
    {
        if (myImage == null) myImage = GetComponent<Image>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Once the pan is here, StoveDropZone shouldn't even be catching drops
        // anymore (its raycasting gets turned off in PlacePan below), but this
        // is a safety net in case it's still active for some reason.
        if (HasPan) return;

        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        TestDrag drag = dropped.GetComponent<TestDrag>();
        if (drag == null) return;

        string droppedId = drag.itemId;

        if (droppedId != panId)
        {
            Debug.Log($"[StoveDrop] '{droppedId}' dropped on stove - only the pan ('{panId}') is accepted. Bouncing back.");
            return;
        }

        PlacePan(dropped, drag);
    }

    private void PlacePan(GameObject dropped, TestDrag drag)
    {
        drag.SnapTo(transform as RectTransform);
        CurrentPan = dropped;

        Debug.Log($"[StoveDrop] Pan placed on stove: {dropped.name}");

        OnPanPlaced?.Invoke(dropped);
        CookingPrepListUI.Instance?.CompleteStep(panStepId);

        // Stop catching drops entirely - let PanDropZone receive them directly from now on
        if (myImage != null)
        {
            myImage.raycastTarget = false;
            Debug.Log("[StoveDrop] Raycast Target disabled - StoveDropZone will no longer intercept drops.");
        }
    }

    /// <summary>Call on Retry / new dish.</summary>
    public void ResetStove()
    {
        CurrentPan = null;
        if (myImage != null) myImage.raycastTarget = true; // re-enable so a new pan can be dropped
    }
}