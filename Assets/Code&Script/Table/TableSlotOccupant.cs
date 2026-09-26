using UnityEngine;

public class TableSlotOccupant : MonoBehaviour
{
    public TableItemSlotManager slotManager;
    public RectTransform slot;

    public void FreeMySlot()
    {
       slotManager?.FreeSlot(slot);
    }
}
