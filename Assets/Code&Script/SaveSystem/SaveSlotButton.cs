using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Put this on each LoadButton / LoadButton (1) ... in the ScrollSave Content.
[RequireComponent(typeof(Button))]
public class SaveSlotButton : MonoBehaviour
{
    [SerializeField] private TMP_Text label; // auto-found if left empty
    [SerializeField] private int totalLevels = 10; // how many level icons you have

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

        // Count each level that has a saved result (distinct, so a level is never counted twice)
        int unlocked = d.levelScores.Select(e => e.levelNumber).Distinct().Count();
        unlocked = Mathf.Clamp(unlocked, 0, totalLevels);

        label.text = $"Slot {slotIndex + 1} - {unlocked}/{totalLevels} Levels Unlocked\n{d.lastSaved}";
    }
}