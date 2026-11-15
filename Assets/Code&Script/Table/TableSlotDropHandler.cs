using UnityEngine;
using UnityEngine.EventSystems;

public class TableSlotDropHandler : MonoBehaviour, IDropHandler
{
   
    [Tooltip("The shared manager for all table slots.")]
    public TableItemSlotManager slotManager;

    private TutorialInteractable tutorialTag;

    private void Awake()
    {
        tutorialTag = GetComponent<TutorialInteractable>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        bool accepted = slotManager != null &&
                  slotManager.HandleDrop(transform as RectTransform, eventData.pointerDrag);
        Debug.Log("The item is dropped in here");
        if (accepted)
            tutorialTag?.ReportDrop();
    }
}