using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Put this on each LoadButton / LoadButton (1) ... in the ScrollSave Content.
[RequireComponent(typeof(Button))]
public class SaveSlotButton : MonoBehaviour
{
    [SerializeField] private TMP_Text label; // auto-found if left empty

    private int slotIndex;

    public int SlotIndex => slotIndex;

    public void Setup(int slot, Action<int> onClick)
    {
        slotIndex = slot;
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);

        Button btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick(slotIndex));
        Refresh();
    }

    public void Refresh()
    {
        if (label == null) return;
        SaveData d = SaveSystem.Load(slotIndex);
        if (d == null)
        {
            label.text = "No Save Data!";
            return;
        }

        int totalStars = 0;
        foreach (var entry in d.levelScores) totalStars += entry.stars;

        label.text = $"Slot {slotIndex + 1} - {totalStars} Stars - {d.unlockedLevels.Count} Levels\n{d.lastSaved}";
    }
}