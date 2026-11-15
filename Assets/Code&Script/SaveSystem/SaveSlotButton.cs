using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SaveSlotButton : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private int totalLevels = 10; 

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

       
        int unlocked = d.levelScores.Select(e => e.levelNumber).Distinct().Count();
        unlocked = Mathf.Clamp(unlocked, 0, totalLevels);

        label.text = $"Slot {slotIndex + 1} - {unlocked}/{totalLevels} Levels Unlocked\n{d.lastSaved}";
    }
}